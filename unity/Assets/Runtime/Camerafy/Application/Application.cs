using System;
using System.Threading;

using UnityEngine;

namespace Camerafy.Application
{
    using Config;
    using Event;

    /// <summary>
    /// Global application object. Holds all references to core functionality.
    /// Note: Application must be one of the first MonoBehaviour to be executed.
    /// </summary>
    [DefaultExecutionOrder(1)]
    public partial class Application : MonoBehaviour
    {
        // holds the current instance at runtime
        public static Application Current { get; private set; } = null;
        
        public string ExecutablePath { get { return System.IO.Directory.GetCurrentDirectory(); } }

        public string DataPath { get { return UnityEngine.Application.dataPath; } }

        public string PersistentDataPath { get { return UnityEngine.Application.persistentDataPath; } }

        public Configuration Config { get; private set; } = new Configuration(); // default config

        /// <summary>
        /// Camerafy session reference.
        /// </summary>
        public Session Session = null;

        /// <summary>
        /// Signleton instance to the applications event bus.
        /// </summary>
        private EventBus EventBus = null;        

        private void Awake()
        {
            // load configuration from file, environment vars and command-line
            this.Config = Configuration.Load();

            // grab main gamethread sync context
            this.GamethreadSyncContext = SynchronizationContext.Current;

            // MAKE SURE THE GAMEOBJECT DOES NOT DESTROYED ON SCENE CHANGE!
            DontDestroyOnLoad(this.gameObject);

            try
            {
                if (this.Session == null)
                    throw new Exception("Application.Session is null.");

                // set target frame rate
                UnityEngine.Application.targetFrameRate = this.Config.MaxFps;

                // initialize unity webrtc plugin
                Unity.WebRTC.WebRTC.Initialize(Unity.WebRTC.WebRTC.SupportHardwareEncoder ? Unity.WebRTC.EncoderType.Hardware : Unity.WebRTC.EncoderType.Software);
              
                // Initialy build the Camerafy event library used by the event system
                EventLibrary.Build();

                // initialize the event-bus
                this.EventBus = new EventBus();

                // make this instance the current application instance
                Current = this;
            }
            catch
            {
                // terminate application on fail.
                UnityEngine.Application.Quit();
            }
        }

        private void Start()
        {
            this.StartHttpListener();

            // kick-off unity webrtc update loop
            StartCoroutine(Unity.WebRTC.WebRTC.Update());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();

            this.StopHttpListener();

            // terminate WebRTC
            Unity.WebRTC.Audio.Stop();
            Unity.WebRTC.WebRTC.Dispose();

            if (Current != null)
            {
                // dispose the event bus
                this.EventBus.Dispose();
            }

            // clear runtime instance
            Current = null;
        }
    }
}