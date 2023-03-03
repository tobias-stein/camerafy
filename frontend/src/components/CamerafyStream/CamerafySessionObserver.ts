import api from "@/api/api";
import ProxyClient from "./ProxyClient";
import { 
    TimeoutServerEvent, 
    SessionTerminateServerEvent 
} from "@/components/CamerafyStream/CamerafyEventReflector";

export enum CamerafySessionState 
{
    Uninitialized,
    Initialized,
    Connecting,
    Connected,
    Timeout,
    Disconnected,
    Terminated,
    Failed
}

export default class CamerafySessionObserver
{
    public state = CamerafySessionState.Uninitialized;

    private proxyClient : ProxyClient;

    constructor()
    {
        this.proxyClient = CamerafySessionState.Uninitialized;
    }

    public initialize(proxy : ProxyClient) : void
    {
        this.proxyClient = proxy;

        // subscribe to session timeout and terminate event
        api.events.$on(TimeoutServerEvent, () => 
        { 
            console.log("Client timeout from session.");
            this.state = CamerafySessionState.Disconnected; 
            this.proxyClient.disconnect();
        });

        api.events.$on(SessionTerminateServerEvent, () => 
        { 
            console.log("Remote session terminated.");
            this.state = CamerafySessionState.Terminated; 
            this.proxyClient.disconnect(true);
        });
    }
}