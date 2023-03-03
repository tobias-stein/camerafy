using UnityEngine;

namespace Camerafy.Camera
{
    /// <summary>
    /// Base camera behaviour settings used for serialization.
    /// </summary>
    public class CameraBehaviourSettings
    {
        public float Weight { get; set; } = 1.0f;
    }

    public abstract class CameraBehaviourBase : MonoBehaviour
    {
        /// <summary>
        /// The weight defines how much a behaviour influences the final linear/angular velocity.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float Weigth = 1.0f;

        /// <summary>
        /// True if behaviour is active and influences the final velocity.
        /// </summary>
        public bool IsActive { get { return this.enabled; } set { this.enabled = value; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="InCamera"></param>
        /// <param name="CameraTarget"></param>
        /// <param name="InLinearVelocity"></param>
        /// <param name="InAngularVelocity"></param>
        public abstract void Apply(UnityEngine.Camera InCamera, Vector3 CameraTarget, Vector3 InLinearVelocity, Vector3 InAngularVelocity);

        private void OnEnable()
        {
        }

        /// <summary>
        /// Apply settings.
        /// </summary>
        /// <param name="Settings"></param>
        public void ApplySettings(CameraBehaviourSettings Settings)
        {
            this.Weigth = Mathf.Clamp01(Settings.Weight);
        }

        /// <summary>
        /// Return current behviours settings.
        /// </summary>
        /// <returns></returns>
        public virtual CameraBehaviourSettings GetSettings()
        {
            return new CameraBehaviourSettings
            {
                Weight = this.Weigth
            };
        }
    }
}
