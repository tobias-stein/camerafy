using System;

namespace Camerafy.Event
{
    [Flags]
    public enum CamerafyEventProperty : short
    {
        None = 0,

        /// <summary>
        /// Marks commands that can be send from client to server.
        /// </summary>
        Client = 1,

        /// <summary>
        /// Marks commands that can be send from server to client.
        /// </summary>
        Server = 2,

        /// <summary>
        /// Marks commands that will not trigger a response.
        /// </summary>
        NoReponse = 4,
    }

    /// <summary>
    /// Exposes a Unity gameobjects method to the Camerafy client javascript library. Those methods
    /// can be called from client-side if user has permission.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class CamerafyEventAttribute : Attribute
    {
        /// <summary>
        /// The permission group required to access this command.
        /// </summary>
        public string RequiredPermissionGroup { get; set; }

        /// <summary>
        /// The commands traits.
        /// </summary>
        public CamerafyEventProperty Properties { get; set; }


        public CamerafyEventAttribute(
            CamerafyEventProperty properties = CamerafyEventProperty.Client,
            string requiredPermission = null)
        {
            this.RequiredPermissionGroup = requiredPermission?.ToLower();
            this.Properties = properties;
        }
    }
}
