<template>
  <div v-if="icon">
    <v-icon>menu</v-icon>
  </div>
  <div v-else>
    Main Menu
  </div>

</template>

<script>
export default {
    name: 'MainMenu',
    props: {
      icon: Boolean
    },  
    data: function () {
      return {
      }
    },
    mounted: function()
    {
    }
}
</script>