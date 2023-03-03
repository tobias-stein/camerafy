<template>
  <div>
    Sessions
  </div>
</template>
<style>
</style>

<script lang="ts">
import Vue from "vue";
import api from "@/api/api";

export default Vue.extend({
  name: "CamerafySessions",

  data: () => ({
  }),

  mounted: async function()
  {
    const headers = new Headers();
    headers.append('Authorization', `Bearer ${api.auth.AcesssToken()}`);
    
    await fetch("http://localhost:8081/sessions/971aef06-0719-49e8-b7da-f695f8999ad7/join/", { method: "GET", headers: headers, credentials: 'include' })
      .then((response) => 
      { 
        if(response.status != 200)
          throw new Error('Failed to receive session credentials.')
      })
      .then(() => { this.$router.replace({name: 'CamerafyStream'}); })
      .catch((error) => {console.error(error); });
  }
});
</script>