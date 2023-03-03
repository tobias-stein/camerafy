<template>
  <div v-if="icon">
    <v-icon>translate</v-icon>
  </div>
  <div v-else style="height: calc(100vh - 48px)">
    <v-tabs v-model="tab" grow height="48px" @change="tabChanged()">
      <v-tab v-for="(item, index) in tabNames" :key="index" >{{ item }}</v-tab>
    </v-tabs>
    <v-tabs-items v-model="tab" ref="tabs">
      <TranslateTab key ="0" />
      <TranslationKeyTab key="1" />
      <LanguagesTab key="2" />
    </v-tabs-items>
  </div>
</template>

<script>
import TranslationService from './service'
import TranslateTab from './TranslateTab'
import TranslationKeyTab from './TranslationKeyTab'
import LanguagesTab from './LanguagesTab'

export default {
    name: 'LocalizationManager',
    components: {
      TranslateTab,
      TranslationKeyTab,
      LanguagesTab
    },
    props: {
      icon: Boolean,
      extensionOrderIndex: {type: Number, default: 4},
      permission_group: {type: String, default: "CamerafyLocalizationManager"}
    },  
    data: function () {
      return {
        tabNames: ['Translate', 'Translation Keys', 'Languages'],
        tab: null,
      }
    },
    methods: {
      tabChanged: function()
      {
        if(this.$refs.tabs)
          this.$refs.tabs.$children[this.tab].refresh();
      }
    }
}
</script>