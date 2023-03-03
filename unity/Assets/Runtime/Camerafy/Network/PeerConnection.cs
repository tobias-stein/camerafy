using System;
using System.Collections;

namespace Camerafy.Network
{
    using Service.Messaging;
    using Service.Webrtc;

    /// <summary>
    /// The PeerConnection encapsules all inter-process communication gateways. In general
    /// all PeerConnection objects are directly linked to the internal event-bus and any 
    /// received data is directly forwarded to it.
    /// </summary>
    public class PeerConnection : Event.Participant
    {
        /// <summary>
        /// Holds the peer connection states
        /// </summary>
        [Flags]
        private enum PeerConnectionState
        { 
            Uninitialized = 0,

            /// <summary>
            /// Peer connection has been failed and is not usable.
            /// </summary>
            Failed = 1,

            /// <summary>
            /// Peer connection to the external message broker is available.
            /// </summary>
            MessageBrokerAvailable = 4,

            /// <summary>
            /// Peer connection to the webrtc data channel is available.
            /// </summary>
            WebRtcChannelAvailable = 8
        }

        /// <summary>
        /// Peer connection state.
        /// </summary>
        private PeerConnectionState State = PeerConnectionState.Uninitialized;

        /// <summary>
        /// Message broker data channel to server.
        /// </summary>
        private MessageBroker.Channel MessageBrokerChannel = null;

        /// <summary>
        /// The webrtc client.
        /// </summary>
        private Webrtc Webrtc = null;

        public bool IsConnected { get { return this.State.HasFlag(PeerConnectionState.MessageBrokerAvailable) || this.State.HasFlag(PeerConnectionState.WebRtcChannelAvailable); } }
        public bool IsFailed { get { return this.State.HasFlag(PeerConnectionState.Failed); } }

        public override void Initialize(string Id = null)
        {
            base.Initialize(Id);

            try
            {
                // allocate a new message broker data channel for this peer 
                this.MessageBrokerChannel = MessageBroker.Current.CreateNewChannel(Id);
                {
                    // forword any received data to receiver method
                    this.MessageBrokerChannel.Received += this.OnPeerDataReceived;
                }

                // update peer connection state to 'Connected' and set message broker available
                this.State = PeerConnectionState.MessageBrokerAvailable;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to initialize new peer connection for '{Id}'. Reason: {e.Message}");
                this.State = PeerConnectionState.Failed;
            }
        }

        /// <summary>
        /// Initialize webrtc stream for peers camera.
        /// </summary>
        /// <param name="InRemoveOffer"></param>
        protected IEnumerator StartWebrtc(string InRemoteOffer)
        {
            if (this.Webrtc != null)
                yield break;

            // initialize WebRTC instance for peer
            this.Webrtc = new Webrtc();
            {
                yield return this.Webrtc.SetRemoteDescription(InRemoteOffer);

                User.User user = (this as User.User);

                this.Webrtc.OnLocalSdpReadytoSend += (string sdp) => { user.ServerRtcAnswer(sdp); };

                if (!this.Webrtc.CaptureVideo(user.UserCameraController.Camera))
                {
                    Logger.Error("Failed to capture user camera stream.");
                    yield break;
                }

                // listen to data channel for direct peer-to-peer communication instead of using the signaling server
                this.Webrtc.OnReady += () => { this.State |= PeerConnectionState.WebRtcChannelAvailable; };
                this.Webrtc.OnClosed += () => { this.State = (this.State & ~PeerConnectionState.WebRtcChannelAvailable); };

                // feed forward any incoming webrtc data-channel data to dispatcher handler
                this.Webrtc.OnRecv += (data) => { this.OnPeerDataReceived(data); };

                // create the webrtc answer
                yield return this.Webrtc.CreateAnswer();
            }
        }

        protected void AddWebrtcIceCandidate(string candidate, int sdp_mlineindex, string sdp_mid) { this.Webrtc?.AddNewIceCandidate(candidate, sdp_mlineindex, sdp_mid); }

        public override void Dispose()
        {
            base.Dispose();

            // free up message broker channel resource (disconnect)
            this.MessageBrokerChannel.Dispose();
        }

        public override void Send(byte[] InData)
        {
            if (this.State.HasFlag(PeerConnectionState.WebRtcChannelAvailable))
            {
                this.Webrtc.SendData(InData);
            }
            else if (this.State.HasFlag(PeerConnectionState.MessageBrokerAvailable))
            {
                this.MessageBrokerChannel.Send(InData);
            }
            else
            {
                Logger.Warning($"Unable to send data. Peer connection (ID: {this.Id}) is not available.");
            }
        }

        /// <summary>
        /// Callback handler method to process any received peer data.
        /// </summary>
        /// <param name="InData"></param>
        private void OnPeerDataReceived(byte[] InData)
        {
            // reset last activity timestamp
            this.LastActivity = DateTime.UtcNow;

            // forward data to the event-bus for further processing
            Event.EventBus.Current.ProcessClientEventData(InData, this.Id);
        }
    }
}