using UnityEngine;

namespace Camerafy.UI
{
    /// <summary>
    /// The Camerafy UI manager object.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        /// <summary>
        /// Root canvas group gameobject.
        /// </summary>
        public CanvasGroup Root = null;

        /// <summary>
        /// Reference to the user camera.
        /// </summary>
        public UnityEngine.Camera UserCamera = null;
        
        private void Start()
        {

        }
    }
}
