
namespace Camerafy.Application.Mode
{
    /// <summary>
    /// The default user application mode.
    /// </summary>
    public class DefaultUserApplicationMode : IUserApplicationMode
    {
        public string ApplicationModeActionMap()
        {
            return "Default";
        }

        public void Deactivate()
        {
        }
    }
}
