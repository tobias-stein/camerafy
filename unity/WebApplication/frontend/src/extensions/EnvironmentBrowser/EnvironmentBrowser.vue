<template>
  <div v-if="icon">
    <v-icon>terrain</v-icon>
  </div>
  <div v-else>
    <v-data-table
      :headers="headers"
      :items="environments"
      :search="search"
      :loading="loading"
      height="calc(100vh - 140px)"
      fixed-header
    >
      <template v-slot:top>
        <v-row>
          <v-col cols="8">
            <v-btn @click="refresh()">Refresh</v-btn>
          </v-col>
          <v-col cols="4">
            <v-text-field
              v-model="search"
              append-icon="search"
              label="Search"
              single-line
              clearable
              hide-details
            ></v-text-field>
          </v-col>
        </v-row>
      </template>

      <template v-slot:item.actions="{ item }">
          <v-btn :loading="item.loadingEnv === true" :disabled="isLoadingEnvironment" @click="loadEnvironment(item)">Load</v-btn>
      </template>
    </v-data-table>
  </div>
</template>

<script>
import EnvironmentService from './service';
import CamerafyLib from '../../services/CamerafyLib';
export default {
    name: 'EnvironmentBrowser',
    props: {
      icon: Boolean,
      extensionOrderIndex: {type: Number, default: 1}
    },  
    data: function () {
      return {
        loading: false,
        search: "",
        isLoadingEnvironment: false,
        headers: [
          { text: 'Preview', value: 'thumbnail', align: 'center', width: '120px' },
          { text: 'Name', value: 'name', align: 'left', width: '200px' },
          { text: 'Brief', value: 'brief', align: 'left' },
          { text: 'Created', value: 'created', align: 'left', width: '120px' },
          { text: 'Updated', value: 'updated', align: 'left', width: '120px' },
          { text: '', value: 'actions', align: 'right', width: '200px' },
        ],
        environments: []
      }
    },

    mounted: function()
    {
      this.refresh();
    },

    methods: {
      refresh: async function()
      {
        this.loading = true;
        this.environments = await EnvironmentService.list();
        this.loading = false;
      },

      loadEnvironment: async function(item)
      {
        this.isLoadingEnvironment = true;
        item.loadingEnv = true;

        const response = await CamerafyLib.Application.Session.ChangeEnvironment(JSON.stringify(item));
      
        this.isLoadingEnvironment = false;
        item.loadingEnv = false;
      }
    }
}
</script>