using UnityEngine;

namespace Camerafy.Camera
{
    /// <summary>
    /// Orbit behaviour settings used for serialization.
    /// </summary>
    public class OrbitBehaviourSettings : CameraBehaviourSettings
    {
        public float Distance       { get; set; } = 1.0f;
        public float MinDistance    { get; set; } = 1.0f;
        public float MaxDistance    { get; set; } = 1.0f;
        public float LookAtDamping  { get; set; } = 1.0f;
        public float ZoomDamping    { get; set; } = 1.0f;
    }

    /// <summary>
    /// Forces a camera to orbit around a target.
    /// </summary>
    public class Orbit : CameraBehaviourBase
    {
        [Range(0.0f, float.MaxValue)]
        public float Distance = 1.0f;

        [Range(0.0f, float.MaxValue)]
        public float MinDistance = 1.0f;

        [Range(0.0f, float.MaxValue)]
        public float MaxDistance = 1.0f;

        [Range(0.00001f, 1.0f)]
        public float LookAtDamping = 1.0f;
       
        [Range(0.00001f, 1.0f)]
        public float ZoomDamping = 1.0f;

        public override void Apply(UnityEngine.Camera InCamera, Vector3 CameraTarget, Vector3 InLinearVelocity, Vector3 InAngularVelocity)
        {
            // rotate around target
            {
                Vector3 dir = InCamera.transform.position - CameraTarget;

                float pitch = -InAngularVelocity.x;
                float yaw = -InAngularVelocity.y;

                dir = Quaternion.AngleAxis(pitch, InCamera.transform.right) * Quaternion.AngleAxis(yaw, Vector3.up)  * dir;
                InCamera.transform.position = Vector3.Lerp(InCamera.transform.position, dir + CameraTarget, this.Weigth);
            }

            // normalized direction to target, if no target set use origon
            Vector3 direction = (CameraTarget - InCamera.transform.position).normalized;

            // zoom
            {
                this.Distance = Mathf.Clamp(this.Distance - InLinearVelocity.z, this.MinDistance, this.MaxDistance);
                if (Mathf.Abs(this.Distance - Vector3.Distance(InCamera.transform.position, CameraTarget)) > 1e-4f)
                {
                    Vector3 desiredDistancePosition = (-direction * this.Distance) + CameraTarget;
                    InCamera.transform.position = Vector3.Lerp(InCamera.transform.position, desiredDistancePosition, this.Weigth * this.ZoomDamping);
                }
            }            

            // look at target
            {
                InCamera.transform.rotation = Quaternion.Slerp(InCamera.transform.rotation, Quaternion.LookRotation(direction), this.Weigth * this.LookAtDamping);
            }
        }

        public void ApplySettings(OrbitBehaviourSettings Settings)
        {
            base.ApplySettings(Settings);

            this.Distance = Settings.Distance;
            this.MinDistance = Settings.MinDistance;
            this.MaxDistance = Settings.MaxDistance;
            this.ZoomDamping = Settings.ZoomDamping;
            this.LookAtDamping = Settings.LookAtDamping;
        }

        public override CameraBehaviourSettings GetSettings()
        {
            return new OrbitBehaviourSettings
            {
                Weight = this.Weigth,
                Distance = this.Distance,
                MinDistance = this.MinDistance,
                MaxDistance = this.MaxDistance,
                ZoomDamping = this.ZoomDamping,
                LookAtDamping = this.LookAtDamping
            };
        }
    }
}
