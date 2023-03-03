<template>
  <div v-if="icon">
    <v-icon>control_camera</v-icon>
  </div>
  <div v-else>
  </div>
</template>

<script>
export default {
    name: 'TouchpointManager',
    props: {
      icon: Boolean,
      extensionOrderIndex: {type: Number, default: 2}
    },  
    data: function () {
      return {
      }
    },
}
</script>