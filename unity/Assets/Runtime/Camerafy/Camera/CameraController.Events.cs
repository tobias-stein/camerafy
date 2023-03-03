using System;

namespace Camerafy.Camera
{
    public partial class CameraController
    {
        #region RECORDER

        public event EventHandler OnRecordingStarted;

        public event EventHandler OnRecordingPaused;

        public event EventHandler OnRecordingResumed;

        public event EventHandler onRecordingStopped;

        #region OnSnapshotTaken

        public class SnapshotTakenArgs : EventArgs
        {
            public Snapshot Snapshot { get; set; }
        }

        public event EventHandler<SnapshotTakenArgs> OnSnapshopTaken;

        #endregion

        #region OnSnapshotRemoved

        public class SnapshotRemovedArgs : EventArgs
        {
            public Snapshot Snapshot { get; set; }
        }

        public event EventHandler<SnapshotRemovedArgs> OnSnapshotRemoved;

        #endregion

        #region OnAutoRecordingChanged

        public class AutoRecordingChangedArgs : EventArgs
        {
            public bool IsAutoRecording { get; set; }
        }

        public event EventHandler<AutoRecordingChangedArgs> OnAutoRecordingChanged;

        #endregion

        public event EventHandler OnRecordingDiscard;

        #region OnRecordingSaved

        public class RecordingSavedArgs : EventArgs
        {
            public string SaveName { get; set; }
        }

        public event EventHandler<RecordingSavedArgs> OnRecordingSaved;

        #endregion

        #endregion

        #region PLAYER

        #region OnAnimationStarted

        public class AnimationStartedArgs : EventArgs
        {
            public Animation Animation { get; set; }
        }

        public event EventHandler<AnimationStartedArgs> OnAnimationStarted;

        #endregion

        public event EventHandler OnAnimationPaused;

        public event EventHandler OnAnimationStopped;

        #region OnAnimationUpdated

        public class AnimationUpdatedArgs : EventArgs
        {
            public float Elasped { get; set; }
            public float Progress { get; set; }
        }

        public event EventHandler<AnimationUpdatedArgs> OnAnimationUpdated;

        #endregion

        #region OnAnimationPlayModeChanged

        public class AnimationPlayModeChangedArgs : EventArgs
        {
            public AnimationPlayMode Mode { get; set; }
        }

        public event EventHandler<AnimationPlayModeChangedArgs> OnAnimationPlayModeChanged;

        #endregion

        #region OnAnimationPlayModeChanged

        public class AnimationSpeedChangedArgs : EventArgs
        {
            public float Speed { get; set; }
        }

        public event EventHandler<AnimationSpeedChangedArgs> OnAnimationSpeedChanged;

        #endregion

        public event EventHandler OnFadeStarted;

        public event EventHandler OnFadeFinished;

        public event EventHandler OnPlaylistEnd;

        #endregion
    }
}