import { Client, Frame, IMessage } from '@stomp/stompjs';
import EventEmitter from 'events'

export enum StompClientState
{
    Uninitialized,
    Initialized,
    Connecting,
    Connected,
    Disconnected,
    Failed
};

export default class StompClient extends EventEmitter
{
    private Client : Client;

    public State : StompClientState = StompClientState.Uninitialized;

    // client exclusive message queue 
    // client -> server [outgoing messages]
    private C2SCMQ? : string;
    // server -> client [incoming messages]
    private S2CCMQ? : string;

    // global message queue for a specific running session [outgoing messages]
    private C2SSMQ? : string;
    // global message queue for a specific running session [incoming messages]
    private S2CSMQ? : string;

    private Subscriptions : Array<any>;

    constructor(brokerUrl : string, username : string, password : string)
    {
        // base class EventEmitter constructor
        super();

        this.Subscriptions = new Array<any>();
        this.Client = new Client(
        {
            brokerURL: brokerUrl,
            //debug: (msg) => { console.log(msg); },
            reconnectDelay: 1000,
            heartbeatIncoming: 5000,
            heartbeatOutgoing: 5000,
            connectHeaders: {
                'login': username,
                'passcode': password
            }
        });

        this.Client.onConnect = (frame : Frame) => 
        { 
            console.log("Connected to message broker.")
            // forward all received binary data from message (unwrap)
            
            // subscribe to client specific messages
            //console.log(`Subscribe to ${this.S2CCMQ}`);
            //this.Subscriptions.push(this.Client.subscribe(this.S2CCMQ, (message : IMessage) => { this.emit("onDataRecv", message.binaryBody); }, { 'auto-delete': 'true', 'x-message-ttl': `${process.env.VUE_APP_MESSAGE_BROKER_MESSAGE_TTL}` })); 
            this.Subscriptions.push(this.Client.subscribe(this.S2CCMQ, (message : IMessage) => { this.emit("onDataRecv", message.binaryBody); })); 
            
            // subscribe to session messages
            //console.log(`Subscribe to ${this.S2CSMQ}`);
            //this.Subscriptions.push(this.Client.subscribe(this.S2CSMQ, (message : IMessage) => { this.emit("onDataRecv", message.binaryBody); }, { 'auto-delete': 'true', 'x-message-ttl': `${process.env.VUE_APP_MESSAGE_BROKER_MESSAGE_TTL}` })); 
            this.Subscriptions.push(this.Client.subscribe(this.S2CSMQ, (message : IMessage) => { this.emit("onDataRecv", message.binaryBody); })); 
            
            this.State = StompClientState.Connected;
        };

        this.Client.onStompError = (frame : Frame) => 
        {
            console.error('Broker reported error: ' + frame.headers['message']);
            console.error('Additional details: ' + frame.body);
        };
        this.Client.onDisconnect = () => { this.State = StompClientState.Disconnected; };
        this.Client.onWebSocketClose = () => { this.State = StompClientState.Disconnected; };
        this.Client.onWebSocketError = (error) => { console.error(error); };

        this.State = StompClientState.Initialized;
    }

    public async connect(clientId : string, sessionId : string) : Promise<void>
    {
        // 'Client to Server' client message queue [OUT]
        this.C2SCMQ = `/queue/${clientId}.C2S`;
        // 'Server to Client' client message queue [IN]
        this.S2CCMQ = `/queue/${clientId}.S2C`;
        // 'Client to Server' session message queue [OUT]
        this.C2SSMQ = `/queue/${sessionId}.C2S`;
        // 'Server to Client' session message queue [IN]
        this.S2CSMQ = `/queue/${sessionId}.S2C`;       

        return new Promise<void>((resolve, reject) =>
        {
            switch(this.State)
            {
                case StompClientState.Failed:
                case StompClientState.Uninitialized:
                    reject("Stomp client is in invalid state and cannot be conneted to message broker.");
                    break;

                case StompClientState.Connecting:
                    reject("Stomp client is currently connecting to the message broker, cannot connect to another server right now.");
                    break;

                case StompClientState.Connected:
                    console.log("Stomp client alrady connected.");
                    resolve();
                    break;

                case StompClientState.Initialized:
                case StompClientState.Disconnected:
                    console.log('Connecting to message broker...');
                    this.State = StompClientState.Connecting;
                    this.Client.activate();
                    resolve();
                    break;

                default:
                    resolve();
            }
        });
    }

    public disconnect() : void
    {
        console.log("Disconnecting from message broker...");

        // disconnect stomp client from broker
        if(this.Client)
        {
            // unsubscribe from all queues
            while(this.Subscriptions.length) 
            { 
                const sub = this.Subscriptions.pop();
                sub.unsubscribe(); 
            }
            this.Client.deactivate();
        }

        console.log("Disconnected from message broker.");

        this.State = StompClientState.Disconnected;
    }

    public send(data : Uint8Array, session = false)
    {
        this.Client.publish(
        {
            destination: session ? this.C2SSMQ : this.C2SCMQ, 
            binaryBody: data,
            headers: {
                'content-type': 'application/octet-stream',
                //'x-message-ttl': `${process.env.VUE_APP_MESSAGE_BROKER_MESSAGE_TTL}`,
                //'x-expires': `${process.env.VUE_APP_MESSAGE_BROKER_MESSAGE_TTL}`,
                //'auto-delete': 'true',
                'timestamp': `${Date.now()}`
            }
        });
    }
}