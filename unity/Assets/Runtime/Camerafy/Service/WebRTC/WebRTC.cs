using System;
using System.Collections.Generic;
using Unity.WebRTC;

namespace Camerafy.Service.Webrtc
{
    using Application;
    using System.Collections;

    // A managed wrapper up class for the native c style peer connection APIs.
    public partial class Webrtc
    {
        /// <summary>
        /// The peer connection.
        /// </summary>
        private RTCPeerConnection PC;

        /// <summary>
        /// Collection of all opened data channels for this connection.
        /// </summary>
        private RTCDataChannel DataChannel = null;

        public DataChannelReady OnReady;
        public DataChannelClosed OnClosed;
        public DataChannelReceived OnRecv;

        /// <summary>
        /// Handle to the camera captured video stream.
        /// </summary>
        private VideoStreamTrack VideoStream = null;

        public Webrtc()
        {
            try
            {
                // create the peer connection
                this.PC = new RTCPeerConnection();
               
                // Handle data channels
                this.PC.OnDataChannel += (channel) =>
                {
                    // we only care for the channel 'data' right now.
                    if (this.DataChannel == null && channel.Label == "data")
                    {
                        this.DataChannel = channel;

                        this.DataChannel.OnOpen += () => { this.OnReady(); };
                        this.DataChannel.OnClose += () => { this.DataChannel = null; this.OnClosed(); };
                        this.DataChannel.OnMessage += (data) => { this.OnRecv(data); };
                    }
                };

                // setup the peer connection configuration
                RTCConfiguration Config = default;
                {
                    // Ice servers
                    {
                        List<RTCIceServer> IceServers = new List<RTCIceServer>();

                        // add Ice servers from config
                        var Servers = Application.Current.Config.iceServers;
                        if (Servers.Count == 0)
                        {
                            Logger.Warning("No Ice servers provided. Check app configuration.");
                        }
                        else
                        {
                            foreach (var server in Servers)
                            {
                                if (server.urls.StartsWith("turn") && (string.IsNullOrEmpty(server.username) && string.IsNullOrEmpty(server.credential)))
                                {
                                    Logger.Warning("{0} server ignored. It has no user creadentails.", server.urls);
                                    continue;
                                }

                                IceServers.Add(new RTCIceServer
                                {
                                    urls = new[] { server.urls },
                                    username = server.username,
                                    credential = server.credential,
                                    credentialType = RTCIceCredentialType.Password
                                });
                            }
                            // set ice servers
                            Config.iceServers = IceServers.ToArray();
                        }
                    }

                    this.PC.SetConfiguration(ref Config);
                }

                // Handle new available ice candidates
                this.PC.OnIceCandidate = (candidate) => { this.OnNewIceCandidate?.Invoke(candidate.candidate, candidate.sdpMLineIndex, candidate.sdpMid); };

            }
            catch (Exception e)
            {
                Logger.Fatal($"Failed to cretae Webrtc peer connection. {e.Message}");
            }
        }

        public void ClosePeerConnection()
        {
            this.PC.Close();
        }

        public IEnumerator CreateAnswer()
        {
            RTCAnswerOptions Answer = default;

            var Op = this.PC.CreateAnswer(ref Answer);
            yield return Op;

            if (Op.IsError)
            {
                Logger.Error($"Failed to create answer for incoming client connection. {Op.Error.errorDetail.ToString()}");
                yield break;
            }

            var desc = Op.Desc;
            var Op2 = this.PC.SetLocalDescription(ref desc);
            yield return Op2;

            if (Op2.IsError)
            {
                Logger.Error($"Failed to set local session description. {Op2.Error.errorDetail.ToString()}");
                yield break;
            }

            // notify peer to local session data is ready to send to client.
            this.OnLocalSdpReadytoSend?.Invoke(desc.sdp);
        }

        public void AddNewIceCandidate(string candidate, int sdp_mlineindex, string sdp_mid)
        {
            RTCIceCandidate Candidate = default;
            {
                Candidate.candidate = candidate;
                Candidate.sdpMLineIndex = sdp_mlineindex;
                Candidate.sdpMid = sdp_mid;
            }

            this.PC.AddIceCandidate(ref Candidate);
        }

        public void SendData(byte[] data) { this.DataChannel?.Send(data); }

        public IEnumerator SetRemoteDescription(string sdp)
        {
            RTCSessionDescription remoteDesc = default;
            {
                remoteDesc.type = RTCSdpType.Offer;
                remoteDesc.sdp = sdp;
            }

            var Op = this.PC.SetRemoteDescription(ref remoteDesc);
            yield return Op;

            if (Op.IsError)
            {
                Logger.Error($"Failed to set remote peer session description. {Op.Error.errorDetail.ToString()}");
            }
        }

        public bool CaptureVideo(UnityEngine.Camera InCamera)
        {
            this.VideoStream = InCamera.CaptureStreamTrack(
                Application.Current.Config.MaxFrameWidth,
                Application.Current.Config.MaxFrameHeight,
                1000000);

            if (this.VideoStream == null)
                return false;

            // add captured camera stream to peer connection
            this.PC.AddTrack(this.VideoStream);

            return true;
        }
    }
}