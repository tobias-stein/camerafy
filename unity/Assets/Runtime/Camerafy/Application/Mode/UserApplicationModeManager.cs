using System.Collections.Generic;

namespace Camerafy.Application.Mode
{
    public delegate void UserApplicationModeActivatedHandler(IUserApplicationMode mode);

    public delegate void UserApplicationModeDeactivatedHandler(IUserApplicationMode mode);

    /// <summary>
    /// A user can change the logical mode he wants the simulation to be in. A mode may effect the way the
    /// application is responding to the users input. It may also activate new functionalities/tools.
    /// </summary>
    public class UserApplicationModeManager
    {
        #region Events

        /// <summary>
        /// Fired after a new mode becomes active.
        /// </summary>
        public event UserApplicationModeActivatedHandler OnUserApplicationModeActivated;

        /// <summary>
        /// Fired after the mode became deactived.
        /// </summary>
        public event UserApplicationModeDeactivatedHandler OnUserApplicationModeDeactivated;

        #endregion

        /// <summary>
        /// Holds the current active mode
        /// </summary>
        public IUserApplicationMode ActiveMode { get; private set; } = null;

        /// <summary>
        /// User modes are cached and their state will be preserved.
        /// </summary>
        private Dictionary<System.Type, IUserApplicationMode> Cache = new Dictionary<System.Type, IUserApplicationMode>();

        /// <summary>
        /// Switches the current active mode with the new one. This method will override any 
        /// previously cached state. If you want to switch back to the old state use the template
        /// version of this method.
        /// </summary>
        /// <param name="newMode"></param>
        public void Activate(IUserApplicationMode newMode)
        {
            if (this.ActiveMode == newMode)
                return;

            if (this.ActiveMode != null)
            {
                // notify currently active mode that is is going to be disabled
                this.ActiveMode.Deactivate();
                // Notify listeners about changes
                this.OnUserApplicationModeDeactivated?.Invoke(this.ActiveMode);
            }

            // make new mode the active one
            this.ActiveMode = newMode;
            // Notify listeners about changes
            this.OnUserApplicationModeActivated?.Invoke(newMode);

            // cache new state
            this.Cache.Add(newMode.GetType(), newMode);

            IUserApplicationMode cached = null;
            // put mode into chace if not already in there
            if (!this.Cache.TryGetValue(newMode.GetType(), out cached))
            {
                this.Cache.Add(newMode.GetType(), newMode);
            }
            // otherwise overide the last cached mode
            else
            {
                this.Cache[newMode.GetType()] = newMode;
            }
        }

        /// <summary>
        /// Switches to new mode, if mode has been activated before it will restore the last cached 
        /// state.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="newMode"></param>
        public void Activate<T>() where T : IUserApplicationMode
        {
            

            IUserApplicationMode cached = null;
            // try to get last cached state of the mode
            if (!this.Cache.TryGetValue(typeof(T), out cached))
            {
                // if there was no previous state cached, chache it now.
                cached = System.Activator.CreateInstance<T>();
                this.Cache.Add(typeof(T), cached);
            }

            if (this.ActiveMode == cached)
                return;

            if (this.ActiveMode != null)
            {
                // notify currently active mode that is is going to be disabled
                this.ActiveMode.Deactivate();
                // Notify listeners about changes
                this.OnUserApplicationModeDeactivated?.Invoke(this.ActiveMode);
            }

            // make new mode the active one
            this.ActiveMode = cached;
            // Notify listeners about changes
            this.OnUserApplicationModeActivated?.Invoke(cached);
        }
    }
}
