
namespace Camerafy.Service.Webrtc
{
    // A managed wrapper up class for the native c style peer connection APIs.
    public partial class Webrtc
    {
        public delegate void LocalSdpReadytoSendDelegate(string sdp);
        public event LocalSdpReadytoSendDelegate OnLocalSdpReadytoSend;

        public delegate void DataChannelReady();
        public event DataChannelReady OnDataChannelReady;

        public delegate void DataChannelClosed();
        public event DataChannelClosed OnDataChannelClosed;

        public delegate void DataChannelReceived(byte[] data);
        public event DataChannelReceived OnDataChannelReceived;

        public delegate void NewIceCandidateDelegate(string candidate, int sdp_mlineindex, string sdp_mid);
        public event NewIceCandidateDelegate OnNewIceCandidate;
    }
}