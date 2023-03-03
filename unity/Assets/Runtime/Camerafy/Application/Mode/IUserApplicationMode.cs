
namespace Camerafy.Application.Mode
{
    public interface IUserApplicationMode
    {
        /// <summary>
        /// Returns a string that names the modes input action map.
        /// </summary>
        /// <returns></returns>
        string ApplicationModeActionMap();

        /// <summary>
        /// Called by the manager class when a mode gets deactivated.
        /// </summary>
        void Deactivate();
    }
}
