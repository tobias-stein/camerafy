using System;
using UnityEngine;

namespace Camerafy.Event
{
    /// <summary>
    /// All event Participants classes are allowed to send and receive events over
    /// the global Camerafy event-bus.
    /// </summary>
    [DisallowMultipleComponent]
    public class Participant : MonoBehaviour, IDisposable
    {
        public delegate void DataReceivedDelegate(byte[] InData);

        /// <summary>
        /// Unique id for this event participant.
        /// </summary>
        public string Id { get; protected set; }

        /// <summary>
        /// Timestamp of the last activity of this participant.
        /// </summary>
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        public virtual void Initialize(string Id = null)
        {
            this.Id = Id ?? Guid.NewGuid().ToString("D");

            // register this participant on EventBus
            EventBus.Current.Register(this);
        }

        /// <summary>
        /// Called by the EventBus to send back event data to the participant.
        /// This method is implemented by the PeerConnection class.
        /// </summary>
        /// <param name="InData"></param>
        public virtual void Send(byte[] InData) { }

        public virtual void Dispose()
        {
            // remove this participant from EventBus
            EventBus.Current?.Unregister(this);
        }
    }
}