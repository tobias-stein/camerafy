using System.Collections;
using UnityEngine;

namespace Camerafy.Camera
{
    /// <summary>
    /// An recorder is used to listen to user input and take camera value snapshots.
    /// Later these snapshots are used to create an animation.
    /// </summary>
    public partial class CameraController
    {        
        /// <summary>
        /// Current taken recording.
        /// </summary>
        public Animation Recording = null;

        public int SnapshowPreviewWidth = 200;
        public int SnapshowPreviewHeight = 100;

        /// <summary>
        /// If enabled recorder will take snapshots automatically when camera is moved around.
        /// </summary>
        public bool AutoRecording = false;

        // Update is called once per frame
        void UpdateCameraRecording()
        {
            if (this.AutoRecording && this.DoAutoSnapshot())
            {
                this.StartCoroutine(this.TakeSnapshot(this.SnapshowPreviewWidth, this.SnapshowPreviewHeight));
            }
        }

        /// <summary>
        /// Takes a snapshot at the end of the frame.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public IEnumerator TakeSnapshot(int width, int height)
        {
            // wait until entire frame is rendered
            yield return new WaitForEndOfFrame();

            // stop right here, if recording is done.
            if (this.Recording == null)
                yield break;

            Texture2D snapshotImage = null;

            // store original active render texture
            RenderTexture originalRenderTarget = RenderTexture.active;            
            {
                // if camera has no render taget we create a temporary
                RenderTexture RT = null;
                if (this.Camera.targetTexture == null)
                {
                    
                    RT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                    this.Camera.targetTexture = RT;
                }

                // create new texture for final snapshot image
                snapshotImage = new Texture2D(RT.width, RT.height, TextureFormat.ARGB32, false);

                // render current camera view to active render texture
                RenderTexture.active = this.Camera.targetTexture;
                this.Camera.Render();

                // read rendet texture pixels
                snapshotImage.ReadPixels(new Rect(0, 0, RT.width, RT.height), 0, 0);
                snapshotImage.Apply();

                // restore original active render texture
                RenderTexture.active = originalRenderTarget;

                // release temp render texture
                if (RT != null)
                {
                    this.Camera.targetTexture = null;
                    RenderTexture.ReleaseTemporary(RT);
                }
            }

            // resize to target size
            if(snapshotImage.width != width && snapshotImage.height != height)
                snapshotImage.Resize(width, height);

            // Encode texture into PNG
            byte[] bytes = snapshotImage.EncodeToPNG();

            Snapshot snapshot = new Snapshot
            {
                Position = this.Camera.transform.position,
                Rotation = this.Camera.transform.rotation,
                FieldOfView = this.Camera.fieldOfView,
                Preview = new Snapshot.Thumbnail
                {
                    ThumbnailWidth = width,
                    ThumbnailHeight = height,
                    Image = bytes
                }
            };

            
            this.Recording.AddSnapshot(snapshot);

            // raise event
            this.OnSnapshopTaken?.Invoke(this, new SnapshotTakenArgs { Snapshot = snapshot });
        }

        /// <summary>
        /// Determines if current camera values differ enough to take a new snapshot.
        /// </summary>
        /// <returns></returns>
        private bool DoAutoSnapshot()
        {
            if (this.Recording != null && this.Recording.Snapshots.Count > 0)
            {
                Snapshot lastSnapshot = this.Recording.Snapshots[this.Recording.Snapshots.Count - 1];

                // current camera values
                Transform cameraTransform = this.Camera.transform;

                // distance changed?
                if (Vector3.Distance(lastSnapshot.Position, cameraTransform.position) > 1.0f)
                    return true;

                // rotation changed?
                if (Vector3.Distance(lastSnapshot.Rotation.eulerAngles, cameraTransform.eulerAngles) > 5.0f)
                    return true;

                // field of view changed?
                if (Mathf.Abs(lastSnapshot.FieldOfView - this.Camera.fieldOfView) > 1.0f)
                    return true;
            }
            else
            {
                // there is no snapshot taken yet, take one.
                return true;
            }

            return false;
        }
    }
}