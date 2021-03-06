﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Linq;
using System.Collections.Generic;

using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;

namespace SiliconStudio.Xenko.Audio
{

    /// <summary>
    /// Describe an play action that a SoundMusic can request to the AudioEngine
    /// </summary>
    internal enum SoundMusicAction
    {
        Play,
        Pause,
        Stop,
        Volume,
    }

    /// <summary>
    /// A SoundMusic action request aimed for the AudioEngine.
    /// </summary>
    internal struct SoundMusicActionRequest
    {
        public SoundMusic Requester;

        public SoundMusicAction RequestedAction;

        public SoundMusicActionRequest(SoundMusic requester, SoundMusicAction request)
        {
            Requester = requester;
            RequestedAction = request;
        }
    }

    /// <summary>
    /// A <see cref="SoundMusic"/> event.
    /// </summary>
    internal enum SoundMusicEvent
    {
        ErrorOccurred,
        MetaDataLoaded,
        ReadyToBePlayed,
        EndOfTrackReached,
        MusicPlayerClosed,
    }

    /// <summary>
    /// A notification of an SoundMusic event.
    /// </summary>
    internal struct SoundMusicEventNotification
    {
        public SoundMusicEvent Event;

        public object EventData;

        public SoundMusicEventNotification(SoundMusicEvent mEvent, object eventData)
        {
            Event = mEvent;
            EventData = eventData;
        }
    }

    /// <summary>
    /// Represents the audio engine. 
    /// In current version, the audio engine necessarily creates its context on the default audio hardware of the device.
    /// The audio engine is required when creating or loading sounds.
    /// </summary>
    /// <remarks>The AudioEngine is Disposable. Call the <see cref="ComponentBase.Dispose"/> function when you do not need to play sounds anymore to free memory allocated to the audio system. 
    /// A call to Dispose automatically stops and disposes all the <see cref="SoundEffect"/>, <see cref="SoundEffectInstance"/> and <see cref="DynamicSoundEffectInstance"/> 
    /// sounds created by this AudioEngine</remarks>
    /// <seealso cref="SoundEffect.Load"/>
    /// <seealso cref="SoundMusic.Load"/>
    /// <seealso cref="DynamicSoundEffectInstance"/>
    public sealed partial class AudioEngine : ComponentBase
    {
        /// <summary>
        /// The logger of the audio engine.
        /// </summary>
        public static readonly Logger Logger = GlobalLogger.GetLogger("AudioEngine");

        /// <summary>
        /// Create an Audio Engine on the default audio device
        /// </summary>
        /// <param name="sampleRate">The desired sample rate of the audio graph. 0 let the engine choose the best value depending on the hardware.</param>
        /// <exception cref="AudioInitializationException">Initialization of the audio engine failed. May be due to memory problems or missing audio hardware.</exception>
        public AudioEngine(uint sampleRate = 0)
            : this(null, sampleRate)
        {
        }

        /// <summary>
        /// Create the Audio Engine on the specified device.
        /// </summary>
        /// <param name="device">Device on which to create the audio engine.</param>
        /// <param name="sampleRate">The desired sample rate of the audio graph. 0 let the engine choose the best value depending on the hardware.</param>
        /// <remarks>Available devices can be queried by calling static method <see cref="GetAvailableDevices"/></remarks>
        /// <exception cref="AudioInitializationException">Initialization of the audio engine failed. May be due to memory problems or missing audio hardware.</exception>
        private AudioEngine(AudioDevice device, uint sampleRate = 0)
        {
            if (device != null)
                throw new NotImplementedException();

            State = AudioEngineState.Running;

            AudioSampleRate = sampleRate;

            AudioEngineImpl(device);

            ++nbOfAudioEngineInstances;
        }

        /// <summary>
        /// Get the list of the audio hardware available on the device.
        /// </summary>
        /// <returns>List of available devices</returns>
        /// <seealso cref="AudioEngine"/>
        private static List<AudioDevice> GetAvailableDevices()
        {
            throw new NotImplementedException();
        }

        private static int nbOfAudioEngineInstances;

        /// <summary>
        /// The list of the sounds that have been paused by the call to <see cref="PauseAudio"/> and should be resumed by <see cref="ResumeAudio"/>.
        /// </summary>
        private readonly List<SoundInstanceBase> pausedSounds = new List<SoundInstanceBase>();

        /// <summary>
        /// The underlying sample rate of the audio system.
        /// </summary>
        internal uint AudioSampleRate { get; private set; }

        /// <summary>
        /// Method that updates all the sounds play status. 
        /// </summary>
        /// <remarks>Should be called in same thread as user main thread.</remarks>
        /// <exception cref="InvalidOperationException">One or several of the sounds asked for play had invalid data (corrupted or unsupported formats).</exception>
        public void Update()
        {
            UpdateMusicImpl();
        }

        /// <summary>
        /// The current state of the <see cref="AudioEngine"/>.
        /// </summary>
        public AudioEngineState State { get; private set; }

        /// <summary>
        /// Pause the audio engine. That is, pause all the currently playing <see cref="SoundInstanceBase"/>, and block any future play until <see cref="ResumeAudio"/> is called.
        /// </summary>
        public void PauseAudio()
        {
            if(State != AudioEngineState.Running)
                return;

            State = AudioEngineState.Paused;

            PauseAudioPlatformSpecific();

            pausedSounds.Clear();
            foreach (var sound in notDisposedSounds)
            {
                var soundEffect = sound as SoundEffect;
                if (soundEffect != null)
                {
                    foreach (var instance in soundEffect.Instances)
                    {
                        if (instance.PlayState == SoundPlayState.Playing)
                        {
                            instance.Pause();
                            pausedSounds.Add(instance);
                        }
                    }
                }
                var soundInstance = sound as SoundInstanceBase;
                if(soundInstance != null && soundInstance.PlayState == SoundPlayState.Playing)
                {
                    soundInstance.Pause();
                    pausedSounds.Add(soundInstance);
                }
            }
        }

        /// <summary>
        /// Resume all audio engine. That is, resume the sounds paused by <see cref="PauseAudio"/>, and re-authorize future calls to play.
        /// </summary>
        public void ResumeAudio()
        {
            if(State != AudioEngineState.Paused)
                return;

            State = AudioEngineState.Running;

            ResumeAudioPlatformSpecific();

            foreach (var playableSound in pausedSounds)
            {
                if (!playableSound.IsDisposed && playableSound.PlayState == SoundPlayState.Paused) // sounds can have been stopped by user while the audio engine was paused.
                    playableSound.Play();
            }
        }

        /// <summary>
        /// Return the <see cref="SoundEffect"/> evaluated as having least impact onto the final audio output among all the non stopped <see cref="SoundEffect"/>.
        /// </summary>
        /// <returns>An instance of <see cref="SoundEffect"/> currently playing or paused, or null if no candidate</returns>
        public SoundEffect GetLeastSignificativeSoundEffect()
        {
            // the current heuristic consist in finding the shortest element not looped
            SoundEffect bestCandidate = null;
            var bestCandidateSize = int.MaxValue;
            lock (notDisposedSounds)
            {
                foreach (var notDisposedSound in notDisposedSounds)
                {
                    var soundEffect = notDisposedSound as SoundEffect;
                    if(soundEffect == null)
                        continue;

                    var sizeSoundEffect = soundEffect.WaveDataSize / soundEffect.WaveFormat.BlockAlign;
                    if(sizeSoundEffect >= bestCandidateSize)
                        continue;

                    foreach (var instance in soundEffect.Instances)
                    {
                        if (instance.PlayState != SoundPlayState.Stopped && !instance.IsLooped)
                        {
                            bestCandidate = soundEffect;
                            bestCandidateSize = sizeSoundEffect;
                            break;
                        }
                    }

                }
            }

            return bestCandidate;
        }

        /// <summary>
        /// Force all the <see cref="SoundEffectInstance"/> to update them-self
        /// </summary>
        internal void ForceSoundEffectInstanceUpdate()
        {
            lock (notDisposedSounds)
            {
                foreach (var notDisposedSound in notDisposedSounds)
                {
                    var soundEffect = notDisposedSound as SoundEffect;
                    if (soundEffect == null)
                        continue;

                    foreach (var instance in soundEffect.Instances)
                        instance.PlayState = instance.PlayState;
                }
            }
        }

        private readonly List<SoundBase> notDisposedSounds = new List<SoundBase>(); 

        internal void RegisterSound(SoundBase newSound)
        {
            lock (notDisposedSounds)
            {
                notDisposedSounds.Add(newSound);
            }
        }

        internal void UnregisterSound(SoundBase disposedSound)
        {
            lock (notDisposedSounds)
            {
                if (!notDisposedSounds.Remove(disposedSound))
                    throw new AudioSystemInternalException("Try to remove a disposed sound not in the list of registered sounds.");
            }
        }

        protected override void Destroy()
        {
            base.Destroy();

            if (IsDisposed)
                return;

            State = AudioEngineState.Disposed;

            SoundBase[] notDisposedSoundsArray;
            lock (notDisposedSounds)
            {
                notDisposedSoundsArray = notDisposedSounds.ToArray();
            }

            // Dispose all the sound not disposed yet.
            foreach (var soundBase in notDisposedSoundsArray)
                soundBase.Dispose();

            --nbOfAudioEngineInstances;

            DestroyImpl();
        }

        /// <summary>
        /// The pending Sound Music requests
        /// </summary>
        private readonly ThreadSafeQueue<SoundMusicActionRequest> musicActionRequests = new ThreadSafeQueue<SoundMusicActionRequest>();

        /// <summary>
        /// Submit an new MusicAction request to the AudioEngine. It will effectively executed during the next AudioEngine.Update().
        /// </summary>
        /// <param name="request">The request to submit.</param>
        internal void SubmitMusicActionRequest(SoundMusicActionRequest request)
        {
            musicActionRequests.Enqueue(request);
        }

        /// <summary>
        /// The music that is currently playing or the last music that have been played.
        /// </summary>
        private SoundMusic currentMusic;

        private void QueueLastPendingRequests(Dictionary<SoundMusicAction, bool> requestAftPlay, SoundMusic requester)
        {
            lock (musicActionRequests)
            {
                foreach (var action in requestAftPlay.Where(x => x.Value).Select(x => x.Key))
                    musicActionRequests.Enqueue(new SoundMusicActionRequest(requester, action));
            }
        }

        /// <summary>
        /// This method process all the events from the MusicPlayer and the requests from the SoundMusics.
        /// <para>There is only a single instance MusicPlayer for several SoundMusic play requests, that why we need to change the player data.
        /// That is why we need one or more call to <see cref="Update"/> to change the current music.</para>
        /// <para>In order to avoid useless change of the MusicPlayer data, 
        /// we analyse the whole MusicActionRequest queue and drop all the requests that are followed by subsequent Play-Requests.</para>
        /// </summary>
        private void UpdateMusicImpl()
        {
            // 1. First deals with the events from the media Session received since the last update
            ProccessQueuedMediaSessionEvents();

            // 2. Deals with the SoundMusic action requests
            ProcessMusicActionRequests();
        }
        
        /// <summary>
        /// In order to avoid problem that appeared (see TestMediaFoundation) when using MediaSession.SetTopology,
        /// we destroy and recreate the media session every time we change the music to play. 
        /// Before shutting down the MediaSession we need to receive the SessionClosed.
        /// </summary>
        private void ProcessMusicActionRequests()
        {
            // Here we need to analyse all the request flow to determine which action really need to be performed at the end.

            // skip everythings if no requests
            if (musicActionRequests.Count == 0)
                return;

            // postpone the proccess of the requests to next Update if there exist a media session but it is not ready yet to be played.
            if (currentMusic != null && !isMusicPlayerReady)
                return;
            
            // now analyse the list of requests to determine the actions to performs
            var lastPlayRequestMusicInstance = currentMusic;    // the last SoundMusic instance that asked for a Play-Request.
            var actionRequestedAftLastPlay = new Dictionary<SoundMusicAction, bool> // Specify if there is Request to the given Action after the Play-Request of the last played music.
                {
                    { SoundMusicAction.Pause, false },
                    { SoundMusicAction.Stop, false },
                    { SoundMusicAction.Volume, false }
                };
            var shouldRestartCurrentMusic = false;
            var shouldStartCurrentMusic = false;

            foreach (var actionRequest in musicActionRequests.DequeueAsList())
            {
                if (actionRequest.RequestedAction == SoundMusicAction.Play)
                {
                    lastPlayRequestMusicInstance = actionRequest.Requester;
                    shouldRestartCurrentMusic = shouldRestartCurrentMusic || lastPlayRequestMusicInstance != currentMusic || actionRequestedAftLastPlay[SoundMusicAction.Stop];
                    shouldStartCurrentMusic = true;
                    foreach (var action in actionRequestedAftLastPlay.Keys.ToArray())
                        actionRequestedAftLastPlay[action] = false;
                }
                else if (actionRequest.Requester == lastPlayRequestMusicInstance)
                {
                    actionRequestedAftLastPlay[actionRequest.RequestedAction] = true;
                }
            }

            // perform the action depending on the request analysis.
            if (currentMusic == null)
            {
                // there is currently no music loaded. 
                // => just load the last music to play and queue subsequent pause/stop/volume requests

                // no play requests => nothing to do (just skip everything)
                if (lastPlayRequestMusicInstance == null)
                    return;

                // Load the music corresponding to the last play-request.
                if(!lastPlayRequestMusicInstance.IsDisposed) // music can be disposed by the user before the call to Update.
                    LoadNewMusic(lastPlayRequestMusicInstance);

                // Queue the remaining waiting requests
                QueueLastPendingRequests(actionRequestedAftLastPlay, currentMusic);
            }
            else
            {
                // there is currently one music loaded:
                //  - if another music need to be played => close the session and queue Play/Pause/Stop/Volume requests
                //  - else => perform Play/Pause/Stop/Volume request on the current music.

                if (currentMusic != lastPlayRequestMusicInstance) // need to change the music
                {
                    ResetMusicPlayer();
                    musicActionRequests.Enqueue(new SoundMusicActionRequest(lastPlayRequestMusicInstance, SoundMusicAction.Play));
                    QueueLastPendingRequests(actionRequestedAftLastPlay, lastPlayRequestMusicInstance);
                }
                else if(!currentMusic.IsDisposed)// apply requests on current music.
                {
                    if (shouldRestartCurrentMusic)
                        RestartCurrentMusic();

                    if (shouldStartCurrentMusic)
                        StartCurrentMusic();

                    if (actionRequestedAftLastPlay[SoundMusicAction.Volume])
                        UpdateMusicVolume();

                    if (actionRequestedAftLastPlay[SoundMusicAction.Stop])
                        StopCurrentMusic();
                    else if (actionRequestedAftLastPlay[SoundMusicAction.Pause])
                        PauseCurrentMusic();
                }
                else // current music has been disposed.
                {
                    ResetMusicPlayer();
                }
            }
        }

        private void ProcessMusicEnded()
        {
            if (currentMusic == null || currentMusic.IsDisposed) //this event is asynchronous so the music can be disposed by the user meanwhile.
                return;

            // If the music need to be looped, we start it again.
            if (currentMusic.IsLooped && !currentMusic.ShouldExitLoop && currentMusic.PlayState != SoundPlayState.Stopped)
            {
                RestartCurrentMusic(); // restart the sound to simulate looping.
                StartCurrentMusic();
            }
            // otherwise we set the state of currentMusic to "stopped"
            else
            {
                currentMusic.SetStateToStopped();
            }
        }

        private void ProcessMusicReady()
        {
            PlatformSpecificProcessMusicReady();

            isMusicPlayerReady = true;

            if (!currentMusic.IsDisposed) // disposal of the music can happen between the call to Play and its ready-to-play state notification
            {
                // Adjust the volume before starting to play.
                UpdateMusicVolume();

                // Play the music
                StartCurrentMusic();
            }
        }

        /// <summary>
        /// The music MediaEvents that arrrived since last update.
        /// </summary>
        private readonly ThreadSafeQueue<SoundMusicEventNotification> musicMediaEvents = new ThreadSafeQueue<SoundMusicEventNotification>();

        private bool isMusicPlayerReady;

        private void ProccessQueuedMediaSessionEvents()
        {
            foreach (var eventNotification in musicMediaEvents.DequeueAsList())
            {
                // Since player calls are asynchronous, error status come asynchronously too
                // We need to check here that the cmd executed properly.
                ProcessMusicError(eventNotification);

                switch (eventNotification.Event)
                {
                    case SoundMusicEvent.MetaDataLoaded:
                        ProcessMusicMetaData();
                        break;
                    case SoundMusicEvent.ReadyToBePlayed:
                        ProcessMusicReady();
                        break;
                    case SoundMusicEvent.EndOfTrackReached:
                        ProcessMusicEnded();
                        break;
                    case SoundMusicEvent.MusicPlayerClosed:
                        ProcessPlayerClosed();
                        break;
                }

                // Release the event data if needed
                var dispEventData = eventNotification.EventData as IDisposable;
                if (dispEventData != null)
                    dispEventData.Dispose();
            }
        }
    }
}
