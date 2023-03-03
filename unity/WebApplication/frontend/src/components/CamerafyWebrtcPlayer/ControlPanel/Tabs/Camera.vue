<template>
  <v-container fluid fill-height>
    
    <!-- Camera Mode -->
    <v-row dense><v-col><span class="headline">Camera Mode</span></v-col></v-row>
    <v-row dense>
      <v-col>
        <v-row dense align="center">
          <v-col cols="4"><v-text-field readonly single-line label="Mode" hide-details/></v-col>
          <v-col cols="8">
            <v-radio-group v-model="freeCameraControl" row @change="cameraModeChanged()">
            <v-radio label="Free Camera" value='true' color="white" hide-details />
            <v-radio label="Orbit Camera" value='false' color="white" hide-details />
          </v-radio-group>
          </v-col>
        </v-row>
      </v-col>
    </v-row>
    <!-- Camera move and turn speed -->
    <v-row><v-col/></v-row> <!-- Vertical Space -->
    <v-row dense><v-col><span class="headline">Camera Speed</span></v-col></v-row>
    <v-row dense align="center">
      <v-col cols="4"><v-text-field readonly single-line label="Move Speed" hide-details/></v-col>
      <v-col cols="8">
        <v-text-field v-model="moveSpeed" type='Number' Min="0" suffix="m/s" hide-details @change="onMoveSpeedChanged()" />
      </v-col>
    </v-row>
    <v-row dense align="center">
      <v-col cols="4"><v-text-field readonly single-line label="Turn Speed" hide-details /></v-col>
      <v-col cols="8">
        <v-text-field v-model="turnSpeed" type='Number' Min="0" suffix="deg/s" hide-details @change="onTurnSpeedChanged()" />
      </v-col>
    </v-row>
    <!-- Camera damping -->
    <v-row><v-col/></v-row> <!-- Vertical Space -->
    <v-row dense><v-col><span class="headline">Camera Damping</span></v-col></v-row>
    <v-row dense align="center">
      <v-col cols="4"><v-text-field readonly single-line label="Linear Damping" hide-details/></v-col>
      <v-col cols="8">
        <v-slider v-model="linearDamping" min="0.0" max="1.0" step="0.01" color="white" hide-details @change="onLinearDampingChanged()">
          <template v-slot:append><span style="width: 40px; text-align: right;">{{linearDamping}}</span></template>
        </v-slider>
      </v-col>
    </v-row>
    <v-row dense align="center">
      <v-col cols="4"><v-text-field readonly single-line label="Angualar Damping" hide-details/></v-col>
      <v-col cols="8">
        <v-slider v-model="angularDamping" min="0.0" max="1.0" step="0.01" color="white" hide-details @change="onAngularDampingChanged()">
          <template v-slot:append><span style="width: 40px; text-align: right;">{{angularDamping}}</span></template>
        </v-slider>
      </v-col>
    </v-row>
    <!-- Miscellaneous -->
    <v-row><v-col/></v-row> <!-- Vertical Space -->
    <v-row dense><v-col><span class="headline">Miscellaneous</span></v-col></v-row>
    <v-row dense align="center">
      <v-col cols="4"><v-text-field readonly single-line label="Inverse Controls" hide-details/></v-col>
      <v-col cols="8">
        <v-checkbox v-model="inverseControls" color="white" hide-details @change="onInvertControlsChanged()" />
      </v-col>
    </v-row>
  </v-container>
</template>

<style>
</style>

<script>
import EventBus from '../../../../services/EventBus'
import CamerafyLib, { InitialCameraValuesServerEvent } from '../../../../services/CamerafyLib';

export default {
  name: 'ControlPanel.CameraTab',
  props: {
  },

  data: function () {
    return {
      freeCameraControl: 'false',
      inverseControls: false,
      moveSpeed: 0.25,
      turnSpeed: 15.0,
      linearDamping: 0.1,
      angularDamping: 1.0
    }
  },
  mounted: function()
  {
    EventBus.$on("InitialCameraValuesServerEvent", this.onInitialCameraValuesServerEvent);
  },

  methods:
  {
    onInitialCameraValuesServerEvent: function(data)
    {
      console.log(data);
      console.log(JSON.parse(data));
    },

    cameraModeChanged: function()
    {
      if(this.freeCameraControl == 'false')
      {
        CamerafyLib.Camera.CameraController.SetOrbitBehaviour(1.0);
        CamerafyLib.Camera.CameraController.SetFreeBehaviour(0.0);
      }
      else
      {
        CamerafyLib.Camera.CameraController.SetOrbitBehaviour(0.0);
        CamerafyLib.Camera.CameraController.SetFreeBehaviour(1.0);
      }
    },

    onMoveSpeedChanged: function()
    {
      CamerafyLib.Camera.CameraController.SetMaxLinearSpeed(this.moveSpeed);
    },

    onTurnSpeedChanged: function()
    {
      CamerafyLib.Camera.CameraController.SetMaxAngularSpeed(this.turnSpeed);
    },

    onLinearDampingChanged: function()
    {
      CamerafyLib.Camera.CameraController.SetLinearDamping(this.linearDamping);
    },

    onAngularDampingChanged: function()
    {
      CamerafyLib.Camera.CameraController.SetAngularDamping(this.angularDamping);
    },

    onInvertControlsChanged: function()
    {
      CamerafyLib.Camera.CameraController.SetInvertControls(this.inverseControls);
    },
  }
}
</script>
