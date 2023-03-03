using System;
using UnityEngine;

namespace Camerafy.Camera
{
    using Event;

    /// <summary>
    /// Atomar camera controls.
    /// </summary>
    public partial class CameraController
    {
        #region CAMERA LIMITS

        [CamerafyEvent]
        public void SetMaxLinearSpeed(float InMaxLinearSpeed) { this.MaxLinearSpeed = Math.Max(0.0f, InMaxLinearSpeed); }

        [CamerafyEvent]
        public void SetMaxAngularSpeed(float InMaxAngularSpeed) { this.MaxAngularSpeed = Math.Max(0.0f, InMaxAngularSpeed); }

        [CamerafyEvent]
        public void SetInvertControls(bool InState) { this.InverseControls = InState; }

        [CamerafyEvent]
        public void SetLinearDamping(float InDamping) { this.LinearDamping = Mathf.Clamp01(InDamping); }

        [CamerafyEvent]
        public void SetAngularDamping(float InDamping) { this.AngularDamping = Mathf.Clamp01(InDamping); }

        [CamerafyEvent]
        public void SetCameraTarget(float InX, float InY, float InZ) { this.Target = new Vector3CameraTarget(new Vector3(InX, InY, InZ)); }

        public void SetCameraTarget(Vector3 InVector3) { this.Target = new Vector3CameraTarget(InVector3); }
        public void SetCameraTarget(Transform InTransform) { this.Target = new TranformCameraTarget(InTransform); }

        [CamerafyEvent]
        public void SetCameraTargetByName(string InTargetName)
        {
            ICameraTarget NewTarget = null;
            if (CameraController.GlobalCameraTarget.TryGetValue(InTargetName, out NewTarget))
            {
                this.Target = NewTarget;
            }
        }

        #endregion

        #region BASE CAMERA MOVE, TURN & ZOOM

        public void MoveUp() { this.LinearVelocity += Vector3.up; }
        public void MoveDown() { this.LinearVelocity += Vector3.down; }
        public void MoveLeft() { this.LinearVelocity += Vector3.left; }
        public void MoveRight() { this.LinearVelocity += Vector3.right; }
        public void MoveForward() { this.LinearVelocity += Vector3.forward; }
        public void MoveBackward() { this.LinearVelocity -= Vector3.forward; }

        public void TurnUp() { this.AngularVelocity.x -= 1.0f;}
        public void TurnDown() { this.AngularVelocity.x += 1.0f; }
        public void TurnLeft() { this.AngularVelocity.y -= 1.0f; }
        public void TurnRight() { this.AngularVelocity.y += 1.0f; }
        public void RollLeft() { throw new System.NotImplementedException(); }
        public void RollRight() { throw new System.NotImplementedException(); }

        public void Move(float deltaX, float deltaY, float deltaZ = 0.0f) { this.LinearVelocity += new Vector3(deltaX, deltaY, deltaZ).normalized; }
        public void Move(Vector3 delta) { this.LinearVelocity += new Vector3(delta.x, delta.y, delta.z).normalized; }

        public void Turn(float deltaX, float deltaY, float deltaZ = 0.0f) { this.AngularVelocity += new Vector3(deltaX, deltaY, deltaZ).normalized; }
        public void Turn(Vector3 delta) { this.AngularVelocity += new Vector3(delta.x, delta.y, delta.z).normalized; }


        //public void ZoomIn() { this.Zoom += 1.0f; }
        //public void ZoomOut() { this.Zoom -= 1.0f; }
        //public void Zoom(float value) { this.Zoom = value; }

        private bool ResetCameraRequested = false;

        public void Reset() { this.ResetCameraRequested = true; }

        private void ResetInternal()
        {
            // reset zoom
            //this.Zoom = 0.0f;

            // reset velocities
            this.LinearVelocity = Vector3.zero;
            this.AngularVelocity = Vector3.zero;

            this.Camera.fieldOfView = this.FieldOfView;
            this.Camera.transform.rotation = Quaternion.identity;

            this.ResetCameraRequested = false;
        }

        #endregion

        #region CHANGE CAMERA BEHAVIOUR

        [CamerafyEvent]
        public void SetFreeBehaviour(float Weight)
        {
            Free Behaviour = null;
            foreach (var b in this.CameraBehaviours)
            {
                if (b.GetType() == typeof(Free))
                {
                    Behaviour = b as Free;
                    break;
                }
            }

            if (Behaviour != null)
            {
                Behaviour.Weigth = Weight;
            }
        }

        [CamerafyEvent]
        public void SetOrbitBehaviour(float Weight)
        {
            Orbit Behaviour = null;
            foreach (var b in this.CameraBehaviours)
            {
                if (b.GetType() == typeof(Orbit))
                {
                    Behaviour = b as Orbit;
                    break;
                }
            }

            if (Behaviour != null)
            {
                Behaviour.Weigth = Weight;
            }
        }

        #endregion // CHANGE CAMERA BEHVIOUR

        #region RECORDER

        public void StartRecording()
        {
            // create a new recording
            this.Recording = new Animation();

            // set recording state
            this.State.SetState(CameraControllerState.Recording);
            this.State.ClearState(CameraControllerState.Paused);

            // raise event
            this.OnRecordingStarted?.Invoke(this, EventArgs.Empty);
        }

        public void PauseRecording()
        {
            this.State.SetState(CameraControllerState.Paused);

            // raise event
            this.OnRecordingPaused?.Invoke(this, EventArgs.Empty);
        }

        public void ResumeRecording()
        {
            this.State.ClearState(CameraControllerState.Paused);

            // raise event
            this.OnRecordingResumed?.Invoke(this, EventArgs.Empty);
        }

        public void StopRecording()
        {
            this.State.ClearState(CameraControllerState.Recording);
            this.State.ClearState(CameraControllerState.Paused);

            // raise event
            this.onRecordingStopped?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Get current camera values and create an new snapshot.
        /// </summary>
        public void TakeSnapshot()
        {
            StartCoroutine(TakeSnapshot(this.SnapshowPreviewWidth, this.SnapshowPreviewHeight));
        }

        public void RemoveSnapshot(Snapshot snapshot)
        {
            // remove this snapshot from recording
            this.Recording.RemoveSnapshot(snapshot);

            // raise event
            this.OnSnapshotRemoved?.Invoke(this, new SnapshotRemovedArgs { Snapshot = snapshot });
        }

        /// <summary>
        /// Persist recording as new asset.
        /// </summary>
        /// <param name="name"></param>
        public void SaveRecording(string name, float duration, AnimationPlayMode mode)
        {
            string savePath = System.IO.Path.Combine("Recordings", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, name);

            this.Recording.Name = name;
            this.Recording.TotalDuration = duration;
            this.Recording.Mode = mode;

            Serialization.EntityManager.SaveEntity(this.Recording, savePath);

            this.Recording = null;

            this.State.ClearState(CameraControllerState.Recording);
            this.State.ClearState(CameraControllerState.Paused);

            // raise event
            this.OnRecordingSaved?.Invoke(this, new RecordingSavedArgs { SaveName = savePath });
        }

        /// <summary>
        /// Discar recording. No save.
        /// </summary>
        public void DiscradRecording()
        {
            this.Recording = null;

            this.State.ClearState(CameraControllerState.Recording);
            this.State.ClearState(CameraControllerState.Paused);

            // raise event
            this.OnRecordingDiscard?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Enables/Disables auto recording.
        /// </summary>
        /// <param name="state"></param>
        public void SetAutoRecording(bool state)
        {
            this.AutoRecording = state;

            // raise event
            this.OnAutoRecordingChanged?.Invoke(this, new AutoRecordingChangedArgs { IsAutoRecording = state });
        }

        /// <summary>
        /// Sets the camera values to the given snapshot.
        /// </summary>
        /// <param name="snapshot"></param>
        public void SnapCameraToSnapshot(Snapshot snapshot)
        {
            this.Camera.transform.SetPositionAndRotation(snapshot.Position, snapshot.Rotation);
        }

        #endregion

        #region PLAYER


        /// <summary>
        /// Play new animation.
        /// </summary>
        /// <param name="animationName"></param>
        public void PlayAnimation(string animationName)
        {
            string loadPath = System.IO.Path.Combine("Recordings", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, animationName);

            this.PlayAnimation(Serialization.EntityManager.LoadEntity<Animation>(loadPath));
        }

        /// <summary>
        /// Play new animation.
        /// </summary>
        /// <param name="animation"></param>
        public void PlayAnimation(Animation animation)
        {
            this.Animation = animation ?? throw new System.Exception("Invalid animation parameter.");

            this.State.SetState(CameraControllerState.Animating);
            this.State.ClearState(CameraControllerState.Paused);

            // raise play event
            OnAnimationStarted?.Invoke(this, new AnimationStartedArgs { Animation = this.Animation });
        }

        /// <summary>
        /// Pause animation.
        /// </summary>
        public void PauseAnimation()
        {
            // do nothing if player is currently fading
            if (this.IsFading)
                return;

            this.State.SetState(CameraControllerState.Paused);

            // raise pause event
            OnAnimationPaused?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Resume playing animation.
        /// </summary>
        public void ResumeAnimation()
        {
            // do nothing if player is currently fading
            if (this.IsFading)
                return;

            this.State.ClearState(CameraControllerState.Paused);

            // raise play event
            OnAnimationStarted?.Invoke(this, new AnimationStartedArgs { Animation = this.Animation });
        }

        /// <summary>
        /// Pause animation.
        /// </summary>
        public void StopAnimation()
        {
            // do nothing if player is currently fading
            if (this.IsFading)
                return;

            // reset progress
            this.Progress = 0.0f;
            this.Elapsed = 0.0f;

            this.State.ClearState(CameraControllerState.Animating);
            this.State.ClearState(CameraControllerState.Paused);

            // raise stop event
            OnAnimationStopped?.Invoke(this, EventArgs.Empty);

            // if we had a playlist, singal end
            if (this.AnimationPlaylist.Count > 0)
            {
                this.OnPlaylistEnd?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Rewinds the animation by 1/TotalDuration time.
        /// </summary>
        public void ForwardAnimation()
        {
            // do nothing if player is currently fading
            if (this.IsFading)
                return;

            switch (this.DriveMode)
            {
                case Drive.Manual:
                    {
                        this.Progress = Mathf.Min(this.Progress + 0.01f, 1.0f);
                        this.Elapsed = this.Progress * this.Animation.TotalDuration;
                        break;
                    }

                case Drive.Automatic:
                    {
                        this.Elapsed = Mathf.Min(this.Elapsed + Mathf.Max(float.Epsilon, this.Animation.TotalDuration * 0.01f), this.Animation.TotalDuration);
                        this.Progress = this.Elapsed / this.Animation.TotalDuration;
                        break;
                    }
            }

            // raise update event
            this.OnAnimationUpdated?.Invoke(this, new AnimationUpdatedArgs { Elasped = this.Elapsed, Progress = this.Progress });
        }

        /// <summary>
        /// Advances the animation by 1/TotalDuration time.
        /// </summary>
        public void BackwardAnimation()
        {
            // do nothing if player is currently fading
            if (this.IsFading)
                return;

            switch (this.DriveMode)
            {
                case Drive.Manual:
                    {
                        this.Progress = Mathf.Max(this.Progress - 0.01f, 0.0f);
                        this.Elapsed = this.Progress * this.Animation.TotalDuration;
                        break;
                    }

                case Drive.Automatic:
                    {
                        this.Elapsed = Mathf.Max(this.Elapsed - Mathf.Max(float.Epsilon, this.Animation.TotalDuration * 0.01f), 0.0f);
                        this.Progress = this.Elapsed / this.Animation.TotalDuration;
                        break;
                    }
            }

            // raise update event
            this.OnAnimationUpdated?.Invoke(this, new AnimationUpdatedArgs { Elasped = this.Elapsed, Progress = this.Progress });
        }

        /// <summary>
        /// Play previous animation.
        /// </summary>
        public void PreviousAnimation()
        {
            // do nothing if currently fading
            if (this.IsFading)
                return;

            // if playlist available
            if (this.AnimationPlaylist.Count > 0)
            {
                // get index to previous animation
                this.AnimationPlaylistIndex = Mathf.Max(this.AnimationPlaylistIndex - 1, 0);

                // start transition
                this.Transition(this.Animation, this.AnimationPlaylist[this.AnimationPlaylistIndex]);
            }
            // else restart current animaiton
            else
            {
                // start transition
                this.Transition(this.Animation, this.Animation);
            }
        }

        /// <summary>
        /// Play next animation.
        /// </summary>
        public void NextAnimation()
        {
            // do nothing if currently fading
            if (this.IsFading)
                return;

            // if playlist available
            if (this.AnimationPlaylist.Count > 0)
            {
                // get index to previous animation
                this.AnimationPlaylistIndex = this.LoopAnimationPlaylist ? (this.AnimationPlaylistIndex + 1) % this.AnimationPlaylist.Count : Mathf.Min(this.AnimationPlaylistIndex + 1, this.AnimationPlaylist.Count - 1);

                // start transition
                this.Transition(this.Animation, this.AnimationPlaylist[this.AnimationPlaylistIndex]);
            }
            // else restart current animaiton
            else
            {
                // start transition
                this.Transition(this.Animation, this.Animation);
            }
        }

        /// <summary>
        /// Transtion from one animation to another.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        private void Transition(Animation from, Animation to)
        {
            // sanity check.
            if (from == null || !from.IsValid() || to == null || !to.IsValid())
                return;

            if (this.UseFadeTransition)
            {
                // fade out current animation
                this.FadeOut(Mathf.Min(1.0f, this.Animation.TotalDuration - this.Elapsed));
                EventHandler onFadeInEnd = null;
                onFadeInEnd = (o, e) =>
                {
                    // change animation
                    this.Animation = to;

                    // raise play event
                    this.OnAnimationStarted?.Invoke(this, new AnimationStartedArgs { Animation = this.Animation });

                    // reset current animation progress
                    this.Progress = 0.0f;
                    this.Elapsed = 0.0f;

                    // remove this delegate from OnFadeEnd event
                    this.OnFadeFinished -= onFadeInEnd;

                    // fade back in
                    this.FadeIn();
                };
                this.OnFadeFinished += onFadeInEnd;
            }
            // no fade
            else
            {
                // change animation
                this.Animation = to;

                // reset current animation progress
                this.Progress = 0.0f;
                this.Elapsed = 0.0f;
            }
        }

        /// <summary>
        /// Changes the play mode of the current set animation.
        /// </summary>
        /// <param name="newMode"></param>
        public void ChangeAnimationPlayMode(AnimationPlayMode newMode)
        {
            if (this.Animation == null || this.Animation.Mode == newMode)
                return;

            // set new mode
            this.Animation.Mode = newMode;

            // raise mode change event
            this.OnAnimationPlayModeChanged?.Invoke(this, new AnimationPlayModeChangedArgs { Mode = newMode });
        }

        /// <summary>
        /// Change animation play speed.
        /// </summary>
        /// <param name="newSpeed"></param>
        public void ChangeSpeed(float newSpeed)
        {
            if (newSpeed < 0.0f)
                return;

            this.Speed = newSpeed;

            // raise speed change event
            this.OnAnimationSpeedChanged?.Invoke(this, new AnimationSpeedChangedArgs { Speed = newSpeed });
        }

        /// <summary>
        /// Add an animation to the playlist.
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="playNext"></param>
        public void AddToPlaylist(Animation animation, bool playNext = false)
        {
            if (playNext)
            {
                this.AnimationPlaylist.Insert(1, animation);
            }
            else
            {
                this.AnimationPlaylist.Add(animation);
            }
        }

        /// <summary>
        /// Clears the current playlist.
        /// </summary>
        public void ClearPlaylist()
        {
            // prevent playlist from being cleared when currently fading
            if (this.IsFading)
                return;

            this.AnimationPlaylist.Clear();
            this.AnimationPlaylistIndex = 0;
        }

        public void SetAnimationSpeed(float speed)
        {
            this.Speed = speed;
        }

        public void SetAnimationProgress(float progress)
        {
            switch (this.DriveMode)
            {
                case Drive.Manual:
                    this.Progress = Mathf.Clamp01(progress);
                    break;

                case Drive.Automatic:
                    this.Elapsed = Mathf.Clamp01(progress) * this.Animation.TotalDuration;
                    break;
            }
        }

        public void SetAnimationTime(float seconds)
        {
            switch (this.DriveMode)
            {
                case Drive.Manual:
                    this.Progress = Mathf.Clamp01(Mathf.Clamp(seconds, 0.0f, this.Animation.TotalDuration) / this.Animation.TotalDuration);
                    break;

                case Drive.Automatic:
                    this.Elapsed = Mathf.Clamp(seconds, 0.0f, this.Animation.TotalDuration);
                    break;
            }
        }

        public void SetAnimationMode(AnimationPlayMode mode)
        {
            this.Animation.Mode = mode;
        }

        #endregion
    }
}