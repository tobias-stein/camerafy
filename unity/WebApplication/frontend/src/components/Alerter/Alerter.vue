<template>
    <div class="alert-container">
        <v-alert
            v-model="isAlerting"
            class="alert" 
            border="left" 
            :type="alert.servity"
            transition="fade-transition"
            prominent
            dense
            dark
            tile
            dismissible>
            {{alert.message}}
        </v-alert>
    </div>
</template>
<style>
.alert-container {
    position: relative;
}

.alert {
    position: absolute;
    width: 60%;
    left: 50%;
    margin-right: -50%;
    transform: translate(-50%, 10px);
    z-index: 999;
}
</style>
<script>
import AlertQueue from './AlertQueue'
export default {
    name: 'Alerter',
    data () {
        return {
            isAlerting: false,
            alert: {messge: "", servity: "info", timeout: 0},
            alerts: AlertQueue.queue,
            timeout: null
        }
    },
    
    watch: {
        alerts: function()
        {
            this.alertNext();
        },
        isAlerting: function(newValue)
        {
            if(newValue === false)
            {
                clearTimeout(this.timeout);
                this.alertNext();
            }
        }
    },

    methods: 
    {
        alertNext: function()
        {
            if(this.isAlerting === false && AlertQueue.queue.length > 0)
            {
                const _this = this;
                // hack: somehow this is necessary for properly reset isAlerting state
                setTimeout(function() 
                {
                    _this.alert = AlertQueue.queue.shift();
                    _this.isAlerting = true;

                    if(_this.alert.timeout > 0)
                        _this.timeout = setTimeout(function() { _this.isAlerting = false; }, _this.alert.timeout);
                }, 1);
            }
        }
    }
}
</script>