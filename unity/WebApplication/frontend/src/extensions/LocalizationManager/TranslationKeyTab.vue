<template>
    <v-tab-item>
        <v-dialog v-model="showDeleteDialog" persistent max-width="50%">
            <v-card>
                <v-card-title class="headline">Delete translation key{{selected.length > 1 ? 's' : ''}}?</v-card-title>
                <v-card-text>
                    <span>Attention! Deleting a translation key will remove all its translations for all languages. All translation progress will be lost for this translation key and can not be restored after. Do you really want to proceed and delete the following translation key{{selected.length > 1 ? 's' : ''}}:</span>
                    <ul>
                        <li v-for="(item, index) in selected" :key="index">{{item.key}}</li>
                    </ul>
                </v-card-text>
                <v-card-actions>
                <v-spacer></v-spacer>
                <v-btn text @click="showDeleteDialog = false">Cancel</v-btn>
                <v-btn color="error" @click="deleteSelectedTranslationKeys()"><v-icon>delete</v-icon>Delete</v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>

        <v-dialog v-model="showEditDialog" persistent max-width="50%">
            <v-card v-if="showEditDialog">
                <v-card-title class="headline">Change Translation Key</v-card-title>
                <v-card-text>
                    <v-form v-model="valid">
                        <v-text-field v-model="edit_item.tmpkey" 
                                label="Translation Key" 
                                counter="256"
                                :rules="[
                                    rules.translationkey, 
                                    rules.noduplicate, 
                                    rules.notailingdot, 
                                    rules.notailingunderscore,
                                    () => edit_item.tmpkey.length <= 256 || 'Translation key is too long.',
                                    () => edit_item.tmpkey !== '' || 'Translation key cannot be empty.'
                                ]" />
                    </v-form>
                </v-card-text>
                <v-card-actions>
                <v-spacer></v-spacer>
                <v-btn text @click="showEditDialog = false">Cancel</v-btn>
                <v-btn color="info" :disabled="!valid" @click="submitEditTranslationKey()"><v-icon>done</v-icon>Submit</v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>

        <v-data-table
            v-model="selected"
            :headers="headers"
            :items="items"
            :search="search"
            :loading="loading"
            sort-by="key"
            height="calc(100vh - 96px - 154px)"
            item-key="key"
            show-select
            dense
            fixed-header
        >
            <template v-slot:top>
                <v-row>
                    <v-col cols="2" style="padding-left: 16px; padding-top: 25px;">
                        <!-- actions -->
                        <v-menu bottom offset-y>
                            <template v-slot:activator="{ on }">
                            <v-btn color="grey lighten-1" tile outlined dark v-on="on"><v-icon right dark class="pr-8">check_box</v-icon>Action</v-btn>
                            </template>
                            <v-list dense>
                            <template v-for="(item, index) in actions">
                                <v-divider v-if="item.divider" :key="index" :inset="item.inset"></v-divider>
                                <v-list-item v-else :key="index" @click="item.action()">
                                <v-list-item-icon>
                                    <v-icon v-text="item.icon"></v-icon>
                                </v-list-item-icon>
                                <v-list-item-content>
                                    <v-list-item-title>{{ item.title }}</v-list-item-title>
                                </v-list-item-content>
                                </v-list-item>
                            </template>
                            </v-list>
                        </v-menu>
                    </v-col>
                    <v-col cols="2"></v-col>
                    <v-col cols="4">
                        <v-form v-model="valid">
                            <v-text-field v-model="newTranslationKey" label="New Translation Key" :rules="[rules.translationkey, rules.noduplicate, rules.notailingdot, rules.notailingunderscore]">
                                <v-btn slot="append" @click="add()" :disabled="!valid || newTranslationKey === ''" x-small tile outlined><v-icon small>playlist_add</v-icon></v-btn>
                            </v-text-field>
                        </v-form>
                    </v-col>
                    <v-col cols="4">
                        <v-text-field v-model="search" append-icon="search" label="Search" clearable hide-details />
                    </v-col>
                </v-row>
            </template>
            <template v-slot:item.key="{ item }">
                <v-hover v-slot:default="{ hover }">
                    <v-row no-gutters>
                        <v-col cols="11">
                            <span >{{item.key}}</span>
                        </v-col>
                        <v-col cols="1">
                            <v-tooltip top>
                                <template v-slot:activator="{ on }">
                                    <v-btn v-show="hover" @click="openEditDialog(item)" x-small tile block v-on="on"><v-icon x-small>edit</v-icon></v-btn>
                                </template>
                                <span>Change this translation key.</span>
                            </v-tooltip>
                        </v-col>
                    </v-row>
                </v-hover>
            </template>
            <template v-slot:item.type="{ item }">
                <v-select v-model="item.type" :items="types" dense hide-details @change="onTypeChanged(item)"/>
            </template>
            <template v-slot:item.progress="{ item }">
                <v-tooltip left>
                    <template v-slot:activator="{ on }">
                        <v-progress-linear :value="item.progress * 100" height="20" v-on="on">
                            <strong>{{ Math.ceil(item.progress * 100) }}%</strong>
                        </v-progress-linear>
                    </template>
                    <span v-if="item.missing.length">Translation is missing: {{item.missing.join(', ')}}.</span>
                    <span v-else-if="item.progress === 0">New translation key. Nothing translated yet.</span>
                    <span v-else>Translation completed.</span>
                </v-tooltip>
            </template>
        </v-data-table>
    </v-tab-item>
</template>
<script>
import TranslationService from './service'
import AlertQueue from '../../components/Alerter/AlertQueue'
import backend from '../../services/backend'
export default {
    name: 'TranslationKeyTab',
    data: function () {
      return {
          loading: false,
          newTranslationKey: "",
          rules: {
            translationkey: value => {
                const pattern = /^[a-zA-Y0-9._]*$/
                return pattern.test(value) || 'Invalid translation key. Only alpha-numeric, period and underscore characters are allowed.'
            },
            noduplicate: value => {
                return !this.items.find(e => e.key === value) || `No duplicates allowed. ${value} already exists.`
            },
            notailingdot: value => {
                return !value.endsWith('.') || "Invalid translation key. No tailing period character allowed."
            },
            notailingunderscore: value => {
                return !value.endsWith('_') || "Invalid translation key. No tailing underscore character allowed."
            }
          },
          search: "",
          headers: [
            { text: 'Translation Key', value: 'key', align: 'left' }, 
            { text: 'Type', value: 'type', align: 'left', width: '130px' }, 
            { text: 'Progress', value: 'progress', align: 'left', width: '250px' } 
          ],
          types: [{text: "String", value: "S"}, {text: "Boolean", value: "B"}],
          items: [],
          selected: [],
          showDeleteDialog: false,
          showEditDialog: false,
          edit_item: null,
          valid: true,
          actions: [
            { _this: this, title: 'Delete', icon: 'delete', action: function() { this._this.showDeleteDialog = this._this.selected.length > 0; } },
          ],
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

            this.reset();

            await this.loadTranslationKeys();
        },

        loadTranslationKeys: async function()
        {
            const TranslationKeys = await TranslationService.listTranslationKeysProgress();
            this.items = [];

            for(var chunk=1;;chunk++)
            {
                const TKs = await TranslationService.listTranslationKeysChunked(chunk);
                // if no more data bailout here
                if(TKs === undefined)
                    break;

                TKs.forEach(tk => 
                { 
                    this.items.push({
                        key: tk.key, 
                        type: tk.type,
                        id: tk.id, 
                        progress: TranslationKeys[tk.key].progress,
                        missing: TranslationKeys[tk.key].missing
                    }); 
                });

                this.loading = false;
            }
        },

        reset: function()
        {
            this.search = "";
            this.newTranslationKey = "";
            this.selected = [];
            this.showDeleteDialog = false;
        },
    
        add: async function()
        {
            if(this.newTranslationKey.length === 0)
                return;

            const response = await TranslationService.addTranslationKey(this.newTranslationKey);
            if(response !== null && response.id !== undefined)
            {
                this.items.push({
                    key: this.newTranslationKey, 
                    type: response.type,
                    id: response.id, 
                    progress: 0.0,
                    missing: [],
                });
                this.newTranslationKey = "";
            }
            else
            {
                AlertQueue.error(`Failed to create new translation key '${this.newTranslationKey}'!`);
            }
        },

        openEditDialog: function(item)
        {
            this.edit_item = item;
            this.edit_item.tmpkey = this.edit_item.key;
            this.showEditDialog = true;
        },

        submitEditTranslationKey: async function()
        {
            const response = await TranslationService.updateTranslationKey(this.edit_item.tmpkey, this.edit_item.id);
            if(response !== null)
            {
                this.edit_item.key = this.edit_item.tmpkey;
            }
            else
            {
                AlertQueue.error("Failed to submit translation key changes!");
            }

            this.showEditDialog = false;
        },

        deleteSelectedTranslationKeys: async function()
        {
            if(this.selected.length)
            {
                const _this = this;
                this.selected.forEach(async e =>
                {
                    await TranslationService.deleteTranslationKey(e.id);
                    _this.items = _this.items.filter(function(obj) { return obj.id !== e.id; });
                });

                // clear selection
                this.selected = [];
            }

            // hide dialog
            this.showDeleteDialog = false;
        },

        onTypeChanged: async function(item)
        {
            await TranslationService.updateTranslationKeyType(item.type, item.id);
        }
    }
}
</script>