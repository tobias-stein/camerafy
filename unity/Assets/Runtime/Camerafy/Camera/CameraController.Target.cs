using System.Collections.Generic;
using UnityEngine;

namespace Camerafy.Camera
{
    /// <summary>
    /// Base public interface for a camera target, which is always a position in 3D space.
    /// </summary>
    public interface ICameraTarget
    {
        Vector3 TargetLocation { get; }
    }

    /// <summary>
    /// Concrete camera target implementation for Transform type.
    /// </summary>
    public class TranformCameraTarget : ICameraTarget
    {
        private Transform Target = null;

        public TranformCameraTarget() { this.Target = null; }
        public TranformCameraTarget(Transform InTransform) { this.Target = InTransform; }
        public Vector3 TargetLocation { get { return this.Target != null ? this.Target.position : Vector3.zero; } }
    }

    /// <summary>
    /// Concrete camera target implementation for Vector3 type.
    /// </summary>
    public class Vector3CameraTarget : ICameraTarget
    {
        private Vector3 Target = Vector3.zero;

        public Vector3CameraTarget() { this.Target = Vector3.zero; }
        public Vector3CameraTarget(Vector3 InVector3) { this.Target = InVector3; }
        public Vector3 TargetLocation { get { return this.Target; } }
    }

    public partial class CameraController
    {
        /// <summary>
        /// A collection of referencable camera targets.
        /// </summary>
        private static Dictionary<string, ICameraTarget> GlobalCameraTarget = new Dictionary<string, ICameraTarget>();

        /// <summary>
        /// Register a new camera target by name.
        /// </summary>
        /// <param name="InTargetName"></param>
        /// <param name="InTarget"></param>
        public static void RegisterCameraTarget(string InTargetName, ICameraTarget InTarget)
        {
            if (!CameraController.GlobalCameraTarget.ContainsKey(InTargetName))
            {
                CameraController.GlobalCameraTarget.Add(InTargetName, InTarget);
            }
        }

        /// <summary>
        /// Remove camera target from collection.
        /// </summary>
        /// <param name="InTargetName"></param>
        public static void UnregisterCameraTarget(string InTargetName)
        {
            CameraController.GlobalCameraTarget.Remove(InTargetName);
        }
    }
}