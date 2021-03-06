﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.MicroThreading;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Xenko.Assets.Debugging;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Engine.Processors;

namespace SiliconStudio.Xenko.Debugger.Target
{
    public class GameDebuggerTarget : IGameDebuggerTarget
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("GameDebuggerSession");

        /// <summary>
        /// The assembly container, to load assembly without locking main files.
        /// </summary>
        // For now, it uses default one, but later we should probably have one per game debugger session
        private AssemblyContainer assemblyContainer = AssemblyContainer.Default;

        private string projectName;
        private Dictionary<DebugAssembly, Assembly> loadedAssemblies = new Dictionary<DebugAssembly, Assembly>();
        private int currentDebugAssemblyIndex;
        private Game game;

        private ManualResetEvent gameFinished = new ManualResetEvent(true);
        private IGameDebuggerHost host;

        private bool requestedExit;

        public GameDebuggerTarget()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            // Make sure this assembly is registered (it contains custom Yaml serializers such as CloneReferenceSerializer)
            // Note: this assembly should not be registered when run by Game Studio
            AssemblyRegistry.Register(typeof(Program).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            lock (loadedAssemblies)
            {
                return loadedAssemblies.Values.FirstOrDefault(x => x.FullName == args.Name);
            }
        }

        /// <inheritdoc/>
        public void Exit()
        {
            requestedExit = true;
        }

        /// <inheritdoc/>
        public DebugAssembly AssemblyLoad(string assemblyPath)
        {
            try
            {
                var assembly = assemblyContainer.LoadAssemblyFromPath(assemblyPath);
                if (assembly == null)
                {
                    Log.Error("Unexpected error while loading assembly reference [{0}] in project [{1}]", assemblyPath, projectName);
                    return DebugAssembly.Empty;
                }

                return CreateDebugAssembly(assembly);
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected error while loading assembly reference [{0}] in project [{1}]", ex, assemblyPath, projectName);
                return DebugAssembly.Empty;
            }
        }

        /// <inheritdoc/>
        public DebugAssembly AssemblyLoadRaw(byte[] peData, byte[] pdbData)
        {
            try
            {
                lock (loadedAssemblies)
                {
                    var assembly = Assembly.Load(peData, pdbData);
                    return CreateDebugAssembly(assembly);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected error while loading assembly reference in project [{0}]", ex, projectName);
                return DebugAssembly.Empty;
            }
        }

        /// <inheritdoc/>
        public bool AssemblyUpdate(List<DebugAssembly> assembliesToUnregister, List<DebugAssembly> assembliesToRegister)
        {
            Log.Info("Reloading assemblies and updating scripts");

            // Unload and load assemblies in assemblyContainer, serialization, etc...
            lock (loadedAssemblies)
            {
                var assemblyReloader = new LiveAssemblyReloader(
                    game,
                    assemblyContainer,
                    assembliesToUnregister.Select(x => loadedAssemblies[x]).ToList(),
                    assembliesToRegister.Select(x => loadedAssemblies[x]).ToList());

                if (game != null)
                {
                    lock (game.TickLock)
                    {
                        assemblyReloader.Reload();
                    }
                }
                else
                {
                    assemblyReloader.Reload();
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public List<string> GameEnumerateTypeNames()
        {
            lock (loadedAssemblies)
            {
                return GameEnumerateTypesHelper().Select(x => x.FullName).ToList();
            }
        }

        /// <inheritdoc/>
        public void GameLaunch(string gameTypeName)
        {
            try
            {
                Log.Info("Running game with type {0}", gameTypeName);

                Type gameType;
                lock (loadedAssemblies)
                {
                    gameType = GameEnumerateTypesHelper().FirstOrDefault(x => x.FullName == gameTypeName);
                }

                if (gameType == null)
                    throw new InvalidOperationException(string.Format("Could not find type [{0}] in project [{1}]", gameTypeName, projectName));

                game = (Game)Activator.CreateInstance(gameType);

                // TODO: Bind database
                Task.Run(() =>
                {
                    gameFinished.Reset();
                    try
                    {
                        using (game)
                        {
                            // Allow scripts to crash, we will still restart them
                            game.Script.Scheduler.PropagateExceptions = false;
                            game.Run();
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error("Exception while running game", e);
                    }

                    host.OnGameExited();

                    // Notify we are done
                    gameFinished.Set();
                });
            }
            catch (Exception ex)
            {
                Log.Error("Game [{0}] from project [{1}] failed to run", ex, gameTypeName, projectName);
            }
        }

        /// <inheritdoc/>
        public void GameStop()
        {
            if (game == null)
                return;

            game.Exit();

            // Wait for game to actually exit?
            gameFinished.WaitOne();

            game = null;
        }

        private IEnumerable<Type> GameEnumerateTypesHelper()
        {
            // We enumerate custom games, and then typeof(Game) as fallback
            return loadedAssemblies.SelectMany(assembly => assembly.Value.GetTypes().Where(x => typeof(Game).IsAssignableFrom(x)))
                .Concat(Enumerable.Repeat(typeof(Game), 1));
        }

        private DebugAssembly CreateDebugAssembly(Assembly assembly)
        {
            var debugAssembly = new DebugAssembly(++currentDebugAssemblyIndex);
            loadedAssemblies.Add(debugAssembly, assembly);
            return debugAssembly;
        }

        public void MainLoop(IGameDebuggerHost gameDebuggerHost)
        {
            host = gameDebuggerHost;
            host.RegisterTarget();

            Log.MessageLogged += Log_MessageLogged;

            // Log suppressed exceptions in scripts
            ScriptSystem.Log.MessageLogged += Log_MessageLogged;
            Scheduler.Log.MessageLogged += Log_MessageLogged;

            Log.Info("Starting debugging session");

            while (!requestedExit)
            {
                Thread.Sleep(10);
            }
        }

        void Log_MessageLogged(object sender, MessageLoggedEventArgs e)
        {
            var message = e.Message;

            var serializableMessage = message as SerializableLogMessage;
            if (serializableMessage == null)
            {
                var logMessage = message as LogMessage;
                if (logMessage != null)
                {
                    serializableMessage = new SerializableLogMessage(logMessage);
                }
            }

            if (serializableMessage == null)
            {
                throw new InvalidOperationException(@"Unable to process the given log message.");
            }

            host.OnLogMessage(serializableMessage);
        }
    }
}
