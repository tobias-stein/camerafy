<template>
  <v-app>
    <v-content>
      <router-view />
    </v-content>
  </v-app>
</template>

<script lang="ts">
import Vue from "vue";
import oauth from "@/api/auth/oauth";

export default Vue.extend({
  name: "App",

  components: {
  },

  data: () => ({
  }),
});
</script>
