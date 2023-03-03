<template>
    <v-tab-item>
        <v-dialog v-model="showDeleteDialog" persistent max-width="50%">
            <v-card>
                <v-card-title class="headline">Delete language{{selected.length > 1 ? 's' : ''}}?</v-card-title>
                <v-card-text>
                    <span>Attention! Deleting a language will remove all its translations as well. All translation progress will be lost for this langauage and can not be restored after. Do you really want to proceed and delete the following language{{selected.length > 1 ? 's' : ''}}:</span>
                    <ul>
                        <li v-for="(item, index) in selected" :key="index">
                            {{item.lan_name}} [{{item.lan_code}}]
                        </li>
                    </ul>
                </v-card-text>
                <v-card-actions>
                <v-spacer></v-spacer>
                <v-btn text @click="showDeleteDialog = false">Cancel</v-btn>
                <v-btn color="error" @click="deleteSelectedLanguage()"><v-icon>delete</v-icon>Delete</v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>

        <v-dialog v-model="showEditDialog" persistent max-width="50%">
            <v-card v-if="showEditDialog">
                <v-card-title class="headline">Edit Language</v-card-title>
                <v-card-text>
                    <v-form v-model="validInput">
                        <v-row>
                            <v-col>
                                <v-text-field v-model="edit_item.tmp.lan_name" 
                                        label="Language Name" 
                                        counter="128"
                                        :rules="[
                                            () => edit_item.tmp.lan_name.length <= 128 || 'Language name is too long.'
                                        ]" />
                            </v-col>
                            <v-col>
                                <v-text-field v-model="edit_item.tmp.lan_code" 
                                        label="Language Code" 
                                        counter="8"
                                        :rules="[
                                            () => (edit_item.tmp.lan_code !== edit_item.lan_code ? !items.find(e => e.lan_code === edit_item.tmp.lan_code) : true) || `No duplicates allowed. ${edit_item.tmp.lan_code} already exists.`,
                                            () => edit_item.tmp.lan_code.length <= 8 || 'Language code is too long.',
                                            () => edit_item.tmp.lan_code !== '' || 'Language code cannot be empty.'
                                        ]" />
                            </v-col>
                            <v-col>
                                <v-text-field v-model="edit_item.tmp.reg_code" 
                                        label="Region Code" 
                                        counter="4"
                                        :rules="[
                                            () => (edit_item.tmp.reg_code !== edit_item.reg_code ? !items.find(e => e.reg_code === edit_item.tmp.reg_code) : true) || `No duplicates allowed. ${edit_item.tmp.reg_code} already exists.`,
                                            () => edit_item.tmp.reg_code.length <= 4 || 'Region code is too long.',
                                            () => edit_item.tmp.reg_code !== '' || 'Region code cannot be empty.'
                                        ]" />
                            </v-col>
                            <v-col>
                                <v-text-field v-model="edit_item.tmp.reg_name" 
                                        label="Region Name" 
                                        counter="128"
                                        :rules="[
                                            () => edit_item.tmp.reg_name.length <= 128 || 'Region name is too long.'
                                        ]" />
                            </v-col>
                        </v-row>
                    </v-form>
                </v-card-text>
                <v-card-actions>
                <v-spacer></v-spacer>
                <v-btn text @click="showEditDialog = false">Cancel</v-btn>
                <v-btn color="info" :disabled="!validInput" @click="submitEditLanguage()"><v-icon>done</v-icon>Submit</v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>

        <v-data-table
            v-model="selected"
            :headers="headers"
            :items="items"
            :search="search"
            :loading="loading"
            sort-by="lan_name"
            height="calc(100vh - 96px - 154px)"
            item-key="lan_id"
            show-select
            dense
            fixed-header
        >
            <template v-slot:top>
                <v-form v-model="validInput" ref="newLangaugeInputForm">
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
                        <v-col cols="2">
                            <v-text-field v-model="newLanguageName" 
                                label="Language Name" 
                                counter="128" 
                                :rules="[
                                    () => newLanguageName.length <= 128 || 'Language name is too long.'
                                ]"/>
                        </v-col>
                        <v-col cols="1">
                            <v-text-field v-model="newLanguageCode" 
                                label="Language Code" 
                                counter="8" 
                                :rules="[
                                    rules.required,
                                    rules.noduplicatelanguagecode,
                                    () => newLanguageCode.length <= 8 || 'Language code is too long.'
                                ]"/>
                        </v-col>
                        <v-col cols="1">
                            <v-text-field v-model="newLanguageRegionCode" 
                                label="Region Code" 
                                counter="4"
                                :rules="[
                                    rules.required,
                                    rules.noduplicateregioncode,
                                    () => newLanguageRegionCode.length <= 4 || 'Region code is too long.'
                                ]" />
                        </v-col>
                        <v-col cols="2">
                            <v-text-field v-model="newLanguageRegionName" 
                                label="Region Name" 
                                counter="128"
                                :rules="[
                                    () => newLanguageRegionName.length <= 128 || 'Region name is too long.'
                                ]">
                                <v-btn slot="append" @click="add()" :disabled="!validInput" x-small tile outlined><v-icon small>playlist_add</v-icon></v-btn>
                            </v-text-field>
                        </v-col>
                        <v-col cols="4">
                            <v-text-field v-model="search" append-icon="search" label="Search" clearable hide-details />
                        </v-col>
                    </v-row>
                </v-form>
            </template>

            <template v-slot:body="{ items }">
                <tbody>
                    <tr v-for="(item, index) in items" :key="index" @mouseenter="hoveredRow=index" @mouseleave="hoveredRow=-1">
                        <td>
                            <v-checkbox v-model="selected" :value="item" multiple color="white" style="margin:0px;padding:0px" hide-details />
                        </td>
                        <td>{{ item.lan_name }}</td>
                        <td>{{ item.lan_code }}</td>
                        <td>{{ item.reg_name }}</td>
                        <td>
                            <v-row no-gutters>
                                <v-col cols="6">
                                    <span >{{item.reg_code}}</span>
                                </v-col>
                                <v-col cols="6">
                                    <v-tooltip top>
                                        <template v-slot:activator="{ on }">
                                            <v-btn v-show="hoveredRow === index" @click="openEditDialog(item)" x-small tile block v-on="on"><v-icon x-small>edit</v-icon></v-btn>
                                        </template>
                                        <span>Edit this langauge.</span>
                                    </v-tooltip>
                                </v-col>
                            </v-row>
                        </td>
                        <td>
                            <v-tooltip left>
                                <template v-slot:activator="{ on }">
                                    <v-progress-linear :value="item.progress * 100" height="20" v-on="on">
                                        <strong>{{ Math.ceil(item.progress * 100) }}%</strong>
                                    </v-progress-linear>
                                </template>
                                <span v-if="item.missing.length">Translation is missing: {{item.missing.join(', ')}}.</span>
                                <span v-else-if="item.progress === 0">New language. Nothing translated yet.</span>
                                <span v-else>Translation completed.</span>
                            </v-tooltip>
                        </td>
                    </tr>
                </tbody>
            </template>
        </v-data-table>
    </v-tab-item>
</template>
<script>
import TranslationService from './service'
import AlertQueue from '../../components/Alerter/AlertQueue'
import backend from '../../services/backend'
export default {
    name: 'LanguagesTab',
    data: function () {
      return {
          loading: false,
          newLanguageName: "",
          newLanguageCode: "",
          newLanguageRegionName: "",
          newLanguageRegionCode: "",
          showDeleteDialog: false,
          showEditDialog: false,
          validInput: true,
          rules: {
            required: value => !!value || 'Required.',
            noduplicatelanguagecode: value => {
                return !this.items.find(e => e.lan_code === value) || `No duplicates allowed. ${value} already exists.`
            },
            noduplicateregioncode: value => {
                return !this.items.find(e => e.reg_code === value) || `No duplicates allowed. ${value} already exists.`
            },
          },
          hoveredRow: -1,
          search: "",
          headers: [
            { text: 'Language Name', value: 'lan_name', align: 'left' }, 
            { text: 'Language Code', value: 'lan_code', width: '150px', align: 'left' }, 
            { text: 'Region Name', value: 'reg_name', align: 'left' },
            { text: 'Region Code', value: 'reg_code', width: '150px', align: 'left' },
            { text: 'Progress', value: 'progress', align: 'left', width: '250px' } 
          ],
          items: [],
          selected: [],
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

            await this.loadLanguages();
        },

        loadLanguages: async function()
        {
            const languages_progress = await TranslationService.listLanguagesProgress();

            this.items = [];

            for(var chunk = 1;;chunk++)
            {
                const languages = await TranslationService.listLanguagesChunked(chunk);
                // if no more data bailout here
                if(languages === undefined)
                    break;

                languages.forEach(l =>
                { 
                    this.items.push({
                        lan_id: l.id,
                        lan_code: l.lan_code, 
                        lan_name: l.lan_name, 
                        reg_code: l.reg_code,
                        reg_name: l.reg_name,
                        progress: languages_progress[l.lan_code].progress,
                        missing: languages_progress[l.lan_code].missing
                    }); 
                });

                this.loading = false;
            }
        },

        reset: function()
        {
            this.search = "";
            this.newLanguageName = "";
            this.newLanguageCode = "";
            this.newLanguageRegionName = "";
            this.newLanguageRegionCode = "";
            this.selected = [];
            this.validInput = true;
            this.showDeleteDialog = false;
        },
    
        add: async function()
        {
           if(!this.validInput)
                return;

            const response = await TranslationService.createOrUpdateLanguage(this.newLanguageCode, this.newLanguageName, this.newLanguageRegionCode, this.newLanguageRegionName);
            if(response !== null && response.id !== undefined)
            {
                this.items.push({
                    lan_id: response.id,
                    lan_code: this.newLanguageCode, 
                    lan_name: this.newLanguageName, 
                    reg_code: this.newLanguageRegionCode,
                    reg_name: this.newLanguageRegionName,
                    progress: 0.0,
                    missing: []
                }); 
                
                this.newLanguageName = "";
                this.newLanguageCode = "";
                this.newLanguageRegionName = "";
                this.newLanguageRegionCode = "";
            }
            else
            {
                AlertQueue.error(`Failed to create new language '${this.newLanguageName} [${this.newLanguageCode}]'!`);
            }

            // remove validation errors (required fields)
            this.$refs.newLangaugeInputForm.reset();
        },

        deleteSelectedLanguage: async function()
        {
            if(this.selected.length)
            {
                const _this = this;
                this.selected.forEach(async e =>
                {
                    await TranslationService.deleteLanguage(e.lan_id);
                    _this.items = _this.items.filter(function(obj) { return obj.lan_id !== e.lan_id; });
                });

                // clear selection
                this.selected = [];
            }

            // hide dialog
            this.showDeleteDialog = false;
        },

        openEditDialog: function(item)
        {
            this.edit_item = item;
            this.edit_item.tmp = JSON.parse(JSON.stringify(this.edit_item)); // deep copy
            this.showEditDialog = true;
        },

        submitEditLanguage: async function()
        {
            const response = await TranslationService.createOrUpdateLanguage(
                this.edit_item.tmp.lan_code, 
                this.edit_item.tmp.lan_name, 
                this.edit_item.tmp.reg_code, 
                this.edit_item.tmp.reg_name, 
                this.edit_item.lan_id);

            if(response !== null)
            {
                this.edit_item.lan_code = this.edit_item.tmp.lan_code; 
                this.edit_item.lan_name = this.edit_item.tmp.lan_name; 
                this.edit_item.reg_code = this.edit_item.tmp.reg_code; 
                this.edit_item.reg_name = this.edit_item.tmp.reg_name; 
            }
            else
            {
                AlertQueue.error("Failed to submit language changes!");
            }

            this.showEditDialog = false;
        }
    }
}
</script>