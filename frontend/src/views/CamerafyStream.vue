<template>
  <div>

    <v-dialog v-model="camerafySessionDialog" persistent max-width="50%" dark>
      <!-- Initialized -->
      <v-card v-if="camerafySession.state == 1">
        <v-card-title class="headline justify-center">Join Camerafy session.</v-card-title>
        <v-card-text class="text-center">
          Press button to connect to remote session.
        </v-card-text>
        <v-card-actions class="justify-center">
          <v-btn color="primary" @click="onConfirmSessionDialog()">Connect</v-btn>
        </v-card-actions>
      </v-card>
      <!-- Connecting -->
      <v-card v-if="camerafySession.state == 2">
        <v-card-title class="headline justify-center">Join Camerafy session.</v-card-title>
        <v-card-text class="text-center">
          Connecting...
          <v-progress-linear indeterminate color="white" class="mb-0" />
        </v-card-text>
      </v-card>
      <!-- Timeout -->
      <v-card v-if="camerafySession.state == 4">
        <v-card-title class="headline justify-center">Timeout.</v-card-title>
        <v-card-text class="text-center">
          Remote session seems no active. 
        </v-card-text>
        <v-card-actions class="justify-center">
          <v-btn color="warning" @click="onConfirmSessionDialog()">Try again</v-btn>
        </v-card-actions>
      </v-card>
      <!-- Disconnected -->
      <v-card v-if="camerafySession.state == 5">
        <v-card-title class="headline justify-center">Disconnected.</v-card-title>
        <v-card-text class="text-center">
          Client is disconnected from remote session.
        </v-card-text>
        <v-card-actions class="justify-center">
          <v-btn color="warning" @click="onConfirmSessionDialog()">Reconnect</v-btn>
        </v-card-actions>
      </v-card>
      <!-- Terminated -->
      <v-card v-if="camerafySession.state == 5">
        <v-card-title class="headline justify-center">Session terminated.</v-card-title>
        <v-card-text class="text-center">
          The remote Camerafy session has been terminated.
        </v-card-text>
      </v-card>
    </v-dialog>

    <div id="videoplayer">
        <video id="video" ref="video">
          Sorry, but your browser does not support video.
        </video>
    </div>
  </div>
</template>
<style>
#videoplayer {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  align-items: center;
  justify-content: center;
  display: flex;
  background-color: #323232;
}

#video {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
} 
</style>

<script lang="ts">
import Vue from "vue";
import api from "@/api/api";
import VideoPlayer from "@/components/CamerafyStream/VideoPlayer";
import ProxyClient, { ProxyClientStatus } from "@/components/CamerafyStream/ProxyClient";
import EventReflector from "@/components/CamerafyStream/CamerafyEventReflector";
import CamerafySessionObserver, { CamerafySessionState } from '@/components/CamerafyStream/CamerafySessionObserver';

export default Vue.extend({
  name: "CamerafyStream",

  data: () => ({
    camerafySession: new CamerafySessionObserver(),
    videoPlayer: new VideoPlayer(),
    proxyClient: new ProxyClient(),
    camerafySessionDialog: true,
    hideScrollbarSyle: document.createElement('style'),
  }),

  created: function()
  {
    // disable rightclick context menu
    window.addEventListener('contextmenu', this.contextmenuListener, false);

    // force hide scrollbars
    this.hideScrollbarSyle.innerHTML = "::-webkit-scrollbar{display:none;}";
    document.head.appendChild(this.hideScrollbarSyle);
  },

  mounted: function()
  {
    this.initiailze();
  },

  beforeDestroy: async function()
  {
    // make sure we disconnect all still on-going connections
    await this.proxyClient.disconnect(true);

    // enable rightclick context menu
    window.removeEventListener('contextmenu', this.contextmenuListener);
    // no longer force hide scrollbars
    document.head.removeChild(this.hideScrollbarSyle);
  },

  methods: 
  {
    contextmenuListener: function(event) { event.preventDefault(); },

    initiailze: async function()
    {
      // setup video player
      this.videoPlayer.initialize(this.$refs.video);

      // create a new proxy client with the given session id
      this.proxyClient.initialize(this.$cookies.get('camfy_broker_url').replaceAll('"', ''), this.$cookies.get('camfy_broker_usr'), this.$cookies.get('camfy_broker_pwd'));

      // initialize camerafy session observer
      this.camerafySession.initialize(this.proxyClient);
      
      // subscribe to 'session connect attempt timeouts'
      this.proxyClient.on("onSessionConnectTimeout", () => { this.camerafySession.state = CamerafySessionState.Timeout; });

      // subscribe to 'new streams' event
      this.proxyClient.on("onNewStreams", (streams : Array<MediaStream>) => 
      { 
        console.log(streams);
        this.videoPlayer.setStream(streams[0]);
        // once we got the stream we can hide the dialog.
        this.camerafySession.state = CamerafySessionState.Connected;
      });

      // serve input events from video player
      {
        // Keyboard
        this.videoPlayer.on("onKeyboardInput", async (isDown : boolean, isRepeat : boolean, keyCode : any, character : string) => 
        {
          await this.proxyClient.sendEvent(EventReflector.Input.RemoteInputModule.ProcessRemoteKeyboardInput(isDown, isRepeat, keyCode, character));
        });

        // Mouse
        this.videoPlayer.on("onMouseInput", async (Xpos : number, Ypos : number, Button : any) => 
        {
          await this.proxyClient.sendEvent(EventReflector.Input.RemoteInputModule.ProcessRemoteMouseInput(Xpos, Ypos, Button));
        });

        this.videoPlayer.on("onMouseWheelInput", async (Xpos : number, Ypos : number) => 
        {
          await this.proxyClient.sendEvent(EventReflector.Input.RemoteInputModule.ProcessRemoteMouseWheelInput(Xpos, Ypos));
        });

        // Touch
        this.videoPlayer.on("onTouchInput", async (NumTouches : number, TouchId : Array<any>, Phase : Array<any>, XPos : Array<any>, YPos : Array<any>, Force : Array<any>) => 
        {
          await this.proxyClient.sendEvent(EventReflector.Input.RemoteInputModule.ProcessRemoteTouchInput(NumTouches, TouchId, Phase, XPos, YPos, Force));
        });
      }

      // try to establish a connection to Stomp message broker and WebRTC.
      await this.proxyClient.connectSignalingServer(api.user.sessionId, this.$cookies.get('camfy_session_id'))
        // after we connected to the signaling server, we set this session to be initialized
        .then(() => { this.camerafySession.state = CamerafySessionState.Initialized; })
        .catch((error) => { 
              console.error(`Failed to connect to signaling server: ${error}`); 
              this.camerafySession.state =  CamerafySessionState.Failed;
        });
    },

    onConfirmSessionDialog: async function()
    {
      switch(this.camerafySession.state)
      {
        case CamerafySessionState.Initialized:
        case CamerafySessionState.Disconnected:
        case CamerafySessionState.Timeout:
          // set session state to 'Connecting'
          this.camerafySession.state = CamerafySessionState.Connecting;

          this.videoPlayer.setStream(null);

          // try to connect
          await this.proxyClient.connectSession(api.user.sessionId)
            // on success trigger play on video player 
            .then(() => { this.videoPlayer.play(); })
            // on error show error and set session to 'Disconnected' state
            .catch((error) => { 
              console.error(`Failed to connect to session (ID: ${this.$cookies.get('camfy_session_id')}): ${error}`); 
              this.camerafySession.state = this.proxyClient.state === ProxyClientStatus.Failed ? CamerafySessionState.Failed : CamerafySessionState.Disconnected;
            });
          
          break;
      }
    }
  }
});
</script>