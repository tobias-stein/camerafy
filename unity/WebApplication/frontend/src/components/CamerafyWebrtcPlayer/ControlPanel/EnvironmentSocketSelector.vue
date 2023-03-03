<template>
    <div class="selector">
        <div v-if="showSelector">
            <v-row dense>
                <v-col cols="2" align="right">
                    <v-btn fab small @click="onSelectPrev"><v-icon>navigate_before</v-icon></v-btn>
                </v-col>
                <v-col cols="8" justify="center" align="center"> 
                    <v-select 
                        :value="selectedSocketName"
                        :items="activeEnvironmentInfoWrapper.env.meta.Sockets" 
                        item-text="Name"
                        item-value="Name"
                        full-width
                        outlined 
                        dense
                        @change="onSelect"
                    />
                </v-col>
                <v-col cols="2">
                    <v-btn small fab @click="onSelectNext"><v-icon>navigate_next</v-icon></v-btn>
                </v-col>
            </v-row>
        </div>
  </div>
</template>

<style>
.selector {
    transform: translate(-50%, 50%);
    opacity: 0.5;
}
</style>

<script>
import CamerafyLib from '../../../services/CamerafyLib';
export default {
  name: 'EnvironmentSocketSelector',
  components: {
  },
  props: {
      envInfo: { type: Object }
  },

  data: function () {
    return {
        showSelector: false,
        selectedSocketName: "",
        selectedSocket: null,
        activeEnvironmentInfoWrapper: null
    }
  },
  mounted: function()
  {
      this.activeEnvironmentInfoWrapper = this.envInfo;
  },

  watch: 
  {
      'activeEnvironmentInfoWrapper.env': function (newValue) 
      {
            try
            {
                this.showSelector = newValue.meta.Sockets.length > 0;
                if(this.showSelector)
                {
                    // select primary camera target if any
                    const primaryTarget = newValue.meta.Sockets.find(x => { return x.Primary; });
                    if(primaryTarget !== null)
                    {
                        // change camera target
                        this.selectedSocketName = primaryTarget.Name;
                        this.onSelect(primaryTarget.Name);
                    }
                }
            }
            catch(e)
            {
                this.showSelector = false;
            }
      }
  },

  methods:
  {
        onSelect: function(item)
        {
            const socket = this.activeEnvironmentInfoWrapper.env.meta.Sockets.find( i => { return i.Name == item});
            this.selectedSocket = socket;

            // change camera target
            CamerafyLib.Camera.CameraController.SetCameraTargetByName(this.selectedSocket.Name);
        },

        onSelectPrev: function()
        {
            if(this.selectedSocket === null)
                return;

            const N = this.activeEnvironmentInfoWrapper.env.meta.Sockets.length - 1; // highest possible index

            // get prev socket index of current selected
            var index = this.activeEnvironmentInfoWrapper.env.meta.Sockets.indexOf(this.selectedSocket) - 1;
            // wrap around if index goes out of range
            if(index < 0) { index = N; }

            // select prev
            this.selectedSocket = this.activeEnvironmentInfoWrapper.env.meta.Sockets[index];
            this.selectedSocketName = this.selectedSocket.Name;

            // change camera target
            CamerafyLib.Camera.CameraController.SetCameraTargetByName(this.selectedSocket.Name);
        },

        onSelectNext: function()
        {
            if(this.selectedSocket === null)
                return;

            const N = this.activeEnvironmentInfoWrapper.env.meta.Sockets.length - 1; // highest possible index

            // get prev socket index of current selected
            var index = this.activeEnvironmentInfoWrapper.env.meta.Sockets.indexOf(this.selectedSocket) + 1;
            // wrap around if index goes out of range
            if(index > N) { index = 0; }

            // select next
            this.selectedSocket = this.activeEnvironmentInfoWrapper.env.meta.Sockets[index];
            this.selectedSocketName = this.selectedSocket.Name;

            // change camera target
            CamerafyLib.Camera.CameraController.SetCameraTargetByName(this.selectedSocket.Name);
        }
  }
}
</script>
