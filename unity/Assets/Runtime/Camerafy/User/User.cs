using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Camerafy.User
{
    using Application;
    using Application.Mode;
    using Camera;
    using Input;
    using Network;

    public enum UserStatus
    {
        Uninitialized,

        Initializing,
        Initialized,

        Connecting,
        Connected,

        Disconnecting,
        Disconnected,

        Failed
    }

    /// <summary>
    /// This class will hold the users login data, if user login was provided.
    /// </summary>
    public class UserLoginData
    {
        public string       UUID        { get; set; } = "";
        public int?         UserId      { get; set; } = null;
        public string       UserName    { get; set; } = "";
        public List<string> Groups      { get; set; } = new List<string>();

        public bool Valid()             { return !string.IsNullOrWhiteSpace(this.UUID); }
        public bool Authenticated()     { return this.UserId != null && this.UserId.HasValue; }

        public override string ToString()
        {
            return string.Format("UserLoginData: [ID: {0}; Name: {1}; Groups: {2}]", 
                this.UserId.GetValueOrDefault(-1), 
                this.UserName, 
                String.Join(",", this.Groups));
        }
    }

    /// <summary>
    /// Represents an active session user.
    /// </summary>
    public partial class User : PeerConnection
    {
        #region STATES

        /// <summary>
        /// Current status.
        /// </summary>
        public UserStatus UserStatus = UserStatus.Uninitialized;

        /// <summary>
        /// User specific application mode mangaer.
        /// </summary>
        public UserApplicationModeManager ApplicationMode = new UserApplicationModeManager();

        #endregion

        #region BACKEND DATA

        /// <summary>
        /// Backend provided user login data.
        /// </summary>
        public UserLoginData LoginData;

        #endregion

        #region CAMERA

        /// <summary>
        /// User camera.
        /// </summary>
        public CameraController UserCameraController;

        #endregion

        #region INPUT

        /// <summary>
        /// Reference to the user input module.
        /// </summary>
        public RemoteInputModule Input;

        #endregion

        #region RENDERING

        public int MaxFps;
        public int FrameWidth;
        public int FrameHeight;

        #endregion

        #region FPS STATS

        private float FPS_CurrentIntervallTime = 0.0f;
        private long FPS_CurrentIntervallFrameCount = 0;

        private Queue<float> FPS_IntervallAvgHistory = new Queue<float>(new float[] { 0.0f });

        private void ResetCurrentIntervall()
        {
            this.FPS_IntervallAvgHistory.Enqueue(this.FPS_CurrentIntervallFrameCount);
            if (this.FPS_IntervallAvgHistory.Count > 10)
                this.FPS_IntervallAvgHistory.Dequeue();

            this.FPS_CurrentIntervallTime = 0.0f;
            this.FPS_CurrentIntervallFrameCount = 0;
        }

        public float AverageFramesPerSecond
        {
            get
            {
                return this.FPS_IntervallAvgHistory.Average();
            }
        }

        #endregion

        /// <summary>
        /// Called by the session spawner, after the User object has been fully constructed.
        /// </summary>
        public override void Initialize(string InUserId)
        {
            base.Initialize(InUserId);

            this.UserStatus = UserStatus.Initializing;

            // set defaults
            {
                this.MaxFps = Application.Current.Config.MaxFps;

                // initialize frame size with session maximum allowed size
                this.FrameWidth = Application.Current.Config.MaxFrameWidth;
                this.FrameHeight = Application.Current.Config.MaxFrameHeight;
            }

            StartCoroutine(this.InitializeIntern());
            StartCoroutine(this.OnEndOfFrame());
        }

        private void Update()
        {
            // update frame counter
            this.FPS_CurrentIntervallTime += Time.unscaledDeltaTime;
            if (this.FPS_CurrentIntervallTime >= 1.0f)
            {
                this.ResetCurrentIntervall();
            }

            // hack: steint, 04/10.2019: auto-rotate feature if user is inactive
            //if (this.IsInactive)
            //{
            //    this.UserCameraController.TurnLeft();
            //}
        }

        private IEnumerator InitializeIntern()
        {
            yield return new WaitForEndOfFrame();

            // activate default mode
            this.ApplicationMode.Activate<DefaultUserApplicationMode>();

            // send initial camera values to frontend
            {
                var CameraData = new
                {
                    MaxLinearSpeed = this.UserCameraController.MaxLinearSpeed,
                    MaxAngularSpeed = this.UserCameraController.MaxAngularSpeed,
                    LinearDamping = this.UserCameraController.LinearDamping,
                    AngularDamping = this.UserCameraController.AngularDamping,
                    InvertedControls = this.UserCameraController.InverseControls
                };
                // send data
                this.InitialCameraValues(Serialization.EntityManager.ToString(CameraData));
            }

            // user is now initialized
            this.UserStatus = UserStatus.Initialized;

            /* After we are done initializing the clients user object, we can signal the
             * client that we are ready to receive the webrtc offer.
             */
            this.ClientServerWebrtcReady();
        }

        private void OnDestroy()
        {
            Logger.Debug($"Remove user {this.Id}");

            StopAllCoroutines();

            this.Dispose();
        }
        
        /// <summary>
        /// Called when final frame was rendered.
        /// </summary>
        /// <returns></returns>
        private IEnumerator OnEndOfFrame()
        {
            while (true)
            {
                // always wait until the end of a frame
                yield return new WaitForEndOfFrame();

                // if user is not yet connected bale out here
                if (this.UserStatus != UserStatus.Connected)
                    continue;

                this.FPS_CurrentIntervallFrameCount++;
            }
        }
    }
}