using UnityEngine;
using UnityEngine.UI;

namespace Camerafy.UI.Utility
{
    using Camerafy.User;

    public class FrameCounter : MonoBehaviour
    {
        public Text FrameCounterText;
        public User User;

        private void Update()
        {
            this.FrameCounterText.text = string.Format("{0:0.00}", User.AverageFramesPerSecond);
        }
    }
}