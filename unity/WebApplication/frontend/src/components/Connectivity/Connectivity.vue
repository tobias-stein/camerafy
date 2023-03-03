<template>
    <v-icon :color="color" class="pr-2" tile>{{icon}}</v-icon>
</template>

<script>
import EventBus, { PlayerStateChangeEvent, PlayerPlayEvent, PlayerStopEvent } from '../../services/EventBus';

import { PS_UNINITIALIZED, 
         PS_INITIALIZED,
         PS_CONNECTING,
         PS_CONNECTED,
         PS_DISCONNECTED,
         PS_FAILED } from '../CamerafyWebrtcPlayer/CamerafyWebrtcPlayer'

const UNINITIALIZED_ICON = "cancel_presentation";
const FAILURE_ICON = "offline_bolt"

export default {
    name: 'Connectivity',
    data () {
        return {
            icon: UNINITIALIZED_ICON,
            color: "grey",
            state: PS_UNINITIALIZED,
            playing: false
        }
    },
    mounted: function()
    {
        EventBus.$on(PlayerStateChangeEvent, this.update);
        EventBus.$on(PlayerPlayEvent, this.update);
        EventBus.$on(PlayerStopEvent, this.update);
    },

    methods: {

        update: function(state, isPlaying)
        {
            this.state = state;
            this.playing = isPlaying;
            
            switch(this.state)
            {
                case PS_UNINITIALIZED:
                    this.color = "grey";
                    this.icon = UNINITIALIZED_ICON;
                    break;
                case PS_INITIALIZED:
                    this.color = "white";
                    this.icon = `cast${this.playing ? "_connected" : ""}`;
                    break;
                case PS_CONNECTING:
                    this.color = "warning";
                   this.icon = `cast${this.playing ? "_connected" : ""}`;
                    break;
                case PS_CONNECTED:
                    this.color = "success";
                    this.icon = `cast${this.playing ? "_connected" : ""}`;
                    break;
                case PS_DISCONNECTED:
                    this.color = "grey";
                    this.icon = "cast";
                    break;
                case PS_FAILED:
                    this.color = "error";
                    this.icon = FAILURE_ICON;
                    break;
            }

            if(this.state !== PS_UNINITIALIZED && this.state !== PS_FAILED)
            {
                this.icon = `cast${this.playing ? "_connected" : ""}`;
            }
        }
    }
}
</script>