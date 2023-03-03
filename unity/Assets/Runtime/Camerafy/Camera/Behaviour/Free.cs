using UnityEngine;

namespace Camerafy.Camera
{
    /// <summary>
    /// Free behaviour settings used for serialization.
    /// </summary>
    public class FreeBehaviourSettings : CameraBehaviourSettings
    {
    }

    public class Free : CameraBehaviourBase
    {
        public Free()
        { }

        public override void Apply(UnityEngine.Camera InCamera, Vector3 CameraTarget, Vector3 InLinearVelocity, Vector3 InAngularVelocity)
        {
            InCamera.transform.Translate(InLinearVelocity * this.Weigth);

            /* When applying horizontal and vertical rotation the roll gets messed up.
             * We save the current roll value and reset it after the rotation is applied.
             */
            float roll = InCamera.transform.localRotation.eulerAngles.z;

            InCamera.transform.Rotate(InAngularVelocity * this.Weigth);
            Vector3 newLocalRotation = InCamera.transform.eulerAngles;
            newLocalRotation.z = roll;
            InCamera.transform.eulerAngles = newLocalRotation;

        }

        public void ApplySettings(FreeBehaviourSettings Settings)
        {
            base.ApplySettings(Settings);
        }

        public override CameraBehaviourSettings GetSettings()
        {
            return new FreeBehaviourSettings
            {
                Weight = this.Weigth,
            };
        }
    }
}
