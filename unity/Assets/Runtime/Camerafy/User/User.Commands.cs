using System.Threading.Tasks;

namespace Camerafy.User
{
    using Application;
    using Event;

    public partial class User
    {    
        /// <summary>
        /// Command send to connecting client to singal ready state to establish webrtc connection.
        /// </summary>
        [CamerafyEvent(CamerafyEventProperty.Server)]
        void ClientServerWebrtcReady()
        {
            this.UserStatus = UserStatus.Connecting;
            EventBus.Current.SendServerEventData(this);
        }
    
        /// <summary>
        /// Command received from client side when new ice candidate has been discovered.
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="sdp_mlineindex"></param>
        /// <param name="sdp_mid"></param>
        [CamerafyEvent]
        void NewIceCandidateReady(string candidate, int sdp_mlineindex, string sdp_mid) { this.AddWebrtcIceCandidate(candidate, sdp_mlineindex, sdp_mid); }

        /// <summary>
        /// Command send to client when new ice candidate has been discovered on server side.
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="sdp_mlineindex"></param>
        /// <param name="sdp_mid"></param>
        [CamerafyEvent(CamerafyEventProperty.Server)]
        void NewIceCandidateReadyServer(string candidate, int sdp_mlineindex, string sdp_mid)
        {
            EventBus.Current.SendServerEventData(this, candidate, sdp_mlineindex, sdp_mid);
        }

        /// <summary>
        /// Command received from client side which holds the WebRTC offer.
        /// </summary>
        /// <param name="sdp"></param>
        [CamerafyEvent]
        async Task ClientRtcOffer(string sdp) 
        {
            await Application.Current.CreateGamethreadTask(delegate { StartCoroutine(this.StartWebrtc(sdp)); });
        }
    
        /// <summary>
        /// Command received from client side that indicates the client is disconnected.
        /// </summary>
        [CamerafyEvent]
        void Disconnect()
        {
            this.UserStatus = UserStatus.Disconnecting;
            Application.Current.Session.RemoveSessionUser(this.Id);
        }

        /// <summary>
        /// Command send to the user when he has timed out.
        /// </summary>
        [CamerafyEvent(CamerafyEventProperty.Server)]
        public void Timeout()
        {
            EventBus.Current.SendServerEventData(this);

            this.UserStatus = UserStatus.Disconnecting;
            Application.Current.Session.RemoveSessionUser(this.Id);
        }

        /// <summary>
        /// Command send to the client to indicate to that the server is 
        /// ready to establish webrtc with client.
        /// </summary>
        [CamerafyEvent(CamerafyEventProperty.Server)]
        public void ServerRtcClientReady() { EventBus.Current.SendServerEventData(this); }

        /// <summary>
        /// Command send to the client from server to transfer webrtc sdp data.
        /// </summary>
        /// <param name="sdp"></param>
        [CamerafyEvent(CamerafyEventProperty.Server)]
        public void ServerRtcAnswer(string sdp)
        {
            EventBus.Current.SendServerEventData(this, sdp);
            // put user to connected state after webrtc has been established.
            this.UserStatus = UserStatus.Connected;
        }

        [CamerafyEvent(Properties = CamerafyEventProperty.Client)]
        public async Task RequestCameraSnapshot(int width, int height, string format)
        {
            //// validate requested image format
            //Camera.SnapshotImageFormat fmt;
            //if (!Enum.TryParse(format.ToLower(), out fmt))
            //{
            //    Logger.Error($"Snapshot request with unsupported image format '{format}'. Request ignored.");
            //    return;
            //}

            //Action<byte[]> OnSnapshotFinsihed = async delegate (byte[] data)
            //{
            //    // upload snapshot image
            //    int BackendSnapshotId = await Application.Current.CamerafyBackend.SaveSnapshot(this.LoginData.UserId.Value, width, height, format, data);
                
            //    // notify client
            //    this.NewSnapshotAvailable(BackendSnapshotId);
            //};


            //// request new snapshot
            //await Application.Current.CreateGamethreadTask(delegate { this.UserCameraController.RequestCameraSnapshot(width, height, fmt, OnSnapshotFinsihed); });
        }

        /// <summary>
        /// Inform the client that a new snapshot is available
        /// </summary>
        [CamerafyEvent(CamerafyEventProperty.Server)]
        void NewSnapshotAvailable(int SnapshotId)
        {
            EventBus.Current.SendServerEventData(this, SnapshotId);
        }

        /// <summary>
        /// Send the initial camera values to the frontend
        /// </summary>
        /// <param name="JsonCameraData"></param>
        [CamerafyEvent(CamerafyEventProperty.Server)]
        public void InitialCameraValues(string JsonCameraData)
        {
            EventBus.Current.SendServerEventData(this, JsonCameraData);
        }
    }
}
