import  EventBus, { SignalerConnectEvent, SignalerDisconnectEvent, WebRTCDataChannelConnectEvent, WebRTCDataChannelDisconnectEvent } from './EventBus'


class PeerConnectionProxy
{
    constructor()
    {
        this.Signaler = null;
        this.WebRTCDataChannel = null;
        this.SessionId = null;
        this.UserSessionId = null;

        EventBus.$on(SignalerConnectEvent, OnSignalerConnected);
        EventBus.$on(SignalerDisconnectEvent, OnSignalerDisconnected);
        EventBus.$on(WebRTCDataChannelConnectEvent, OnWebRTCConnected);
        EventBus.$on(WebRTCDataChannelDisconnectEvent, OnWebRTCDisconnected);
        
        const _this = this;

        function OnSignalerConnected(SignalerInstance, SessionId, UserSessionId) 
        { 
            _this.SessionId = SessionId;
            _this.UserSessionId = UserSessionId;
            _this.Signaler = SignalerInstance; 
        }

        function OnWebRTCConnected(WebRTCDataChannelInstance) 
        { 
            _this.WebRTCDataChannel = WebRTCDataChannelInstance; 
        }
        function OnSignalerDisconnected() { _this.Signaler = null; }
        function OnWebRTCDisconnected() { _this.WebRTCDataChannel = null; }
    }

    send(InArrayBuffer, session = false)
    {
        // convert array buffer to byte array buffer
        const u8a = new Uint8Array(InArrayBuffer);
        // convert bytes to base64 string
        const b64 = btoa(String.fromCharCode.apply(null, u8a));

        // try always to use WebRTC peer-2-peer channel to send data
        if(this.WebRTCDataChannel != null)
        {            
            this.WebRTCDataChannel.send(b64);
        }
        // if the peer-2-peer data-channel is not available yet, use the stomp message channel as fallback
        else if(this.Signaler != null)
        {
            this.Signaler.send(b64, session);
        }
        else
        {
            console.error("Trying to send data without initialized peer connection.");
        }
    }
}

export default new PeerConnectionProxy();