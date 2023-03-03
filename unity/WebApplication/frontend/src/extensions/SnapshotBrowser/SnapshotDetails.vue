<template>
  <v-dialog v-model="open" hide-overlay max-width="80%" :key="componentKey">
    <div v-if="image === null || image.base64_image === undefined">

      <!-- download indicator -->
      <v-card color="primary" dark>
        <v-card-text class="text-center">
          Downloading image data ...
          <v-progress-linear indeterminate color="white" class="mb-0" />
        </v-card-text>
      </v-card>
    </div>
    <div v-else ref="imageContainer" style="position: relative">
      <!-- image -->
      <v-img :aspect-ratio="image.width/image.height" :src="`data:image/${image.format};base64,${image.base64_image}`" />
    
      <!-- fullscreen toggle -->
      <v-hover v-slot:default="{ hover }">
        <v-btn tile icon class="imageFullscreenButton" v-bind:style="{opacity: hover ? 1.0 : 0.25}" @click="toggleFullscreen()">
          <v-icon v-show="!fullscreen">aspect_ratio</v-icon>
          <v-icon v-show="fullscreen">featured_video</v-icon>
        </v-btn>
      </v-hover>

      <!-- close button -->
      <v-hover v-slot:default="{ hover }">
        <v-btn tile icon class="closeButton" v-bind:style="{opacity: hover ? 1.0 : 0.25}" @click="hideImage()">
          <v-icon>cancel_presentation</v-icon>
        </v-btn>
      </v-hover>
    </div>
  </v-dialog>
</template>

<style>

.imageFullscreenButton{
    position: absolute;
    top: 5px;
    right: 40px;
    color: white;
    z-index: 100;
}

.closeButton{
    position: absolute;
    top: 5px;
    right: 5px;
    color: white;
    z-index: 100;
}
</style>

<script>
import SnapshotService from './service'
export default {
    name: 'SnapshotDetails',
    data: function () {
      return {
        componentKey: 0,
        open: false,
        image: null,
        fullscreen: false
      }
    },

    methods: {
      showImage: async function(image)
      {
        if(image === null || image === undefined)
          return;

        // store reference to image
        this.image = image;

        // show image dialog
        this.open = true;

        // download image if necessary
        if(this.image.base64_image === undefined)
        {
          const data = await SnapshotService.download(this.image.id);
          this.image.base64_image = data.result.base64_image;
          this.componentKey += 1;  
        }
      },
      hideImage: function()
      {
        if(this.fullscreen)
          this.toggleFullscreen();

        this.open = false;        
      },
      toggleFullscreen()
      {
          this.fullscreen = !this.fullscreen;
          
          // put in fullscreen mode
          if(this.fullscreen)
          {
              if(this.$refs.imageContainer.requestFullscreen) 
              {
                  this.$refs.imageContainer.requestFullscreen();
              } 
              else if(this.$refs.imageContainer.mozRequestFullScreen) /* Firefox */
              { 
                  this.$refs.imageContainer.mozRequestFullScreen();
              } 
              else if(this.imageContainer.webkitRequestFullscreen) /* Chrome, Safari and Opera */
              { 
                  this.$refs.imageContainer.webkitRequestFullscreen();
              } 
              else if(this.imageContainer.msRequestFullscreen) /* IE/Edge */
              { 
                  this.$refs.imageContainer.msRequestFullscreen();
              }
          }
          // put in window mode
          else
          {
              if(document.exitFullscreen) 
              {
                  document.exitFullscreen();
              } 
              else if(document.mozCancelFullScreen) /* Firefox */
              { 
                  document.mozCancelFullScreen();
              } 
              else if(document.webkitExitFullscreen) /* Chrome, Safari and Opera */
              { 
                  document.webkitExitFullscreen();
              } 
              else if(document.msExitFullscreen) /* IE/Edge */
              { 
                  document.msExitFullscreen();
              }
          }

          return this.fullscreen;
      }
    }
}
</script>