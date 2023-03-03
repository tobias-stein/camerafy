import Vue from 'vue';

/** Client side specific events */

export const ApplicationConnectEvent = "ApplicationConnectEvent";
export const ApplicationDisconnectEvent = "ApplicationDisconnectEvent";

export const UserLoginEvent = "UserLoginEvent";
export const UserLogoutEvent = "UserLogoutEvent";

export const CamerafySessionUnreachableEvent = "CamerafySessionUnreachableEvent";

export const SignalerConnectEvent = 'SignalerConnectEvent';
export const SignalerDisconnectEvent = 'SignalerDisconnectEvent';
export const WebRTCDataChannelConnectEvent = 'WebRTCDataChannelConnectEvent';
export const WebRTCDataChannelDisconnectEvent = 'WebRTCDataChannelDisconnectEvent';

export const SessionDataReceivedEvent = 'SessionDataReceivedEvent';

export const PlayerStateChangeEvent = "PlayerStateChangeEvent";
export const PlayerPlayEvent = "PlayerPlayEvent";
export const PlayerStopEvent = "PlayerStopEvent";

export const EnableRemoteInputTransmitEvent = "EnableRemoteInputTransmitEvent";
export const DisableRemoteInputTransmitEvent = "DisableRemoteInputTransmitEvent";

export const ExtensionActivateEvent = "ExtensionActivateEvent";

// export default eventbus instance
const EventBus = new Vue();
export default EventBus; 