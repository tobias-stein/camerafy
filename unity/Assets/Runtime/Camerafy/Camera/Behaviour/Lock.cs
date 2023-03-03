using UnityEngine;

namespace Camerafy.Camera
{
    /// <summary>
    /// Lock behaviour settings used for serialization.
    /// </summary>
    public class LockBehaviourSettings : CameraBehaviourSettings
    {
        public float MinPitch   { get; set; } = 0.0f;
        public float MaxPitch   { get; set; } = 180.0f;
        public float MinYaw     { get; set; } = 0.0f;
        public float MaxYaw     { get; set; } = 360.0f;
    }


    ///-------------------------------------------------------------------------------------------------
    /// Class:  Lock
    ///
    /// Summary:    This behaviour enforces the camera be locked or bound in its min/max-rotation axes.
    /// If the minimum yaw-limit value is higher than the maximum limit, the boundary will be reversed.
    ///
    /// Author: Tobias Stein
    ///
    /// Date:   24/03/2019
    ///-------------------------------------------------------------------------------------------------

    public class Lock : CameraBehaviourBase
    {
        // pitch - rotation around right axis (X) [0 := top, 180 := bottom]
        [Range(0.0f, 180.0f)]
        public float MinPitch = 0.0f;

        [Range(0.0f, 180.0f)]
        public float MaxPitch = 180.0f;

        // yaw - rotation around up axis (Y) [0 := right, 180 := left, 360 := right]
        [Range(0.0f, 360.0f)]
        public float MinYaw = 0.0f;

        [Range(0.0f, 360.0f)]
        public float MaxYaw = 360.0f;

        // roll - rotation around forward axis (Z)
        //[Range(0.0f, 360.0f)]
        //public float MinRoll = 0.0f;

        //[Range(0.0f, 360.0f)]
        //public float MaxRoll = 360.0f;

        public override void Apply(UnityEngine.Camera InCamera, Vector3 CameraTarget, Vector3 InLinearVelocity, Vector3 InAngularVelocity)
        {
            // get camera direction to target
            Vector3 dir = InCamera.transform.position - CameraTarget;

            // Check Pitch
            {
                // get angle
                float angle = Vector3.Angle(dir, Vector3.up);

                if (angle < this.MinPitch)
                {
                    // enforce angle limit
                    dir = Quaternion.AngleAxis(angle - this.MinPitch, InCamera.transform.right) * dir;
                    InCamera.transform.position = Vector3.Lerp(InCamera.transform.position, dir + CameraTarget, this.Weigth);
                }
                else if (angle > this.MaxPitch)
                {
                    // enforce angle limit
                    dir = Quaternion.AngleAxis(angle - this.MaxPitch, InCamera.transform.right) * dir;
                    InCamera.transform.position = Vector3.Lerp(InCamera.transform.position, dir + CameraTarget, this.Weigth);
                }
            }

            // Check Yaw
            {
                bool reversed = this.MinYaw > this.MaxYaw;
                bool bound = Mathf.Abs(this.MaxYaw - this.MinYaw) < 359.99f;
                float min = reversed ? this.MaxYaw : this.MinYaw;
                float max = reversed ? this.MinYaw : this.MaxYaw;


                // convert angle from +/-180 to 0-360
                float angle = Vector3.SignedAngle(new Vector3(dir.x, 0.0f, dir.z), reversed ? Vector3.back : Vector3.forward, Vector3.up);
                angle = angle > 0.0f ? angle : 360.0f + angle;

                // this will prevent the angle form overshoot the bounds
                float tollerance = bound ? Mathf.Abs(InAngularVelocity.y) : 0.0f;

                bool minNear0 = (min - tollerance) < 0.0f;
                bool maxNear360 = (max + tollerance) > 360.0f;

                if (angle < (minNear0 ? min + tollerance : min))
                {
                    // enforce angle limit
                    dir = Quaternion.AngleAxis(angle - (min - (minNear0 ? -tollerance : 0.0f)), InCamera.transform.up) * dir;
                    InCamera.transform.position = Vector3.Lerp(InCamera.transform.position, dir + CameraTarget, this.Weigth);
                }
                else if (angle > (maxNear360 ? max - tollerance : max))
                {
                    // enforce angle limit
                    dir = Quaternion.AngleAxis(angle - (max - (maxNear360 ? tollerance : 0.0f)), InCamera.transform.up) * dir;
                    InCamera.transform.position = Vector3.Lerp(InCamera.transform.position, dir + CameraTarget, this.Weigth);
                }
            }

            // Check Roll
            {
                // NOT USED YET!
            }
        }

        public void ApplySettings(LockBehaviourSettings Settings)
        {
            base.ApplySettings(Settings);

            this.MinPitch   = Mathf.Clamp(Settings.MinPitch, 0.0f, 180.0f);
            this.MaxPitch   = Mathf.Clamp(Settings.MaxPitch, 0.0f, 180.0f);
            this.MinYaw     = Mathf.Clamp(Settings.MinYaw, 0.0f, 360.0f);
            this.MaxYaw     = Mathf.Clamp(Settings.MaxYaw, 0.0f, 360.0f);
        }

        public override CameraBehaviourSettings GetSettings()
        {
            return new LockBehaviourSettings
            {
                Weight      = this.Weigth,
                MinPitch    = this.MinPitch,
                MaxPitch    = this.MaxPitch,
                MinYaw      = this.MinYaw,
                MaxYaw      = this.MaxYaw
            };
        }
    }
}
