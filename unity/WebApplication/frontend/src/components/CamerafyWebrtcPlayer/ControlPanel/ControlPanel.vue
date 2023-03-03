<template>
  <div>
      <!-- right hand-side expandable panel -->
      <div v-if="!open">
        <!-- panel is closed, only show small icon on the bottom-right -->
        <v-btn class="control-panel-button" color="transparent" tile @click="open=true"><v-icon x-large>apps</v-icon></v-btn>
      </div>
      <div v-else>
          <!-- panel is open but not expanded -->
          <div v-if="!expand">
            <v-navigation-drawer absolute permanent right mini-variant mini-variant-width="72px" class="control-panel">
              <template v-slot:prepend>
                <v-list-item style="background-color: #212121; height: 48px;" @click="expand=true">
                  <v-list-item-icon><v-icon>keyboard_arrow_left</v-icon></v-list-item-icon>
                  <v-list-item-content />
                </v-list-item>
              </template>
              <v-divider></v-divider>
              <v-list class="pa-0" tile>   
                <v-list-item-group v-model="selected" mandatory color="grey lighten-2">          
                  <v-list-item v-for="tab in tabs" :key="tab.tab" style="height: 48px;" @click="onSelect(tab)">
                    <v-list-item-icon><v-icon :color="tab.id == selected ? 'primary' : 'white'">{{ tab.icon }}</v-icon></v-list-item-icon>
                    <v-list-item-content></v-list-item-content>
                  </v-list-item>
                </v-list-item-group>
              </v-list>
              <template v-slot:append>
                <v-list-item style="background-color: #212121; height: 64px;" @click="open=false">
                  <v-list-item-icon><v-icon x-large>apps</v-icon></v-list-item-icon>
                  <v-list-item-content /> 
                </v-list-item>
              </template>
            </v-navigation-drawer>
          </div>
          <!-- panel is open and expanded -->
          <div v-else>
            <v-card v-show="open" tile class="control-panel-expanded">
                <v-card-title class="control-panel-title">
                    <v-btn tile style="min-width: 90px; min-height: 48px;" @click="expand=false"><v-icon>keyboard_arrow_right</v-icon></v-btn>
                    <span>{{tabs[selected].title}}</span>
                </v-card-title>
                <v-card-text class="pa-0">
                  <v-tabs v-model="selected" vertical>
                    <v-tab v-for="tab in tabs" :key="tab.id" width="128px">
                      <v-icon>{{tab.icon}}</v-icon>
                    </v-tab>
                    <v-tab-item v-for="tab in tabs" :key="tab.id">
                        <v-card flat style="overflow-y: auto;" class="pa-0" height="calc(100vh - 116px)">
                          <v-card-text>
                            <keep-alive>
                              <component v-bind:is="tab.tab"></component>
                            </keep-alive>
                          </v-card-text>
                        </v-card>
                    </v-tab-item>
                  </v-tabs>
                      
                </v-card-text>
            </v-card>
          </div>
      </div>

      <!-- camera target selection -->
      <EnvironmentSocketSelector 
        class="env-socket-selector"
        :envInfo="ActiveEnvironmentInfoWrapper"
      />
  </div>
</template>

<style>

.env-socket-selector {
  position: absolute;
  left: 50%;
  bottom: 25px;
}

.control-panel-button {
  position: absolute;
  min-width: 64px;
  min-height: 64px;

  right: 0px;
  bottom: 0px;
  opacity: 0.1;
}

.control-panel-button:hover {
  opacity: 0.66;
}

.control-panel {
  position: absolute;
  opacity: 0.7;
  z-index: 99;
}

.control-panel-expanded {
  position: absolute;
  max-width: 800px;
  right: 0px;
  width: 100%;
  height: 100%;
  opacity: 0.7;
  z-index: 99;
}

.control-panel-title {
  padding: 0px;
  background-color: #212121;
  opacity: 1;
}
</style>

<script>
import CameraTab from './Tabs/Camera';
import TouchpointTab from './Tabs/Touchpoint';
import EnvironmentSocketSelector from './EnvironmentSocketSelector';
import EventBus, { 
  PlayerStateChangeEvent
} from '../../../services/EventBus';
import CamerafyLib, { 
  EnvironmentChangedServerEvent
} from '../../../services/CamerafyLib';

import { PS_UNINITIALIZED, 
         PS_INITIALIZED,
         PS_CONNECTING,
         PS_CONNECTED,
         PS_DISCONNECTED,
         PS_FAILED 
} from '../CamerafyWebrtcPlayer'

export default {
  name: 'ControlPanel',
  components: {
    CameraTab,
    TouchpointTab,
    EnvironmentSocketSelector
  },

  data: function () {
    return {
        open: false,
        expand: false,
        selected: 0,
        ActiveEnvironmentInfoWrapper: { env: null },
        tabs: [
          { title: 'Camera Mode', icon: '3d_rotation', tab: 'CameraTab', id: 0 },
          { title: 'Touchpoint Mode', icon: 'control_camera', tab: 'TouchpointTab', id: 1 }
        ]
    }
  },
  mounted: function()
  {
    // register event handler
    EventBus.$on(EnvironmentChangedServerEvent, this.onEnvironmentChangedServerEvent); 
    EventBus.$on(PlayerStateChangeEvent, this.onPlayerStateChangeEvent); 
  },

  methods:
  {
    onEnvironmentChangedServerEvent: function(evnInfo)
    {
      this.ActiveEnvironmentInfoWrapper.env = JSON.parse(evnInfo);
    },

    onPlayerStateChangeEvent: async function(state)
    {
      if(state === PS_CONNECTED)
      {
        // get current active environment information
        const response = await CamerafyLib.User.User.GetEnvironmentInfo();
        this.ActiveEnvironmentInfoWrapper.env = response.result;
      }
    },

    onSelect: function(e)
    {
    }
  }
}
</script>
