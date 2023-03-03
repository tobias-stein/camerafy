import Vue from 'vue'
import Vuetify from 'vuetify'

Vue.use(Vuetify)

const opts = {
    icons: {
        iconfont: 'md',  // 'mdi' || 'mdiSvg' || 'md' || 'fa' || 'fa4'
    },
    theme: {
        dark: true,
    }
}

export default new Vuetify(opts)