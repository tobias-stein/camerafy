<template>
  <div v-if="icon">
    <v-icon v-if="unseenSnapshots === 0">photo_library</v-icon>
    <v-badge v-else color="primary" left overlap>
      <template v-slot:badge>
        <span>{{unseenSnapshots}}</span>
      </template>
      <v-icon>photo_library</v-icon>
    </v-badge>
  </div>
  <div v-else>
    <SnapshotDetails ref="imgDetails" />

    <v-container fluid ma-0 pa-0 fill-height>
      <v-row align="end">
        <v-col cols="8">
          <!-- actions -->
          <v-menu bottom offset-y>
            <template v-slot:activator="{ on }">
              <v-btn color="grey lighten-1" class="ml-3 pa-2" tile outlined dark v-on="on"><v-icon right dark class="pr-4">check_box</v-icon>{{ $t('snapshotmanager.btn.action') }}</v-btn>
            </template>
            <v-list>
              <template v-for="(item, index) in actions">
                <v-divider v-if="item.divider" :key="index" :inset="item.inset"></v-divider>
                <v-list-item v-else :key="index" @click="item.action()">
                  <v-list-item-icon>
                    <v-icon v-text="item.icon"></v-icon>
                  </v-list-item-icon>
                  <v-list-item-content>
                    <v-list-item-title>{{ $t(item.title) }}</v-list-item-title>
                  </v-list-item-content>
                </v-list-item>
              </template>
            </v-list>
          </v-menu>
        </v-col>
        <v-col cols="4">
          <!-- search -->
          <v-text-field v-model="search" append-icon="search" :label="$t('snapshotmanager.input.search')" single-line hide-details />
        </v-col>
      </v-row>
      <v-row no-gutters>
        <v-col>
          <v-simple-table fixed-header height="calc(100vh - 48px - 72px)">
            <template v-slot:default>
              <col width="30">
              <col width="160">
              <col width="200">
              <col width="120">
              <col width="100">
              <col width="150">
              <col width="100">
              <thead>
                <tr>
                  <th class="text-left"><v-checkbox :indeterminate="selected.length > 0 && selected.length < items.length" :value="allSelected" @change="onSelectAll()" class="ma-0 pa-0" hide-details color="primary" ref="selectAll"/></th>
                  <th class="text-left">{{ $t('snapshotmanager.header.preview') }}</th>
                  <th class="text-left">{{ $t('snapshotmanager.header.title') }}</th>
                  <th class="text-left">{{ $t('snapshotmanager.header.created') }}</th>
                  <th class="text-left">{{ $t('snapshotmanager.header.format') }}</th>
                  <th class="text-left">{{ $t('snapshotmanager.header.resolution') }}</th>
                  <th class="text-left">{{ $t('snapshotmanager.header.size') }}</th>
                  <th class="text-left">{{ $t('snapshotmanager.header.notes') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-show="item.title.includes(search) || item.description.includes(search) || item.created.includes(search)" v-for="(item, index) in items" :key="index">
                  <td>
                    <v-checkbox v-model="selected" :value="item" @change="onSelectionChanged(index)" class="ma-0 pa-0" hide-details color="primary" />
                  </td>
                  <td>
                    <v-hover v-slot:default="{ hover }">
                      <v-img :aspect-ratio="16/9" width="160" height="90" :src="`data:image/${item.thumbnail.format};base64,${item.thumbnail.base64_image}`">
                        <div style="position: relative">
                          <!-- show button -->
                          <v-btn v-show="hover" icon @click="openSnapshotDetails(index)" color="white" opacity="0.5" style="position: absolute; top: 2px; right: 2px;"><v-icon color="white" opacity="0.5">search</v-icon></v-btn>
                          <!-- new indicator -->
                          <div v-show="!item.seen">
                            <v-chip x-small class="ma-2" color="orange" label outlined>New</v-chip>
                          </div>
                        </div>
                      </v-img>
                    </v-hover>
                  </td>
                  <td>{{item.title}}</td>
                  <td>{{item.created}}</td>
                  <td>{{item.format}}</td>
                  <td>{{item.width}} x {{item.height}}</td>
                  <td>{{item.size}}</td>
                  <td>{{item.description}}</td>
               </tr>
              </tbody>
            </template>
          </v-simple-table>
        </v-col>
      </v-row>
    </v-container>
  </div>

</template>

<script>
import SnapshotService from './service'
import SnapshotDetails from './SnapshotDetails'
import JSZip from 'jszip'
import { saveAs } from 'file-saver';
import EventBus, { UserLoginEvent, UserLogoutEvent } from '../../services/EventBus'
import { NewSnapshotAvailableServerEvent } from '../../services/CamerafyLib'
import AlertQueue from '../../components/Alerter/AlertQueue'

export default {
    name: 'SnapshotBrowser',
    components: {
      SnapshotDetails
    },
    props: {
      icon: Boolean,
      extensionOrderIndex: {type: Number, default: 3},
      requires_authentication: {type: Boolean, default: true},
    },  
    data: function () {
      return {
        actions: [
          { title: 'snapshotmanager.action.download', icon: 'archive', action: this.download },
          { title: 'snapshotmanager.action.share', icon: 'share', action: this.share },
          { divider: true, inset: false },
          { title: 'snapshotmanager.action.delete', icon: 'delete', action: this.delete },
        ],
        search: "",
        allSelected: false,
        items: [],
        selected: [],
        openDetails: null,
        unseenSnapshots: 0
      }
    },
    mounted: function()
    {      
      this.refresh();

      EventBus.$on(UserLoginEvent, this.refresh);
      EventBus.$on(UserLogoutEvent, this.refresh);
      EventBus.$on(NewSnapshotAvailableServerEvent, this.onNewSnapshotAvailableServerEvent);
    },

    methods: {

      refresh: async function()
      {
        this.search = "";
        this.allSelected = false;
        this.openDetails = null;
        this.items = [];
        this.selected = [];

        const data = await SnapshotService.list();
        this.items = data !== null ? data : [];

        this.refreshUnseen();
      },

      refreshUnseen: function()
      {
        this.unseenSnapshots = this.items.filter(image => { return image.seen === false; }).length;
        this.x += 1;
      },

      onNewSnapshotAvailableServerEvent: async function(SnapshotId)
      {
        const snapshot = await SnapshotService.retrieve(SnapshotId);
        this.items.push(snapshot);

        this.refreshUnseen();
      },

      openSnapshotDetails: async function(index)
      {
        await this.$refs.imgDetails.showImage(this.items[index]);
        this.items[index].seen = true;
        this.refreshUnseen();
      },

      onSelectionChanged: function(index)
      {
        this.allSelected = this.selected.length === this.items.length ? true : undefined;
      },

      onSelectAll: function()
      {
        if(this.allSelected)
        {
          this.selected = [];
          this.allSelected = undefined;
        }
        else
        {
          this.selected = this.items;
          this.allSelected = true;
        }
      },

      download: async function()
      {
        if(this.selected.length)
        {
          // display user hint
          AlertQueue.info(`Start downloading ${this.selected.length} snapshot${this.selected.length > 1 ? 's' : ''}...`);

          // zip package
          const zip = new JSZip();

          const promises = this.selected.map(async image => 
          {
            return new Promise(async (resolve) => {
              // begin download image data...
              const data = await SnapshotService.download(image.id);
              // cache downloaded image data
              image.base64_image = data.result.base64_image;
              // add to image to zip package
              zip.file(`${image.title}.${image.format}`, image.base64_image, { base64: true });
              // resolve this promise
              resolve();
            });
          });
          // download all selected images in parallel
          await Promise.all(promises);

          // generate zip package and execute download
          zip.generateAsync({type:"blob"}).then(function(content) { saveAs(content, "Camerafy-Snapshots.zip"); });
        }
      },

      share: function()
      {

      },

      delete: async function()
      {
        if(this.selected.length)
        {
          // display user hint
          AlertQueue.info(`${this.selected.length} snapshot${this.selected.length > 1 ? 's' : ''} deleted.`);

          const _selected = this.selected;
          // clear selection
          this.selected = [];

          const _this = this;
          _selected.forEach(async image => {
            // remove from items list
            _this.items = _this.items.filter(function(obj) { return obj.id !== image.id; });
            this.refreshUnseen();

            // tell backend to remove item from database
            const response = await SnapshotService.delete(image.id); 
          });
        }
      }

    }
}
</script>