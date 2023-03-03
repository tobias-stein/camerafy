using System;
using System.Collections.Generic;
using UnityEngine;

namespace Camerafy.Camera
{
    /// <summary>
    /// The animation player is used to play an animation.
    /// </summary>
    public partial class CameraController
    {
        /// <summary>
        /// Animation drive mode. User or automatic (time).
        /// </summary>
        public enum Drive
        {
            /// <summary>
            /// Animation is driven by elapsed delta time.
            /// </summary>
            Automatic,

            /// <summary>
            /// Animation is driven by user input.
            /// </summary>
            Manual
        };
        
        /// <summary>
        /// Current playing animation.
        /// </summary>
        public Animation Animation = null;

        /// <summary>
        /// Holds a list of animations to play in a sequence.
        /// </summary>
        public List<Animation> AnimationPlaylist = new List<Animation>();

        /// <summary>
        /// Start playing first animation from playlist, if done with list.
        /// </summary>
        public bool LoopAnimationPlaylist = false;

        /// <summary>
        /// Holds the index of the current played animation from playlist.
        /// </summary>
        public int AnimationPlaylistIndex;

        /// <summary>
        /// Fades animations in and out.
        /// </summary>
        public bool UseFadeTransition = true;

        /// <summary>
        /// Shows the progress of the animation. 0.0 means not started, 1.0 means complete.
        /// </summary>
        public float Progress = 0.0f;

        /// <summary>
        /// Currently elapsed seconds.
        /// </summary>
        public float Elapsed = 0.0f;

        /// <summary>
        /// Speed controls how fast the animation will be played.
        /// </summary>
        [Range(0.125f, 8.0f)]
        public float Speed = 1.0f;

        /// <summary>
        /// Current drive mode.
        /// </summary>
        public Drive DriveMode = Drive.Automatic;

        #region FADE

        /// <summary>
        /// Indicates if player is currently fading.
        /// </summary>
        public bool IsFading { get; private set; } = false;

        public enum FadeDirection { In, Out }

        /// <summary>
        /// Simple texture covering entire screenspace which gets faded in and out.
        /// </summary>
        private Texture2D FadeScreenMask;

        /// <summary>
        /// Defines the fade mask color.
        /// </summary>
        public Color FadeColor = Color.black;

        /// <summary>
        /// Current transparecny of the fade mask.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float FadeAlpha = 0.0f;

        /// <summary>
        /// The direction of the fade (in/out).
        /// </summary>
        public FadeDirection FadeDir;

        /// <summary>
        /// The total duration the fade is taking to fade from transparent to full fade color or vice versa.
        /// </summary>
        public float FadeDuration = 1.0f;

        /// <summary>
        /// The current time point of the total duration the fade is currently in.
        /// </summary>
        public float FadeTime;

        /// <summary>
        /// Starts a new fade.
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="direction"></param>
        /// <param name="color"></param>
        public void Fade(float duration, FadeDirection direction, Color color)
        {
            if (this.IsFading)
                this.StopFade();

            this.FadeTime = 0.0f;
            this.FadeDir = direction;
            this.FadeDuration = duration;
            this.FadeColor = color;

            this.IsFading = true;

            // raise begin fade event
            this.OnFadeStarted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Stops the current ongoing fade.
        /// </summary>
        public void StopFade()
        {
            if (!this.IsFading)
                return;

            this.IsFading = false;

            // raise fade end event
            this.OnFadeFinished?.Invoke(this, EventArgs.Empty);
        }

        // A few convenient fade in/out functions.

        public void FadeIn(float duration, Color color) { this.Fade(duration, FadeDirection.In, color); }
        public void FadeIn(float duration = 1.0f) { this.Fade(duration, FadeDirection.In, Color.black); }
        public void FadeOut(float duration, Color color) { this.Fade(duration, FadeDirection.Out, color); }
        public void FadeOut(float duration = 1.0f) { this.Fade(duration, FadeDirection.Out, Color.black); }

        /// <summary>
        /// Udpate fade state.
        /// </summary>
        private void UpdateFade()
        {
            // stop right here, if not fading anything
            if (!this.IsFading)
                return;

            // check if fade is complete
            if (this.FadeTime >= this.FadeDuration)
            {
                this.IsFading = false;

                // raise fade end event
                this.OnFadeFinished?.Invoke(this, EventArgs.Empty);
                return;
            }

            // update fade time
            this.FadeTime = Mathf.Min(this.FadeTime + Time.deltaTime, this.FadeDuration);

            // update fade alpha
            switch (this.FadeDir)
            {
                case FadeDirection.In:
                    this.FadeAlpha = 1.0f - (this.FadeTime / Mathf.Max(1e-4f, this.FadeDuration));
                    break;

                case FadeDirection.Out:
                    this.FadeAlpha = this.FadeTime / Mathf.Max(1e-4f, this.FadeDuration);
                    break;
            }
        }

        /// <summary>
        /// Render and update fade.
        /// </summary>
        private void OnGUI()
        {
            // stop right here, if not fading anything
            if (!this.IsFading)
                return;

            // update fade mask (tween the alpha for better visual effect)
            this.FadeScreenMask.SetPixel(0, 0, this.FadeColor * Mathf.Pow(this.FadeAlpha,  2.0f));
            this.FadeScreenMask.Apply();

            // draw the mask
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), this.FadeScreenMask);
        }

        #endregion

        // Update is called once per frame
        private void UpdateCameraAnimation()
        {
            #region Update Fade

            // todo: steint, 24.03.19: change fade from OnGUI to rendertexture fade on camera, otherwise multiple cameras fades will interfear
            this.UpdateFade();

            #endregion

            #region Update Animation Progress

            // if no animation or play is paused/stopped do nothing
            if (this.Animation == null || !this.State.HasState(CameraControllerState.Animating) || this.Animation.TotalDistance <= float.Epsilon)
                return;

            float dt = Time.deltaTime * this.Speed;

            switch (this.DriveMode)
            {
                case Drive.Automatic:
                {
                    // do not update if not in play mode
                    if(!this.State.HasState(CameraControllerState.Animating))
                        break;

                    // If there are animation queued in list, force player to play animation once.
                    AnimationPlayMode mode = this.AnimationPlaylist.Count > 0 ? AnimationPlayMode.Once : this.Animation.Mode;

                    switch (mode)
                    {
                        case AnimationPlayMode.Loop:
                        {
                            this.Elapsed += dt * (this.Animation.Reversed ? -1.0f : 1.0f);

                            if (this.Elapsed > this.Animation.TotalDuration)
                            {
                                this.Elapsed = this.Elapsed - this.Animation.TotalDuration;
                            }
                            else if (this.Elapsed < 0.0f)
                            {
                                this.Elapsed = this.Animation.TotalDuration + this.Elapsed;
                            }

                            break;
                        }

                        case AnimationPlayMode.PingPong:
                        {
                            this.Elapsed += dt * (this.Animation.Reversed ? -1.0f : 1.0f);

                            if (this.Elapsed > this.Animation.TotalDuration)
                            {
                                this.Elapsed = this.Animation.TotalDuration;
                                this.Animation.Reversed = true;
                            }
                            else if (this.Elapsed < 0.0f)
                            {
                                this.Elapsed = 0.0f;
                                this.Animation.Reversed = false;
                            }

                            break;
                        }

                        case AnimationPlayMode.Once:
                        {
                            this.Elapsed += dt;

                            // if there are animations queued, play next if current animation is almost finsihed. 
                            if(this.AnimationPlaylist.Count > 0 && (this.Animation.TotalDuration - this.Elapsed) <= 1.0f)
                            {
                                bool IsLastAnimation = this.AnimationPlaylistIndex == (this.AnimationPlaylist.Count - 1);
                                if(!IsLastAnimation || this.LoopAnimationPlaylist)
                                {
                                    this.NextAnimation();
                                }
                            }

                            if (this.Elapsed > this.Animation.TotalDuration)
                            {
                                this.StopAnimation();
                                return;
                            }
                            
                            break;
                        }
                    }

                    // update progress
                    this.Progress = this.Elapsed / this.Animation.TotalDuration;
                    break;
                }

                case Drive.Manual:
                {
                    this.Elapsed = this.Progress * this.Animation.TotalDuration;
                    break;
                }
            }

            // apply current animation state to camera.
            {
                // get delta frame
                Snapshot frame = this.DriveMode == Drive.Automatic ? this.Animation.GetFrameByTime(this.Elapsed) : this.Animation.GetFrameByProgress(this.Progress);

                // update camera transform
                this.Camera.transform.SetPositionAndRotation(frame.Position, frame.Rotation);

                // raise update event
                this.OnAnimationUpdated?.Invoke(this, new AnimationUpdatedArgs { Elasped = this.Elapsed, Progress = this.Progress });
            }

            #endregion
        }
    }
}
