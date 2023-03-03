import webrtc from 'webrtc-adapter';
import CamerafyConfig from '../CamerafyConfig'
import EventBus, { SessionDataReceivedEvent, WebRTCDataChannelConnectEvent, WebRTCDataChannelDisconnectEvent } from '../services/EventBus'
import CamerafyLib from './CamerafyLib';

const RTCPeerConnection     = window.RTCPeerConnection || window.mozRTCPeerConnection || window.webkitRTCPeerConnection;
const RTCIceCandidate       = window.RTCIceCandidate || window.mozRTCIceCandidate || window.webkitRTCIceCandidate;
const RTCSessionDescription = window.RTCSessionDescription || window.mozRTCSessionDescription || window.webkitRTCSessionDescription;


class WebRTC
{
    constructor()
    {
        this.Peer = null;
        this.Stream = null;
        this.WebRTCDataChannel = null;
    }

    connect(onConnected)
    {
        const _this = this;

        // skip, if already initialized.
        if(this.Peer)
          return;

        try
        {
          // create rtc peer connection config
          const RTCPeerConnectionConfiguration = 
          {
            iceServers: CamerafyConfig.iceServers
          };

          // create connection with configuration
          this.Peer = new RTCPeerConnection(RTCPeerConnectionConfiguration);

          // check connection state changes...
          this.Peer.onconnectionstatechange = function(event) 
          {
              switch(_this.Peer.connectionState) 
              {
                  case "new":
                      break;

                  case "connecting":
                      break;

                  case "connected":
                      break;
                
                  case "disconnected":
                      break;

                  case "closed":
                      break;

                  case "failed":
                      break;
              }
          };

          this.Peer.onsignalingstatechange = function(event) { };

          // check ice gathering state changes...
          this.Peer.onicegatheringstatechange = function(event) 
          {
              switch(_this.Peer.iceGatheringState) 
              {
                  case "new":
                      break;

                  case "gathering":
                      break;

                  case "complete":
                      onConnected(_this.Stream);
                      break;
              }
          };

          this.Peer.oniceconnectionstatechange = function(event) { };

          // notify remote session on new ice candidate...
          this.Peer.onicecandidate = function (event)
          {
              if(event.candidate)
              {
                  // broadcast new ice candidate
                  const Candidate = event.candidate;
                  CamerafyLib.User.User.NewIceCandidateReady(Candidate.candidate, Candidate.sdpMLineIndex, Candidate.sdpMid)
              }
          };

          // setup the render target remote stream...
          this.Peer.ontrack = function(event) { _this.Stream = event.streams[0]; };
          this.Peer.onremovetrack = function(event) { };
          this.Peer.ondatachannel = function(event)
          {
            // store data channel object
            _this.WebRTCDataChannel = event.channel;

            // notify lister about new data-channel connection
            EventBus.$emit(WebRTCDataChannelConnectEvent, _this.WebRTCDataChannel);

            _this.WebRTCDataChannel.onmessage = function(e) { EventBus.$emit(SessionDataReceivedEvent, Uint8Array.from(atob(e.data), c => c.charCodeAt(0))); };
            _this.WebRTCDataChannel.onopen = function(e) { };
            _this.WebRTCDataChannel.onclose = function(e) { _this.WebRTCDataChannel = null; };
          };
        }
        catch(err)
        {
          console.error(err);
          this.disconnect();
          return false;
        }

        // initialized!
        return true;
    }

    async OnOffer(sdp)
    {
      try
      {
          // update remote description
          await this.Peer.setRemoteDescription(new RTCSessionDescription({ type: "offer", sdp: sdp }));  
          // determine and set local desciption
          await this.Peer.setLocalDescription(await this.Peer.createAnswer());  
          // send answer to server
          CamerafyLib.User.User.ClientRtcAnswer(this.Peer.localDescription.sdp);
      }
      catch(err)
      {
          this.disconnect();
      }
    }

    disconnect()
    {
        // notify lister about new data-channel connection
        EventBus.$emit(WebRTCDataChannelDisconnectEvent);

        if(this.WebRTCDataChannel !== undefined && this.WebRTCDataChannel !== null)
        {
            this.WebRTCDataChannel.close();
        }

        if(this.Peer !== undefined && this.Peer !== null)
        {
            this.Peer.close();
        }

        this.WebRTCDataChannel = null;
        this.Peer = null;
    }

    send(data)
    {
        if(this.WebRTCDataChannel !== null)
        {
            this.WebRTCDataChannel.send(data);
        }
        else
        {
            console.error("Failed to send data over webRTC data-cannel.");
        }
    }
}

export default new WebRTC();