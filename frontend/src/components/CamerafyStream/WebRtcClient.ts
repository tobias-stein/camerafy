import EventEmitter from 'events'
import api from "@/api/api"; 
import { NewIceCandidateReadyServerServerEvent, ServerRtcAnswerServerEvent } from "./CamerafyEventReflector"
import { ProxyClientStatus } from './ProxyClient';

const RTCPeerConnection     = window.RTCPeerConnection || window.mozRTCPeerConnection || window.webkitRTCPeerConnection;
const RTCIceCandidate       = window.RTCIceCandidate || window.mozRTCIceCandidate || window.webkitRTCIceCandidate;
const RTCSessionDescription = window.RTCSessionDescription || window.mozRTCSessionDescription || window.webkitRTCSessionDescription;

export enum WebRtcClientState
{
    Uninitialized,
    Initializing,
    Initialized,
    Connecting,
    Connected,
    Disconnected,
    Failed
};

export default class WebRtcClient extends EventEmitter
{
    // Core peer connection
    private Connection? : RTCPeerConnection;
    
    // Data channel for peer data message transer
    private DataChannel? : RTCDataChannel;

    public State : WebRtcClientState;

    // true if remote sdp received
    private remoteSdpReceived : boolean;

    constructor()
    {
        super();

        this.State = WebRtcClientState.Uninitialized;

        this.remoteSdpReceived = false;
    }

    /**
     * Creates a webrtc offer.
     */
    public connect() : Promise<any>
    {
        console.log("Initialize WebRTC connection...");

        // make sure any ongoing connection is closed
        this.disconnect();

        return this.initializeConnection()
            .then((connection : RTCPeerConnection) => 
            {
                return this.createOffer(connection);
            })
            .catch((error) => 
            {
                console.error(error);
                this.State = ProxyClientStatus.Failed;
            });
    }

    private initializeConnection() : Promise<RTCPeerConnection>
    {
        // clear 'remote sdp received' flag
        this.remoteSdpReceived = false;

        return new Promise<RTCPeerConnection>((resolve, reject) =>
        {
            try
            {
                // create rtc peer connection config
                const RTCPeerConnectionConfiguration =
                {
                    iceServers: [
                        {
                            "urls": ["stun:stun.l.google.com:19302"],
                            "username": "",
                            "credential": ""
                        }
                    ]
                };

                // create connection with configuration
                this.Connection = new RTCPeerConnection(RTCPeerConnectionConfiguration);

                // check whenever session state is modified
                this.Connection.onnegotiationneeded = (event : Event) => { this.onNegotiationNeeded(event); };
                // check connection state changes...
                this.Connection.onconnectionstatechange = (event : Event) => { this.onConnectionStateChange(event); };
                // check on set local/remote sdp
                this.Connection.onsignalingstatechange = (event : Event) => { this.onSignalingStateChange(event); };
                // check ice gathering state changes...
                this.Connection.onicegatheringstatechange = (event : Event) => { this.onIceGatheringStateChange(event); };
                // check on negotiation and ice restart
                this.Connection.oniceconnectionstatechange = (event : Event) => { this.onIceConnectionStateChange(event); };
                // notify remote session on new ice candidate...
                this.Connection.onicecandidate = (event : RTCPeerConnectionIceEvent) => { if(event.candidate) { this.emit("onNewIceCandidate", event.candidate); } };
                // setup the render target remote stream...
                this.Connection.ontrack = async (event : RTCTrackEvent) =>
                { 
                    console.log(event); 

                    await new Promise<void>((resolve, reject) =>
                    {
                        console.log(this.Connection?.iceGatheringState);
                        this.emit("onNewStreams", event.streams); 
                        resolve();
                    });
                };

                api.events.$on(NewIceCandidateReadyServerServerEvent, (candidate : string, sdpMlineindex : number, sdpMid : string) => { this.onIceCandidateServer(candidate, sdpMlineindex, sdpMid); } );
                api.events.$on(ServerRtcAnswerServerEvent, (remoteSdp : string) => { this.onServerRtcAnswer(remoteSdp); });

                // This operation is required to generate offer SDP correctly!
                // allow peer to receive one video stream
                this.Connection.addTransceiver('video', { direction: 'recvonly' });

                // create a data-channel
                this.DataChannel = this.Connection.createDataChannel('data');
                this.DataChannel.onopen = () => { this.State = WebRtcClientState.Connected; console.log("Data channel opened."); };
                this.DataChannel.onclose = () => { console.log("Data channel closed."); };
                this.DataChannel.onerror = (e) => { console.error(`DataChannel error: ${e.error.message}`); };
                this.DataChannel.onmessage = (e) => { console.log(e); this.emit('onDataRecv', e.data); };

                // set client state to initialized
                this.State = WebRtcClientState.Initialized;
                resolve(this.Connection);
            }
            catch(error)
            {
                reject(error);
            }
        });
    }

    private createOffer(connection : RTCPeerConnection) : Promise<void>
    {
        console.log('Create WebRTC client offer.')
        this.State = WebRtcClientState.Connecting;

        return connection.createOffer()
            .then((localSdp) => 
            {
                return connection.setLocalDescription(localSdp);
            })
            .then(() => 
            {
                this.emit("onLocalOfferReady", connection.localDescription);
            });
    }

    private onNegotiationNeeded(event : Event) : void
    {
        console.log(event);
        return;
    }

    private onConnectionStateChange(event : Event) : void
    {
        console.log(event, this.Connection?.connectionState);
        switch(this.Connection?.connectionState) 
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
    }

    private onSignalingStateChange(event : Event) : void
    {
        console.log(event, this.Connection?.signalingState);
        return;
    }

    private onIceGatheringStateChange(event : Event) : void
    {
        console.log(event, this.Connection?.iceGatheringState);
        switch(this.Connection?.iceGatheringState) 
        {
            case "new":
                break;

            case "gathering":
                break;

            case "complete":
                this.emit("onConnected");
                break;
        }
    }

    private onIceConnectionStateChange(event : Event) : void
    {
        return;
    }

    private onIceCandidateServer(candidate : string, sdpMlineindex : number, sdpMid : string) : void 
    { 
        const _candidate = new RTCIceCandidate(
        {
            candidate: candidate, 
            sdpMLineIndex: sdpMlineindex, 
            sdpMid: sdpMid
        });

        this.Connection?.addIceCandidate(_candidate);
    }

    private onServerRtcAnswer(remoteSdp : string) : void
    {
        // do not try to set remote sdp twice (seems to happen in chrome, when reconnecting)
        if(this.remoteSdpReceived)
            return;

        // set 'remote sdp received' flag
        this.remoteSdpReceived = true;

        this.Connection?.setRemoteDescription({ sdp: remoteSdp, type: "answer" });
    }

    public disconnect() : void
    {
        // close data channel, if open.
        if(this.DataChannel)
            this.DataChannel.close();

        // close connection, if open.
        if(this.Connection)
            this.Connection.close();

        this.DataChannel = undefined;
        this.Connection = undefined;

        this.State = WebRtcClientState.Disconnected;
    }

    /**
     * Send data over data-channel.
     * @param data 
     */
    public send(data : Uint8Array) : void { this.DataChannel?.send(data); }
}