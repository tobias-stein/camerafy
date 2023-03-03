import Vue from 'vue'
import vuetify from "./plugins/vuetify"
import App from './App.vue'
import CamerafyExtensions from './extensions/Extensions'

Vue.config.productionTip = false

Vue.mixin({
  methods: {
    $t: function (tk) { return window.$t(tk); }
  }
})

new Vue({
  el: '#app',
  vuetify,
  render: h => h(App),
}).$mount('#app')
