<template>
  <v-app>
      <!-- user alerts -->
      <Alerter />

      <!-- Left-side navigation bar -->
      <div v-if="$vuetify.breakpoint.mdAndUp">
      <v-navigation-drawer
        app
        dark
        mini-variant
        mini-variant-width="56"
        permanent
      >
        <!-- Main menu button sits on top -->
        <v-list-item @click="activateExtension(null, null)">
          <v-list-item-action>
            <Connectivity />
          </v-list-item-action>
          <v-list-item-content />
        </v-list-item>  
        <!-- divide main menu button from other extensions -->
        <v-divider></v-divider> 
        <!-- add all extensions -->
        <v-list dense nav>
          <v-list-item-group v-model="activeExtensionIndex" color="grey lighten-2">
          <v-list-item v-for="(extension, index) in extensions" :key="index" @click="activateExtension(index, extension)">
            <v-list-item-action>
              <keep-alive>
                <component v-bind:is="extension" icon></component>
              </keep-alive>
            </v-list-item-action>
            <v-list-item-content />
          </v-list-item>
          </v-list-item-group>
        </v-list>
      </v-navigation-drawer>
      </div>  
    <!-- Top bar -->
    <v-app-bar flat dark app height="48px">
      <div v-if="!$vuetify.breakpoint.mdAndUp">
        <Connectivity />
      </div>
      <v-spacer></v-spacer>
      <Localization />
      <UserLogin />
    </v-app-bar>  
    <!-- Main content -->
    <v-content app>
      <v-container fluid ma-0 pa-0 fill-height>
        <v-row no-gutters>
          <v-col>
            <div v-if="activeExtension !== null">
              <keep-alive>
                <component v-bind:is="activeExtension"></component>
              </keep-alive>
            </div>
            <div v-else>
              <keep-alive>
                <CamerafyWebrtcPlayer />
              </keep-alive>
            </div>
          </v-col>
        </v-row>
      </v-container>
    </v-content>
  </v-app>
</template>
<style>
  html { overflow-y: hidden; }
</style>
<script>
import Config from './CamerafyConfig';
import User from './services/user';
import Signaler from './services/signaling';
import Alerter from './components/Alerter/Alerter';
import MainMenu from './components/MainMenu/MainMenu';
import UserLogin from './components/User/UserLogin';
import Localization from './components/Localization/Localization';
import Connectivity from './components/Connectivity/Connectivity';
import CamerafyWebrtcPlayer from './components/CamerafyWebrtcPlayer/CamerafyWebrtcPlayer';
import AlertQueue from './components/Alerter/AlertQueue'
import Extensions from './extensions/Extensions';
import CamerafyExtensions from './extensions/Extensions';

import EventBus, { 
  CamerafySessionUnreachableEvent, 
  ApplicationConnectEvent, 
  ApplicationDisconnectEvent, 
  UserLoginEvent, 
  UserLogoutEvent, 
  ExtensionActivateEvent 
} from './services/EventBus'
import CamerafyLib, {
  TimeoutServerEvent, 
  SessionTerminateServerEvent 
} from './services/CamerafyLib';

export default {
  name: 'app',
  components: {
    Localization,
    Alerter,
    MainMenu,
    UserLogin,
    Connectivity,
    CamerafyWebrtcPlayer
  },
  data () {
      return {
        extensions: [],
        activeExtensionIndex: null,
        activeExtension: null,
        userSessionId: null,
        sessionId: null
      }
  },
  mounted: function()
  {
    // disable default right-click context menu
    window.document.oncontextmenu = function () { return false; }

    EventBus.$on(UserLoginEvent, this.onUserLogin);
    EventBus.$on(UserLogoutEvent, this.onUserLogout);

    this.loadExtensions();

    this.connect();
  },

  destroyed: function()
  {
    this.disconnect();
  },

  methods: {

    connect: function()
    {
      // connect to message broker
      Signaler.connect(User.UUID, Config.SessionId);
    },

    disconnect: function()
    {
      // disconnect from message broker
      Signaler.disconnect();
    },

    onUserLogin: function()
    {
      this.loadExtensions();
    },

    onUserLogout: function()
    {
      this.loadExtensions();
    },

    loadExtensions: function()
    {
      this.extensions = Extensions.getEligibleExtensions();
    },

    activateExtension: function(index, extension) 
    {
      this.activeExtensionIndex = index;
      this.activeExtension = extension;

      EventBus.$emit(ExtensionActivateEvent, extension);
    },
  }
}
</script>