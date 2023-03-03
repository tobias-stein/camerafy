<template>
  <div v-if="icon">
    <v-icon>local_grocery_store</v-icon>
  </div>
  <div v-else>
    <v-data-table
      :headers="headers"
      :items="sockets"
      :search="search"
      :loading="loading"
      height="calc(100vh - 140px)"
      fixed-header
    >
      <template v-slot:top>
        <v-row>
          <v-col cols="2">
            <v-btn style="width: calc(100% - 5px)" @click="refresh()">Refresh</v-btn>
          </v-col>
          <v-col cols="2">
            <v-btn style="width: calc(100% - 5px)" @click="apply" :loading="applying">Apply</v-btn>
          </v-col>
          <v-col cols="4"/>
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

      <template v-slot:item.socketable="{ item }">
        <v-select 
            :value="item.PreselectedSocketable"
            :items="item.Socketables" 
            item-text="name"
            item-value="name"
            full-width
            outlined 
            dense
            hide-details
            single-line
            clearable
            @change="onChange(item, $event)"
        />
      </template>
    </v-data-table>
  </div>
</template>

<script>
import ProductService from './service';
import CamerafyLib from '../../services/CamerafyLib';
export default {
    name: 'ProductBrowser',
    props: {
      icon: Boolean,
      extensionOrderIndex: {type: Number, default: 0}
    },  
    data: function () {
      return {
        loading: false,
        applying: false,
        search: "",
        headers: [
          { text: 'Socket', value: 'Name', align: 'left', width: '250px' },
          { text: 'Model', value: 'socketable', align: 'left', width: '200px', sortable: false },
        ],
        sockets: [],
        selected: {}
      }
    },

    mounted: async function()
    {
      await this.refresh();
    },

    methods: 
    {
      refresh: async function()
      {
        const _this = this;
        this.loading = true;

        const response = await CamerafyLib.Application.Session.GetEnvironmentSockets();
        this.sockets = response.result;
      
        const models = await ProductService.list();
        
        this.selected = {};
        this.sockets.forEach(e => 
        {
          e.PreselectedSocketable = models.find(f => { return e.PreselectedSocketable == f.id; });
          e.Socketables = e.Socketables.map(s => { return models.find(f => { return s == f.id; }); });

          _this.selected[e.Name] = e.PreselectedSocketable !== undefined ? e.PreselectedSocketable.id : -1;
        });

        this.loading = false;
      },

      onChange: function(item, value)
      {
        this.selected[item.Name] = (value == "" || value === null || value === undefined) ? -1 : item.Socketables.find(e => { return e.name == value; }).id;
      },

      apply: async function()
      {
        this.applying = true;
        Object.entries(this.selected).forEach(async s => 
        {
          const response = await CamerafyLib.Application.Session.ChangeEnvironmentSocket(s[0], s[1]);
        });

        this.applying = false;
      },
    }
}
</script>