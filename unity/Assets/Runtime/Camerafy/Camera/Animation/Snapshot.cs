using System;
using UnityEngine;

namespace Camerafy.Camera
{
    /// <summary>
    /// Represents a snapshot in time of certain camera values.
    /// </summary>
    [Serializable]
    public class Snapshot
    {
        [Serializable]
        public class Thumbnail
        {
            public int ThumbnailWidth = 0;
            public int ThumbnailHeight = 0;
            public byte[] Image; // png
        }

        public Vector3 Position = Vector3.zero;

        public Quaternion Rotation = Quaternion.identity;

        public float FieldOfView = 60.0f;

        public Thumbnail Preview = new Thumbnail();

        /// <summary>
        /// Returns a linearly interpolated value between 'from' and 'to' with respect
        /// to 't' value. 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="t">Value between 0.0 and 1.0</param>
        /// <returns></returns>
        public static Snapshot Lerp(Snapshot from, Snapshot to, float t)
        {
            return new Snapshot
            {
                Position = Vector3.Lerp(from.Position, to.Position, t),
                Rotation = Quaternion.Slerp(from.Rotation, to.Rotation, t),
                FieldOfView = Mathf.Lerp(from.FieldOfView,to.FieldOfView, t)
            };
        }
    }
}
