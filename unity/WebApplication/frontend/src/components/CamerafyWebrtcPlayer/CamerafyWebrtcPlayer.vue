<template>
  <div>
    <div class="video-container" ref="videoContainer">
      <video ref="video" />

      <v-hover v-show="State !== 3 || paused" v-slot:default="{ hover }">
        <v-btn tile icon class="playButton" v-bind:loading="State > 0 && !paused" v-bind:style="{opacity: hover ? 1.0 : 0.1}" @click="Play()">
          <v-icon v-show="State !== 5" size="75">play_circle_filled</v-icon>
          <v-icon v-show="State === 5" size="75">offline_bolt</v-icon>
        </v-btn>
      </v-hover>

      <v-hover v-show="userLoggedIn && State === 3 && !paused" v-slot:default="{ hover }">
        <v-btn icon class="snapshotButton" v-bind:style="{opacity: hover ? 1.0 : 0.1}" @click="TakeSnapshot()">
          <v-icon size="32">photo_camera</v-icon>
        </v-btn>
      </v-hover>

      <v-hover v-show="!nofullscreen" v-slot:default="{ hover }">
        <v-btn tile icon class="fullscreenButton" v-bind:style="{opacity: hover ? 1.0 : 0.1}" @click="ToggleFullscreen()">
          <v-icon x-large v-show="!fullscreen">aspect_ratio</v-icon>
          <v-icon x-large v-show="fullscreen">featured_video</v-icon>
        </v-btn>
      </v-hover>

      <ControlPanel />
    </div>
  </div>
</template>

<style>
.video-container {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 100%;
  height: 100%; 
  overflow: hidden;
  background-color: black; 
}

.video-container video {
  position: absolute;
  top: 50%;
	left: 50%;

  /* Make video to at least 100% wide and tall */
  min-width: 100%; 
  min-height: 100%; 

  /* Setting width & height to auto prevents the browser from stretching or squishing the video */
  width: auto;
  height: auto;

  transform: translate(-50%,-50%) rotateY(180deg);
  background-color: black; 
}

.playButton{
    position: absolute;
    top: calc(50% - 25px);
    right: calc(50% - 25px);
    color: white;
    z-index: 100;
}

.snapshotButton{
    position: absolute;
    top: calc(50% - 25px);
    left: 8px;
    color: white;
    z-index: 100;
}

.fullscreenButton{
    position: absolute;
    top: 8px;
    right: 8px;
    color: white;
}
</style>

<script>
import ControlPanel from './ControlPanel/ControlPanel';
import backend from '../../services/backend';
import User from '../../services/user'
import Signaler from '../../services/signaling';
import WebRTC from '../../services/webrtc';
import AlertQueue from '../Alerter/AlertQueue';
import CamerafyConfig from '../../CamerafyConfig';
import InputHandler from './InputHandler';
import { VideoPlayer } from './VideoPlayer';
import CamerafyLib from '../../services/CamerafyLib';
import EventBus, { 
  UserLoginEvent,
  UserLogoutEvent,
  CamerafySessionUnreachableEvent, 
  PlayerStateChangeEvent, 
  PlayerPlayEvent, 
  PlayerStopEvent, 
  ApplicationDisconnectEvent, 
  EnableRemoteInputTransmitEvent, 
  DisableRemoteInputTransmitEvent, 
  ExtensionActivateEvent 
  } from '../../services/EventBus'
import { 
  ServerRtcClientReadyServerEvent, 
  ServerRtcOfferServerEvent,
  TimeoutServerEvent,
  SessionTerminateServerEvent
  } from '../../services/CamerafyLib';

/** WebRTC Player states */
export const PS_UNINITIALIZED = 0;
export const PS_INITIALIZED = 1;
export const PS_CONNECTING = 2;
export const PS_CONNECTED = 3;
export const PS_DISCONNECTED = 4;
export const PS_FAILED = 5;

export default {
  name: 'CamerafyWebrtcPlayer',
  props: {
    nofullscreen: Boolean
  },
  components: {
    ControlPanel
  },
  data: function () {
    return {
      VideoPlayer: null,
      fullscreen: false,
      UnregisterInputHandlerFunction: function() { },
      State: PS_UNINITIALIZED,
      paused: true,
      userLoggedIn: false
    }
  },
  mounted: function()
  {
    // grab object reference to <video> element
    this.VideoPlayer = new VideoPlayer(this.$refs.video);

    // initialize webrtc player
    this.InitializePlayer();
  },

  methods:
  {
    ChangeState(newState)
    {
      this.State = newState;
      EventBus.$emit(PlayerStateChangeEvent, newState, !this.paused);
    },

    InitializePlayer: async function()
    {
      const _this = this;

      // resize video on window resize
      window.addEventListener('resize', function() { _this.VideoPlayer.resizeVideo(); }, true);
      // disconnect webrtc client before leaving the page
      window.addEventListener('beforeunload', function() { _this.disconnect(); });

      // listen to fullscreen exit events
      document.addEventListener('fullscreenchange', onFullscreenChange, false);
      document.addEventListener('mozfullscreenchange', onFullscreenChange, false);
      document.addEventListener('MSFullscreenChange', onFullscreenChange, false);
      document.addEventListener('webkitfullscreenchange', onFullscreenChange, false);

      function onFullscreenChange()
      {
          var fullscreenElement = document.fullscreenElement || document.mozFullScreenElement || document.webkitFullscreenElement;
          if (fullscreenElement === null)
          {
              _this.fullscreen = false;
          }
      }

      EventBus.$on(ServerRtcClientReadyServerEvent, this.OnServerRtcClientReadyServerEvent);
      EventBus.$on(ServerRtcOfferServerEvent, this.OnServerRtcOfferServerEvent);
      EventBus.$on(TimeoutServerEvent, this.OnTimeoutServerEvent);
      EventBus.$on(EnableRemoteInputTransmitEvent, this.OnEnableRemoteInputTransmitEvent);
      EventBus.$on(DisableRemoteInputTransmitEvent, this.OnDisableRemoteInputTransmitEvent);
      EventBus.$on(ExtensionActivateEvent, this.OnExtensionActivateEvent);
      EventBus.$on(UserLoginEvent, this.OnUserLogin);
      EventBus.$on(UserLogoutEvent, this.OnUserLogout);
      EventBus.$on(SessionTerminateServerEvent, this.OnSessionTerminateServerEvent);

      this.ChangeState(PS_INITIALIZED);
    },
    
    OnTimeoutServerEvent: function()
    {
      // display user hint
      AlertQueue.warning("Camerafy WebRTC session timeout.", -1);
      this.disconnect();
    },

    OnSessionTerminateServerEvent: function()
    {
      // display user hint
      AlertQueue.warning("Remote Camerafy WebRTC session terminated.", -1);
      this.disconnect();
    },
    OnExtensionActivateEvent: async function(extensionName)
    {
      if(extensionName == null)
      {
        await this.VideoPlayer.exitPictureInPicture();
        // enable remote input
        this.OnEnableRemoteInputTransmitEvent();
      }
      else
      {
        await this.VideoPlayer.requestPictureInPicture();
        // disable remote input
        this.OnDisableRemoteInputTransmitEvent();
      }
    },
    OnEnableRemoteInputTransmitEvent: function()
    {
      this.UnregisterInputHandlerFunction();
      this.UnregisterInputHandlerFunction = InputHandler.RegisterInputEvents(this.VideoPlayer);
    },

    OnDisableRemoteInputTransmitEvent: function()
    {
      this.UnregisterInputHandlerFunction();
      this.UnregisterInputHandlerFunction = function() {};
    },

    OnServerRtcClientReadyServerEvent: function()
    {
      const _this = this;
      // only initialize webrtc stream if user wants to connect
      if(this.State == PS_CONNECTING) 
      {
        if(WebRTC.connect(
          function(stream) 
          { 
            // setup the received remote video stream
            _this.VideoPlayer.changeStream(stream);
            // hook-up the input manager
            const f = InputHandler.RegisterInputEvents(_this.VideoPlayer);
            _this.UnregisterInputHandlerFunction = f;

            _this.VideoPlayer.play();

            // change player to connected state
            _this.ChangeState(PS_CONNECTED); 
          },
          function(err) 
          { 
            console.log(err); 
            _this.disconnect(); 
            _this.ChangeState(PS_FAILED); 
          }))
        {
          // after WebRTC peer initialization, signal server we are ready to receive a call
          CamerafyLib.User.User.ClientRtcReady();
        }
      }
    },

    OnServerRtcOfferServerEvent: function(sdp)
    {
      WebRTC.OnOffer(sdp);
    },

    ToggleFullscreen: function()
    {
        const _videoContainer = this.$refs.videoContainer;
        this.fullscreen = !this.fullscreen;
        
        // put in fullscreen mode
        if(this.fullscreen)
        {
            if(_videoContainer.requestFullscreen) 
            {
                _videoContainer.requestFullscreen();
            } 
            else if(_videoContainer.mozRequestFullScreen) /* Firefox */
            { 
                _videoContainer.mozRequestFullScreen();
            } 
            else if(_videoContainer.webkitRequestFullscreen) /* Chrome, Safari and Opera */
            { 
                _videoContainer.webkitRequestFullscreen();
            } 
            else if(_videoContainer.msRequestFullscreen) /* IE/Edge */
            { 
                _videoContainer.msRequestFullscreen();
            }
        }
        // put in window mode
        else
        {
            if(document.exitFullscreen) 
            {
                document.exitFullscreen();
            } 
            else if(document.mozCancelFullScreen) /* Firefox */
            { 
                document.mozCancelFullScreen();
            } 
            else if(document.webkitExitFullscreen) /* Chrome, Safari and Opera */
            { 
                document.webkitExitFullscreen();
            } 
            else if(document.msExitFullscreen) /* IE/Edge */
            { 
                document.msExitFullscreen();
            }
        }
    },

    Play: function()
    {
      if(this.State === PS_CONNECTING || this.State === PS_CONNECTED)
        return;

      this.connect();
      this.VideoPlayer.play();
      this.paused = false;

      EventBus.$emit(PlayerPlayEvent, this.State, true);
    },

    Stop: function()
    {
      // unhook the input manager
      this.UnregisterInputHandlerFunction();
      this.VideoPlayer.stop();
      this.$emit(PlayerStopEvent, this.State, false);

      this.paused = true;
    },

    connect: async function()
    {
      // make sure we start from clean state
      this.disconnect();
      const Result = await CamerafyLib.Application.Session.Connect(User.ULD);
      if(Result.result == false)
      {
        this.ChangeState(PS_FAILED);
        return;
      }

      this.ChangeState(PS_CONNECTING);
    },

    disconnect: function()
    {
      this.Stop();
      WebRTC.disconnect();

      this.ChangeState(PS_DISCONNECTED);
    },

    OnUserLogin: function()
    {
      this.userLoggedIn = true;
      this.disconnect();
    },

    OnUserLogout: function()
    {
      this.userLoggedIn = false;
      this.disconnect();
    },

    TakeSnapshot: async function()
    {      
      // request full hd snapshot
      CamerafyLib.User.User.RequestCameraSnapshot(1920, 1080, 'png');
    }
  }
}
</script>
