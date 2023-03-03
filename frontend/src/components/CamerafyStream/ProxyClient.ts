import EventEmitter from 'events'
import StompClient, { StompClientState } from "./StompClient";
import WebRtcClient, { WebRtcClientState } from "./WebRtcClient";
import api from "@/api/api";

import EventReflector, { 
    ICamerafyEvent, 
    ICamerafySessionData, 
    CamerafySessionDataType,
    CamerafyResponseStatus,
    CamerafyResponseData,
    ClientServerWebrtcReadyServerEvent, 
    TimeoutServerEvent} from "./CamerafyEventReflector";

interface PendingRequestContext
{
    resolve : { (result : any) : void };
    reject : { (reason : any) : void };
}

export enum ProxyClientStatus
{
    Uninitialized,
    Initialized,
    Connecting,
    Connected,
    Disconnected,
    Failed
};

export default class ProxyClient extends EventEmitter
{
    public state : ProxyClientStatus;

    // client for message broker communication
    private stompClient? : StompClient;
    // client for webrtc communication
    private webrtcClient? : WebRtcClient;

    /** Collection of still pending requests that did not have received a response yet. */
    private pendingRequests : Map<string, PendingRequestContext>;

    /** When attempting connect to remote session, this will hold a timeout handle. Which is
     * cleared once we connected to the session. Otherwise, if this timeout is fullfilled we
     * abort the connection attempt.
     */
    private pendingSessionConnectTimeout : number;

    constructor()
    {
        super();

        this.pendingRequests = new Map<string, PendingRequestContext>();

        this.pendingSessionConnectTimeout = -1;

        this.state = ProxyClientStatus.Uninitialized;

        /**
         * We will listen for incoming server events of type 'ClientServerWebrtcReady'
         * that indicates we are good to go to send our initial Webrtc offer to the 
         * server.
         */
        api.events.$on(ClientServerWebrtcReadyServerEvent, async () => 
        { 
            await this.webrtcClient?.connect()
                // any error will cause the proxy client to be set to failed state
                .catch((error) => 
                {
                    this.state = ProxyClientStatus.Failed;
                    throw error;
                })
                .finally(() => 
                {
                    if(this.pendingSessionConnectTimeout > 0)
                    {
                        clearTimeout(this.pendingSessionConnectTimeout);
                        this.pendingSessionConnectTimeout = -1;
                    }
                });
        });
    }

    public initialize(brokerUrl : string, username : string, password : string) : void
    {
        /** Create new client for message broker and delegate all received data to this.onDataRecv handler. */
        this.stompClient = new StompClient(brokerUrl, username, password);
        // subscribe to 'data received' event
        this.stompClient.on("onDataRecv", (data : Uint8Array) => { this.onDateRecv(data); });

        /** Create new client for webrtc */
        this.webrtcClient = new WebRtcClient();
        // subscribe to 'data received' event
        this.webrtcClient.on("onDataRecv", (data : Uint8Array) => { this.onDateRecv(data); });
        // subscribe to 'webrtc connected' event
        this.webrtcClient.on("onConnected", () => { this.onWebrtcConnected(); });
        // subscribe to 'new ice candidate' event
        this.webrtcClient.on("onNewIceCandidate", (iceCandidate : any) => { this.onNewIceCandidate(iceCandidate); });
        // subscribe to 'local offer ready' event
        this.webrtcClient.on("onLocalOfferReady", (localOffer : any) => { this.onLocalOfferReady(localOffer); });
        // subscribe to 'new stream' event
        this.webrtcClient.on("onNewStreams", (streams : Array<MediaStream>) => { this.emit("onNewStreams", streams); });
    }

    public async connectSignalingServer(clientId : string, sessionId : string)
    {
        // connect to message broker
        return this.stompClient?.connect(clientId, sessionId)
            // notify Camerafy application about client and try to connect to session
            .then(async () => 
            {
                // wait until stomp client connection has fully established
                while(this.stompClient?.State == StompClientState.Connecting)
                { await new Promise(resolve => setTimeout(resolve, 200)); }

                // if we do not have a stomp connection, give up 
                if(this.stompClient?.State != StompClientState.Connected)
                {
                    throw new Error("Failed to establish stomp client connection.");
                }
                else
                {
                    // connection to stomp signaling server established
                    this.state = ProxyClientStatus.Initialized;
                }
            })
            // any error will cause the proxy client to be set to failed state
            .catch((error) => 
            {
                this.state = ProxyClientStatus.Failed;
                throw error;
            });
    }

    public async connectSession(clientId : string) : Promise<any>
    {
        // notify Camerafy application about client and try to connect to session
        return new Promise<any>((resolve, reject) => 
        {
            switch(this.state)
            {
                case ProxyClientStatus.Uninitialized:
                    reject(Error("Unable to initialize session. Proxy client is not initialized yet."));
                    break;
                case ProxyClientStatus.Failed:
                    reject(Error("Unable to initilaize session. Proxy client is in fail state."));
                    break;
                case ProxyClientStatus.Connected:
                    console.log("Proxy client already connected to a session.");
                    resolve();
                    break;
                case ProxyClientStatus.Connecting:
                    console.log("Proxy client is currently connection to a session.");
                    resolve();
                    break;
                case ProxyClientStatus.Initialized:
                case ProxyClientStatus.Disconnected:
                    this.state = ProxyClientStatus.Connecting;
                    resolve();
                    break;
            }
        })
        // attempt to connect to session
        .then(() => { 
            console.log("Attempt connect to remote session...");

            // refelect event data
            const reflected = EventReflector.Application.Session.Connect(clientId);

            // send event data via message broker
            this.stompClient?.send(reflected.data, true);

            // set connection attempt timeout
            this.pendingSessionConnectTimeout = setTimeout(() =>
            {
                this.state = ProxyClientStatus.Initialized;
                
                clearTimeout(this.pendingSessionConnectTimeout);
                this.pendingSessionConnectTimeout = -1;

                // emit session connect timeout event
                this.emit("onSessionConnectTimeout");
            },
            process.env.VUE_APP_REMOTE_SESSION_CONNECT_TIMEOUT);

            // add pending request
            return this.addPendingRequestFor(reflected.id);
         })
        // catch any error and put proxy client to fail state
        .catch((error) => 
        {
            this.state = ProxyClientStatus.Failed;
            throw error;
        });
    }

    /** Closes webrtc peer session connection. */
    public disconnect(closeSignalingServerConnection = false) : void
    {
        this.webrtcClient?.disconnect();

        if(closeSignalingServerConnection)
            this.stompClient?.disconnect();

        this.state = ProxyClientStatus.Disconnected;        
    }

    private onLocalOfferReady(localOffer : any) : Promise<any>
    {
        console.log(localOffer.sdp);
        const reflected = EventReflector.User.User.ClientRtcOffer(localOffer.sdp);
        // send event data via message broker
        this.stompClient?.send(reflected.data, false);
        // add pending request
        return this.addPendingRequestFor(reflected.id);
    }

    private onNewIceCandidate(iceCandidate : any) : Promise<any>
    {
        console.log(iceCandidate);
        // refelect event data
        const reflected = EventReflector.User.User.NewIceCandidateReady(iceCandidate.candidate, iceCandidate.sdpMlineindex, iceCandidate.sdpMid);
        // send event data via message broker
        this.stompClient?.send(reflected.data, false);
        // add pending request
        return this.addPendingRequestFor(reflected.id);
    }

    private onWebrtcConnected() : void
    {
        this.state = ProxyClientStatus.Connected;
    }

    /** Adds a new promise to the pending request collection and returns the promise. */
    private addPendingRequestFor(messageId : string) : Promise<any>
    {
        return new Promise((resolve, reject) => 
        { 
            // if a messageId was provided queue a pending request
            if(messageId.length)
            {
                this.pendingRequests.set(messageId, {resolve: resolve, reject: reject}); 
            }
            // otherwise return immediately
            else
            {
                resolve();
            }
        });
    }

    /**
     * Send data to the server.
     * @param eventData 
     */
    public sendEvent(eventData : ICamerafyEvent) : Promise<any>
    {
        // always try to send event data via webrtc p2p data-channel (fastest)
        if(this.webrtcClient?.State === WebRtcClientState.Connected)
        {
            this.webrtcClient?.send(eventData.data);
        }
        // if the data-channel is unavailable try the message broker (slower)
        else if(this.stompClient?.State === StompClientState.Connected)
        {
            // send event data via message broker
            this.stompClient?.send(eventData.data, false);
        }
        // else we ran out of options and are not able to communicate with the session.
        else
        {
            console.error("Unable to send event data to session. No connectivity.");
            return new Promise((resolve, reject) => { reject("Unable to send event data to session. No connectivity."); });
        }

        // add pending request
        return this.addPendingRequestFor(eventData.id);
    }

    /**
     * Data receive handler for incoming data from stomp-client and webrtc data-channel.
     * @param sessionData 
     */
    private onDateRecv(sessionData : Uint8Array) : void
    {
        const reflected = EventReflector.deserializeSessionData(sessionData);
        //console.log(reflected)
        switch(reflected.type)
        {
            case CamerafySessionDataType.EventResponse:
                if(reflected.responseRefId)
                {
                    const pendingRequestCtx = this.pendingRequests.get(reflected.responseRefId);
                    if(pendingRequestCtx)
                    {
                        // check if we have valid response data
                        if(!reflected.responseData)
                        {
                            pendingRequestCtx.reject(`Reponse for pending reqeust ${reflected.responseRefId} has no data!`);
                        }
                        // check if response was error
                        else if(reflected.responseData?.status == CamerafyResponseStatus.error)
                        {
                            pendingRequestCtx.reject(`Request ${reflected.responseRefId} failed. Reason: ${reflected.responseData.status}`)
                        }
                        // else success
                        else
                        {
                            pendingRequestCtx.resolve(reflected.responseData);
                        }

                        // remove request from pending collection
                        this.pendingRequests.delete(reflected.responseRefId);
                    }
                    else
                    {
                        console.error(`Received session event response data, but found no pending request for reference event id '${reflected.responseRefId}'.`);
                    }
                }
                else
                {
                    console.error("Received session data without response reference id.");
                }
                break;

            case CamerafySessionDataType.SessionEvent:
                api.events.broadcast(reflected.sessionEventName ?? "UNKNOWN_SESSION_EVENT", reflected.sessionEventArgs ?? []);
                break;
        }
    }
};