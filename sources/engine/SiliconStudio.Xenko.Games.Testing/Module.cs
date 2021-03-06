﻿// Copyright (c) 2014-2015 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Threading.Tasks;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Engine;

namespace SiliconStudio.Xenko.Games.Testing
{
    //This is how we inject the assembly to run automatically at game start, paired with Xenko.targets and the msbuild property SiliconStudioAutoTesting
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            //Quit after 10 seconds anyway!
            Task.Run(async () =>
            {
                await Task.Delay(10000);
                if (!GameTestingSystem.Initialized)
                {
                    GameTestingSystem.Quit();
                }
            });

            //quit after 10 seconds in any case
            Game.GameStarted += (sender, args) =>
            {              
                var game = (Game)sender;
                var testingSystem = new GameTestingSystem(game.Services);
                game.GameSystems.Add(testingSystem);
            };
        }
    }
}
