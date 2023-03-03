import stomp from 'stompjs';
import CamerafyConfig from '../CamerafyConfig';
import CamerafyLib from './CamerafyLib';
import EventBus, 
    { 
        SignalerConnectEvent, 
        SignalerDisconnectEvent, 
        SessionDataReceivedEvent 
    } from './EventBus'

const S_INITIALIZED = 0;
const S_CONNECTING = 1;
const S_CONNECTED = 2;
const S_DISCONNECTED = 3;

class Signaler
{
    constructor()
    {
        this.state = S_INITIALIZED;
    }

    async connect(userid, sessionid)
    {
        if(this.state === S_CONNECTING || this.state === S_CONNECTED)
            return;
            
        this.state = S_CONNECTING;

        const _this = this;

        this.SessionId = sessionid;
        this.UserSessionId = userid;

        // setup singaling message queue names
        this.SignalIn1 = `/queue/users/${userid}.S2C`;
        this.SignalIn2 = `/queue/sessions/${sessionid}.S2C`;
        this.SignalOut1 = `/queue/users/${userid}.C2S`;
        this.SignalOut2 = `/queue/sessions/${sessionid}.C2S`;

        // create stomp client
        this.Client = stomp.client(`ws://${CamerafyConfig.SignaligServer}:61614/stomp`);

        this.Client.heartbeat.outgoing = 20000;
        this.Client.heartbeat.incoming = 0;
        this.Client.debug = null; // prevents client to log messages

        // and connect to broker
        this.Client.connect('', '', OnConnect, OnError);

        function OnConnect()
        {
            // listen to incoming session messages
            _this.Client.subscribe(_this.SignalIn1, OnDataReceived);
            _this.Client.subscribe(_this.SignalIn2, OnDataReceived);

            this.state = S_CONNECTED;

            EventBus.$emit(SignalerConnectEvent, _this, _this.SessionId, _this.UserSessionId);
        }

        function OnError(error)
        {
            console.log(error.headers.message);
        }

        function OnDataReceived(message)
        {
            // decode base64 string back to data
            EventBus.$emit(SessionDataReceivedEvent, Uint8Array.from(atob(message.body), c => c.charCodeAt(0)));
        }
    }

    disconnect()
    {
        this.state = S_DISCONNECTED;

        // disconnect stomp client from broker
        if(this.Client)
        {
            CamerafyLib.User.User.Disconnect();
            this.Client.disconnect();
            this.Client = null;
        }

        EventBus.$emit(SignalerDisconnectEvent);
    }

    send(b64, session = false)
    {
        if(this.Client !== null)
        {
            // send encoded OpCode
            this.Client.send(session ? this.SignalOut2 : this.SignalOut1, {}, b64);
        }
    }
}

export default new Signaler();