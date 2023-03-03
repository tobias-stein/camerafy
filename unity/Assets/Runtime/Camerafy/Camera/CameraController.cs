using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Camerafy.Camera
{
    using Application.Mode;
    using User;    

    #region CAMERA CONTROLLER STATE

    /// <summary>
    /// Possible CameraController states.
    /// </summary>
    [Flags]
    public enum CameraControllerState
    {
        None        = 0,

        /// <summary>
        /// CameraController is idle if no actions happen for a certain amount of time.
        /// </summary>
        Idle        = 1,
    
        /// <summary>
        /// CameraController is currently recording an animation.
        /// </summary>
        Recording   = 2,

        /// <summary>
        /// CameraController is currently playing a keyframe animation.
        /// </summary>
        Animating   = 4,

        /// <summary>
        /// Animation/Recording is currently paused, if this flag is set.
        /// </summary>
        Paused      = 8,
    }

    public static class CameraControllerStateExtensions
    {
        public static bool HasState(this CameraControllerState ccs, CameraControllerState state)
        {
            return (ccs & state) != CameraControllerState.None;
        }

        public static void SetState(this CameraControllerState ccs, CameraControllerState state)
        {
            ccs |= state;
        }

        public static void ClearState(this CameraControllerState ccs, CameraControllerState state)
        {
            ccs &= ~state;
        }
    }

    #endregion

    /// <summary>
    /// Suppoted snapshot image formats.
    /// </summary>
    public enum SnapshotImageFormat
    {
        png,
        tga,
        jpg,
        exr,
        raw
    }

    public class CameraControllerSettings
    {
        public float MinZoom                                                { get; set; } = 0.0f;
        public float MaxZoom                                                { get; set; } = 180.0f;
        public float MaxLinearSpeed                                         { get; set; } = 5.0f;
        public float MaxAngularSpeed                                        { get; set; } = 15.0f;
        public float FieldOfView                                            { get; set; } = 60.0f;
        public float LinearDamping                                          { get; set; } = 1.0f;
        public float AngularDamping                                         { get; set; } = 1.0f;
        public float MoveUnitsPerSecond                                     { get; set; } = 1.0f;
        public float TurnDegreesPerSecond                                   { get; set; } = 15.0f;

        public bool InverseControls                                         { get; set; } = false;

        public Dictionary<Type, CameraBehaviourSettings> CameraBehaviours   { get; set; } = new Dictionary<Type, CameraBehaviourSettings>();
    }

    /// <summary>
    /// The CameraController is the core component and split across multiple partial classes. All controller actions
    /// and events are defined in separat classes. Since the CameraController has an animation and recording feature,
    /// these are out-source to separat partial classes as well.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public partial class CameraController : MonoBehaviour
    {
        private const string ConfigFilename = "Config/DefaultUserCameraSettings.json";

        #region USER 

        public User User = null;

        #endregion

        #region POSSESSED CAMERA 

        /// <summary>
        /// Camera controlled by this controller.
        /// </summary>
        public UnityEngine.Camera Camera { get; private set; }

        #endregion

        #region Camera Thresholds

        public float MinZoom = 0.0f;
        public float MaxZoom = 180.0f;

        public float MaxLinearSpeed = 5.0f;
        public float MaxAngularSpeed = 15.0f;

        public float FieldOfView = 60.0f;

        /// <summary>
        /// Ratio of linear velocity lost each frame. 0.0 means velocity is maintained and
        /// 1.0 means velocity is zero after each frame.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float LinearDamping = 1.0f;

        /// <summary>
        /// Same as for linear damping, but for rotation.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float AngularDamping = 1.0f;

        /// <summary>
        /// How many units the camera will move with velocity of 1.0.
        /// </summary>
        public float MoveUnitsPerSecond = 1.0f;

        /// <summary>
        /// How many degrees the camera will turn with velocity of 1.0.
        /// </summary>
        public float TurnDegreesPerSecond = 15.0f;

        #endregion

        /// <summary>
        /// Current camera zoom. Zoom will be added to current set fov value.
        /// </summary>
        //public float Zoom = 0.0f;

        /// <summary>
        /// Current linear camera velocity. Influenced by user input and camera behaviours.
        /// </summary>
        private Vector3 LinearVelocity = Vector3.zero;

        /// <summary>
        /// Current angular camera velocity. Influenced by user input and camera behaviours.
        /// </summary>
        private Vector3 AngularVelocity = Vector3.zero;

        /// <summary>
        /// Inverse all input controls.
        /// </summary>
        public bool InverseControls = false;

        /// <summary>
        /// User camera target.
        /// </summary>
        private ICameraTarget Target = new Vector3CameraTarget(Vector3.zero);

        /// <summary>
        /// List of all camera behaviours attached to the camera.
        /// </summary>
        public List<CameraBehaviourBase> CameraBehaviours { get; private set; } = new List<CameraBehaviourBase>();

        /// <summary>
        /// Current CameraController state.
        /// </summary>
        public CameraControllerState State { get; set; } = CameraControllerState.Idle;

        // Start is called before the first frame update
        private void Start()
        {
            // initialize camera with default settings
            this.LoadDefaultCameraControllerSettings();

            // grab all required components
            {
                this.Camera = this.GetComponent<UnityEngine.Camera>();
            }

            this.State = CameraControllerState.Idle;

            this.FadeScreenMask = new Texture2D(1, 1);

            // register for event: user mode activated
            this.User.ApplicationMode.OnUserApplicationModeActivated += this.OnUserApplicationModeActivated;
        }

        private void OnDestroy()
        {
            // unregister events
            this.User.ApplicationMode.OnUserApplicationModeActivated -= this.OnUserApplicationModeActivated;

#if UNITY_EDITOR
            // Save Camera Controller to default settings file
            //SaveDefaultCameraControllerSettings();
#endif 


            #region RECORDER

            this.Recording = null;

            #endregion

            #region PLAYER

            this.Animation = null;

            #endregion
        }

        /// <summary>
        /// Listen to any input actions if this is the default user application mode.
        /// </summary>
        /// <param name="mode"></param>
        private void OnUserApplicationModeActivated(IUserApplicationMode mode)
        {
        }


        // Update is called once per frame
        private void Update()
        {
            /** Only update if we are in default mode */
            if (!(this.User.ApplicationMode.ActiveMode is DefaultUserApplicationMode))
                return;

            if (this.ResetCameraRequested)
            {
                this.ResetInternal();
            }

            this.HandleInput();

            if (this.State.HasState(CameraControllerState.Animating))
            {
                this.UpdateCameraAnimation();
            }

            if (this.State.HasState(CameraControllerState.Recording))
            {
                this.UpdateCameraRecording();
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// Fn: private void LateUpdate()
        ///
        /// Summary:    Applies all attached camera behaviours to the camera transform.
        ///
        /// Author: Tobias Stein
        ///
        /// Date:   24/03/2019
        ///-------------------------------------------------------------------------------------------------

        private void LateUpdate()
        {
            // truncate velocity if its magnitude is above max linear speed
            this.LinearVelocity = Vector3.ClampMagnitude(this.LinearVelocity, this.MaxLinearSpeed);
            this.AngularVelocity = Vector3.ClampMagnitude(this.AngularVelocity, this.MaxAngularSpeed);

            #region APPLY BEHAVIOURS

            float behaviourTotalWeight = this.CameraBehaviours.Select(x => { return x.IsActive ? x.Weigth : 0.0f; }).Sum();

            if (behaviourTotalWeight > 1e-6f)
            {
                foreach (CameraBehaviourBase behaviour in this.CameraBehaviours)
                {
                    // skip if weight is to low
                    if (!behaviour.IsActive || behaviour.Weigth < 1e-6f)
                        continue;

                    // apply this behaviour to the current camera transform.
                    behaviour.Apply(
                        this.Camera, 
                        this.Target.TargetLocation,
                        this.LinearVelocity * this.MoveUnitsPerSecond * (this.InverseControls ? -1.0f : 1.0f), 
                        this.AngularVelocity * this.TurnDegreesPerSecond * (this.InverseControls ? -1.0f : 1.0f));
                }
            }

            #endregion

            // apply damping
            this.LinearVelocity = this.LinearVelocity * Mathf.Clamp01(1.0f - this.LinearDamping);
            this.AngularVelocity = this.AngularVelocity * Mathf.Clamp01(1.0f - this.AngularDamping);
        }

        #region SNAPSHOT 

        /// <summary>
        /// Request a snapshot
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="OnSnapshotFinsihed"></param>
        public void RequestCameraSnapshot(int width, int height, SnapshotImageFormat format, Action<byte[]> OnSnapshotFinsihed)
        {
            // trigger snapshot
            StartCoroutine(DoTakeSnapshot(width, height, format, OnSnapshotFinsihed));
        }

        
        /// <summary>
        /// Takes a snapshot at the end of the frame. Returns the raw pixel data via callback.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public IEnumerator DoTakeSnapshot(int width, int height, SnapshotImageFormat format, Action<byte[]> OnSnapshotFinsihed)
        {
            // wait until entire frame is rendered
            yield return new WaitForEndOfFrame();

            // create new texture for final snapshot image
            Texture2D snapshotImage = new Texture2D(width, height, TextureFormat.ARGB32, false);
            RenderTexture RT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, 0);

            int oldCameraCullingMask = this.Camera.cullingMask;
            RenderTexture oldCameraRenderTexture = this.Camera.targetTexture;
            {
                // do not render ui
                this.Camera.cullingMask &= ~( 1 << LayerMask.NameToLayer("UI"));

                // render current camera view to active render texture     
                this.Camera.targetTexture = RT;
                this.Camera.Render();

                // read rendet texture pixels
                RenderTexture oldActiveRenderTexture = RenderTexture.active;
                {
                    RenderTexture.active = this.Camera.targetTexture;
                    snapshotImage.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    snapshotImage.Apply();
                }
                // restore original active render texture
                RenderTexture.active = oldActiveRenderTexture;
            }
            this.Camera.targetTexture = oldCameraRenderTexture;
            this.Camera.cullingMask = oldCameraCullingMask;

            RenderTexture.ReleaseTemporary(RT);

            // return image data
            {
                byte[] data = null;
                switch (format)
                {
                    case SnapshotImageFormat.png:
                        data = snapshotImage.EncodeToPNG();
                        break;
                    case SnapshotImageFormat.tga:
                        data = snapshotImage.EncodeToTGA();
                        break;
                    case SnapshotImageFormat.jpg:
                        data = snapshotImage.EncodeToJPG();
                        break;
                    case SnapshotImageFormat.exr:
                        data = snapshotImage.EncodeToEXR();
                        break;
                    case SnapshotImageFormat.raw:
                        data = snapshotImage.GetRawTextureData();
                        break;
                }

                // return image data
                OnSnapshotFinsihed(data);
            }            
        }

        #endregion

        #region INPUT HANDLER

        /// <summary>
        /// Handels the user input.
        /// </summary>
        public void HandleInput()
        {
            #region Global Commands

            #endregion

            // Do not allow direct camera controll while in Animating mode.
            if (!this.State.HasState(CameraControllerState.Animating))
            {
                this.HandleKeyboard();
                this.HandleMouse();
                this.HandleTouch();
            }
        }

        /// <summary>
        /// Basic keyboard and mouse input mapping to camera controls.
        /// </summary>
        private void HandleKeyboard()
        {
            // Move Forward/Backward - W/S
            if (User.Input.Keyboard.wKey.isPressed) { this.MoveForward(); }
            if (User.Input.Keyboard.sKey.isPressed) { this.MoveBackward(); }

            // Straf Left/Right - A/D
            if (User.Input.Keyboard.aKey.isPressed) { this.MoveLeft(); }
            if (User.Input.Keyboard.dKey.isPressed) { this.MoveRight(); }

            // Move Up/Down - Q/E
            if (User.Input.Keyboard.qKey.isPressed) { this.MoveUp(); }
            if (User.Input.Keyboard.eKey.isPressed) { this.MoveDown(); }

            // Turn Left/Right - Left/Right
            if (User.Input.Keyboard.leftArrowKey.isPressed) { this.TurnLeft(); }
            if (User.Input.Keyboard.rightArrowKey.isPressed) { this.TurnRight(); }

            // Turn Up/Down - Up/Down
            if (User.Input.Keyboard.upArrowKey.isPressed) { this.TurnUp(); }
            if (User.Input.Keyboard.downArrowKey.isPressed) { this.TurnDown(); }

            // Calibrate (reset all rotations and zoom) - Space
            if (User.Input.Keyboard.spaceKey.isPressed) { this.Reset(); }
        }

        private void HandleMouse()
        {
            // rotate if lmb down and moving
            if (User.Input.Mouse.leftButton.isPressed)
            {
                var delta = User.Input.Mouse.delta.ReadValue();
                this.Turn(delta.y, -delta.x);
            }

            // zoom if mouse wheel used
            var scrollY = User.Input.Mouse.scroll.y.ReadValue();
            if (scrollY > 0.0f) { this.MoveForward(); } else if (scrollY < 0.0f) { this.MoveBackward(); }

            // Calibrate (reset all rotations and zoom)
            if (User.Input.Mouse.middleButton.isPressed) { this.Reset(); }
        }

        private float TouchLastDistance = -1.0f;
        private float TouchLastZoom = 0.0f;

        private void HandleTouch()
        {
            // if we got two touches
            if (User.Input.Touch.touches[0].press.isPressed && User.Input.Touch.touches[1].press.isPressed)
            {
                Vector2 a = User.Input.Touch.touches[0].position.ReadValue();
                Vector2 b = User.Input.Touch.touches[1].position.ReadValue();

                float dist = Vector2.Distance(a, b);
                float zoom = this.TouchLastZoom;
                {
                    if (this.TouchLastDistance > -1.0f)
                    {
                        if (dist < this.TouchLastDistance)
                        {
                            zoom = -1.0f;
                        }
                        else if (dist > this.TouchLastDistance)
                        {
                            zoom = +1.0f;
                        }
                    }
                }

                if (zoom != 0.0f) { if (zoom < 0.0f) { this.MoveBackward(); } else { this.MoveForward(); } }

                this.TouchLastDistance = dist;
                this.TouchLastZoom = zoom;
            }
            else if (User.Input.Touch.touches[0].press.isPressed)
            {
                this.TouchLastDistance = -1.0f;
                this.TouchLastZoom = 0.0f;

                Vector2 touchDelta = User.Input.Touch.touches[0].delta.ReadValue();
                this.Turn(touchDelta.y, -touchDelta.x);
            }
            else
            {
                this.TouchLastDistance = -1.0f;
                this.TouchLastZoom = 0.0f;
            }
        }

        #endregion

        #region LOAD/SAVE CONTROLLER SETTINGS

        private void LoadDefaultCameraControllerSettings()
        {
            // load settings.
            var settings = Serialization.EntityManager.LoadEntity<CameraControllerSettings>(new System.IO.FileInfo(ConfigFilename).FullName);

            this.InverseControls        = settings.InverseControls;
            this.LinearDamping          = Mathf.Clamp01(settings.LinearDamping);
            this.AngularDamping         = Mathf.Clamp01(settings.AngularDamping);
            this.FieldOfView            = Mathf.Clamp(settings.FieldOfView, 0.0f, 180.0f);
            this.MaxLinearSpeed         = Mathf.Max(settings.MaxLinearSpeed, 0.0f);
            this.MaxAngularSpeed        = Mathf.Max(settings.MaxAngularSpeed, 0.0f);
            this.MinZoom                = Mathf.Max(settings.MinZoom, 0.0f);
            this.MaxZoom                = Mathf.Max(settings.MaxZoom, 0.0f);
            this.MoveUnitsPerSecond     = Mathf.Max(settings.MoveUnitsPerSecond, 0.0f);
            this.TurnDegreesPerSecond   = Mathf.Max(settings.TurnDegreesPerSecond, 0.0f);

            // initialize default behaviours
            this.CameraBehaviours.Clear();
            foreach (var kvp in settings.CameraBehaviours)
            {
                if (kvp.Key == typeof(Free))
                {
                    var DefaultBehaviour = this.gameObject.AddComponent<Free>();
                    DefaultBehaviour.ApplySettings(kvp.Value as FreeBehaviourSettings);
                    this.CameraBehaviours.Add(DefaultBehaviour);
                }
                else if (kvp.Key == typeof(Orbit))
                {
                    var DefaultBehaviour = this.gameObject.AddComponent<Orbit>();
                    DefaultBehaviour.ApplySettings(kvp.Value as OrbitBehaviourSettings);
                    this.CameraBehaviours.Add(DefaultBehaviour);
                }
                else if (kvp.Key == typeof(Lock))
                {
                    var DefaultBehaviour = this.gameObject.AddComponent<Lock>();
                    DefaultBehaviour.ApplySettings(kvp.Value as LockBehaviourSettings);
                    this.CameraBehaviours.Add(DefaultBehaviour);
                }
            }
        }

        private void SaveDefaultCameraControllerSettings()
        {
            var settings = new CameraControllerSettings
            {
                CameraBehaviours        = new Dictionary<Type, CameraBehaviourSettings>(),
                InverseControls         = this.InverseControls,
                LinearDamping           = this.LinearDamping,
                AngularDamping          = this.AngularDamping,
                FieldOfView             = this.FieldOfView,
                MaxLinearSpeed          = this.MaxLinearSpeed,
                MaxAngularSpeed         = this.MaxAngularSpeed,
                MinZoom                 = this.MinZoom,
                MaxZoom                 = this.MaxZoom,
                MoveUnitsPerSecond      = this.MoveUnitsPerSecond,
                TurnDegreesPerSecond    = this.TurnDegreesPerSecond
            };

            foreach(var behaviour in this.CameraBehaviours)
            {
                settings.CameraBehaviours.Add(behaviour.GetType(), behaviour.GetSettings());
            }

            // save settings.
            Serialization.EntityManager.SaveEntity(settings, new System.IO.FileInfo(ConfigFilename).FullName);
        }

        #endregion 
    }
}