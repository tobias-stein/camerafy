<template>
    <v-tab-item>
        <v-dialog v-model="showTranslationDialog" persistent max-width="50%">
            <v-card v-if="showTranslationDialog">
                <v-card-title class="headline">Translate</v-card-title>
                <v-card-text>
                    <v-container fluid>
                        <v-textarea ref="sourceTranlationField" v-model="translationCtx.item.source_language.translation" rows="5" :success="translationCtx.source_edit" :filled="translationCtx.source_edit" :readonly="!translationCtx.source_edit" no-resize outlined :label="translationCtx.selected_source_language !== null ? translationCtx.selected_source_language.lan_name : 'Source Language'" />
                        <v-textarea ref="targetTranlationField" v-model="translationCtx.item.target_language.translation" rows="5" :success="!translationCtx.source_edit" :filled="!translationCtx.source_edit" :readonly="translationCtx.source_edit" no-resize outlined :label="translationCtx.selected_target_language !== null ? translationCtx.selected_target_language.lan_name : 'Target Language'" />
                    </v-container>
                </v-card-text>
                <v-card-actions>
                <v-spacer></v-spacer>
                <v-btn text @click="showTranslationDialog = false">Cancel</v-btn>
                <v-btn color="info" @click="submitTranslation()"><v-icon>done</v-icon>Submit</v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>

        <v-data-table ref="table"
            :headers="headers"
            :items="items"
            :search="search"
            :loading="loading"
            sort-by="translation_key.key"
            height="calc(100vh - 96px - 154px)"
            item-key="translation_key.key"
            dense
            fixed-header
        >
            <template v-slot:top>
                <v-row>
                    <v-col cols="3" style="padding-left: 30px">
                        <v-autocomplete 
                            autocomplete="off"
                            v-model="selected_source_language" 
                            @change="onSourceLanguageChanged" 
                            :items="source_languages" 
                            :filter="filterSelectLanguage"
                            label="Source Language"
                            item-value="id"
                            clearable
                            :loading="loading_langs || loading"
                        >
                            <template v-slot:selection="data">{{data.item.lan_name}} [{{data.item.lan_code}}]</template>
                            <template v-slot:item="data">{{data.item.lan_name}} [{{data.item.lan_code}}]</template>
                        </v-autocomplete>
                    </v-col>
                    <v-col cols="3">
                        <v-autocomplete
                            autocomplete="off"
                            v-model="selected_target_language" 
                            @change="onTargetLanguageChanged" 
                            :items="target_languages" 
                            :filter="filterSelectLanguage"
                            label="Target Language"
                            item-value="id"
                            clearable
                            :loading="loading_langs || loading"
                        >
                            <template v-slot:selection="data">{{data.item.lan_name}} [{{data.item.lan_code}}]</template>
                            <template v-slot:item="data">{{data.item.lan_name}} [{{data.item.lan_code}}]</template>
                        </v-autocomplete>
                    </v-col>
                    <v-col cols="2">
                        <v-switch v-model="showOnlyNonTranslated" color="white" inset persistent-hint :hint="showOnlyNonTranslated ? 'Show only non-translated' : 'Show all'" />
                    </v-col>
                    <v-col cols="4">
                        <v-text-field v-model="search" append-icon="search" label="Search" clearable hide-details />
                    </v-col>
                </v-row>
            </template>
            <template v-slot:header.source_language.translation="{ head }">
                <v-tooltip top>
                    <template v-slot:activator="{ on }">
                        <span>
                            {{source_language_header}}
                            <v-btn 
                                v-if="selected_source_language !== null" 
                                x-small text icon class="ml-1" 
                                v-on="on" 
                                @click="onSourceLanguageChanged"
                                @mouseenter="headers[1].sortable=false" 
                                @mouseleave="headers[1].sortable=true">
                                <v-icon small>refresh</v-icon>
                            </v-btn>
                        </span>
                    </template>
                    <span>Refresh</span>
                </v-tooltip>
            </template>
            <template v-slot:header.target_language.translation="{ head }">
                <v-tooltip top>
                    <template v-slot:activator="{ on }">
                        <span>
                            {{target_language_header}}
                            <v-btn 
                                v-if="selected_target_language !== null" 
                                x-small text icon class="ml-1" 
                                v-on="on" 
                                @click="onTargetLanguageChanged"
                                @mouseenter="headers[2].sortable=false" 
                                @mouseleave="headers[2].sortable=true">
                                <v-icon small>refresh</v-icon>
                            </v-btn>
                        </span>
                    </template>
                    <span>Refresh</span>
                </v-tooltip>
            </template>
            <template v-slot:item.source_language.translation="{ item }">
                <div v-if="selected_source_language !== null">
                    <div v-if="item.translation_key.type == 'S'">
                        <v-hover v-slot:default="{ hover }">
                            <v-row no-gutters>
                                <v-col cols="10">
                                    <span :class="item.source_language.id === -1 ? 'transparent--text' : 'white--text'">{{item.source_language.id === -1 ? "." : item.source_language.translation}}</span>
                                    <v-tooltip v-if="item.source_language.id === -1" top>
                                        <template v-slot:activator="{ on }">
                                            <v-btn style="width: calc(100% - 5px)" v-show="hover" @click="openTranslationDialog(item, true)" x-small tile v-on="on"><v-icon x-small>add</v-icon></v-btn>
                                        </template>
                                        <span>Add new translation.</span>
                                    </v-tooltip>
                                </v-col>
                                <v-col cols="1">
                                    <v-tooltip top>
                                        <template v-slot:activator="{ on }">
                                            <v-btn v-show="hover" :disabled="item.source_language.id === -1" @click="openTranslationDialog(item, true)" x-small block tile v-on="on"><v-icon x-small>edit</v-icon></v-btn>
                                        </template>
                                        <span>Edit this translation.</span>
                                    </v-tooltip>
                                </v-col>
                                <v-col cols="1">
                                    <v-tooltip top>
                                        <template v-slot:activator="{ on }">
                                            <v-btn v-show="hover" color="error" @click="deleteTranslation(item, true)" :disabled="item.source_language.id === -1" x-small block tile v-on="on"><v-icon x-small>delete</v-icon></v-btn>
                                        </template>
                                        <span>Remove this translation.</span>
                                    </v-tooltip>
                                </v-col>
                            </v-row>
                        </v-hover>
                    </div>
                    <div v-else-if="item.translation_key.type == 'B'">
                        <v-checkbox v-model="item.source_language.translation" hide-details class="ma-0 pa-0" color="white" @change="onTranslationBoolChanged(item, true)" />
                    </div>
                </div>
            </template>
            <template v-slot:item.target_language.translation="{ item }">
                 <div v-if="selected_target_language !== null">
                    <div v-if="item.translation_key.type == 'S'">
                        <v-hover v-slot:default="{ hover }">
                            <v-row no-gutters>
                                <v-col cols="10">
                                    <span :class="item.target_language.id === -1 ? 'transparent--text' : 'white--text'">{{item.target_language.id === -1 ? "." : item.target_language.translation}}</span>
                                    <v-tooltip v-if=" item.target_language.id === -1" top>
                                        <template v-slot:activator="{ on }">
                                            <v-btn style="width: calc(100% - 5px)" v-show="hover" @click="openTranslationDialog(item, false)" x-small tile v-on="on"><v-icon x-small>add</v-icon></v-btn>
                                        </template>
                                        <span>Add new translation.</span>
                                    </v-tooltip>
                                </v-col>
                                <v-col cols="1">
                                    <v-tooltip top>
                                        <template v-slot:activator="{ on }">
                                            <v-btn v-show="hover" :disabled="item.target_language.id === -1" @click="openTranslationDialog(item, false)" x-small tile block v-on="on"><v-icon x-small>edit</v-icon></v-btn>
                                        </template>
                                        <span>Edit this translation.</span>
                                    </v-tooltip>
                                </v-col>
                                <v-col cols="1">
                                    <v-tooltip top>
                                        <template v-slot:activator="{ on }">
                                            <v-btn v-show="hover" color="error" @click="deleteTranslation(item, false)" :disabled="item.target_language.id === -1" x-small tile block v-on="on"><v-icon x-small>delete</v-icon></v-btn>
                                        </template>
                                        <span>Remove this translation.</span>
                                    </v-tooltip>
                                </v-col>
                            </v-row>
                        </v-hover>
                    </div>
                    <div v-else-if="item.translation_key.type == 'B'">
                        <v-checkbox v-model="item.target_language.translation" hide-details class="ma-0 pa-0" color="white" @change="onTranslationBoolChanged(item, false)" />
                    </div>
                </div>
            </template>
        </v-data-table>
    </v-tab-item>
</template>
<script>
import TranslationService from './service'
import AlertQueue from '../../components/Alerter/AlertQueue';
export default {
    name: 'TransalteTab',
    data: function () {
      return {
          selected_source_language: null,
          selected_target_language: null,
          source_language_header: "Source Language",
          target_language_header: "Target Language",
          source_languages: [],
          target_languages: [],
          loading: false,
          loading_langs: false,
          showTranslationDialog: false,
          showOnlyNonTranslated: false,
          translationCtx: {},
          source_translation: "",
          search: "",
          headers: [
            { text: 'Translation Key', value: 'translation_key.key', align: 'left', width: '30%', sortable: true}, 
            { text: 'Source Language', value: 'source_language.translation', align: 'left', width: '35%', sortable: true }, 
            { text: 'Target Language', value: 'target_language.translation', align: 'left', width: '35%', sortable: true } 
          ],
          items: []
      }
    },

    mounted: function()
    {
        this.refresh();
    },

    watch: {
        showOnlyNonTranslated: function(newState)
        {
            this.filterOnlyNonTranslated(newState);
        }
    },
    methods: {
        refresh: async function()
        {
            const _this = this;
            
            this.reset();

            await this.loadLanguages();
            await this.loadTranslationKeys();         
            
            // refresh source column
            if(this.selected_source_language !== null)
            {
                const found = this.source_languages.find(obj => { return obj.id === _this.selected_source_language; });
                if(found) 
                { 
                    this.onTargetLanguageChanged();
                }
                else
                {
                    this.selected_source_language = null;
                }
            }

            // refresh target column
            if(this.selected_target_language !== null)
            {
                const found = this.target_languages.find( obj => { return obj.id === _this.selected_target_language; });
                if(found) 
                { 
                    this.onTargetLanguageChanged();
                }
                else
                {
                    this.selected_target_language = null;
                }
            }
        },

        loadTranslationKeys: async function()
        {
            this.loading = true;
            const _this = this;
            this.items = [];            
            for(var chunk=1;;chunk++)
            {
                const TKs = await TranslationService.listTranslationKeysChunked(chunk);
                // if no more data bailout here
                if(TKs === undefined)
                    break;

                TKs.forEach(tk => { _this.items.push({translation_key: tk, source_language: {}, target_language: {}}); });
            }

            this.loading = false;
        },

        loadLanguages: async function()
        {
            this.loading_langs = true;

            this.source_languages = [];
            this.target_languages = [];

            for(var chunk = 1;;chunk++)
            {
                const languages = await TranslationService.listLanguagesChunked(chunk);
                // if no more data bailout here
                if(languages === undefined)
                    break;

                this.source_languages = this.source_languages.concat(languages);
                this.target_languages = this.target_languages.concat(languages);
                this.loading_langs = false;
            }

            // Sort langauge selection descending by language name
            this.source_languages.sort(function (a, b) { return ('' + a.lan_name).localeCompare(b.lan_name); })
            this.target_languages.sort(function (a, b) { return ('' + a.lan_name).localeCompare(b.lan_name); })
        },

        reset: function()
        {
            this.source_language_header = "Source Language";
            this.target_language_header = "Target Language";
            this.source_languages = [];
            this.target_languages = [];
            this.search = "";
            this.translationCtx = {};
            this.loading = false;
            this.loading_langs = false;
            this.showTranslationDialog = false;
            this.showOnlyNonTranslated = false;
        },
    
        onSourceLanguageChanged: async function()
        {
            // hack: steint, 28.12.2019: this is necessary to work-around the broken selection bug
            const selected = this.selected_source_language;
            if(selected === undefined )
            {
                this.source_language_header = "Source Language";
                this.selected_source_language = null;
                return;
            }

            this.loading = true;

            var language = null;
            this.source_languages.forEach(lang => { if(lang.id == selected) { language = lang; } });

            this.source_language_header = `${language.lan_name} [${language.lan_code}]`;
            var translations = await TranslationService.getTranslation(language.lan_code);
            translations = translations[language.lan_code];
            this.items.forEach(it => 
            { 
                var trans = translations[it.translation_key.key];

                // convert boolean types from string to boolean
                if(it.translation_key.type == 'B')
                    trans.translation = (trans.translation == "" || trans.translation == "false") ? false : true;

                it.source_language = trans;
            });

            // necessary, otherwise we will lose the filtered (non-translated) items
            if(this.showOnlyNonTranslated && this._items !== undefined)
                this.items = this._items;

            // apply custom sorting
            this.filterOnlyNonTranslated(this.showOnlyNonTranslated);

            this.loading = false;
        },

        onTargetLanguageChanged: async function()
        {
            // hack: steint, 28.12.2019: this is necessary to work-around the broken selection bug
            const selected = this.selected_target_language;
            if(selected === undefined )
            {
                this.target_language_header = "Target Language";
                this.selected_target_language = null;
                return;
            }

            this.loading = true;

            var language = null;
            this.source_languages.forEach(lang => { if(lang.id == selected) { language = lang; } });

            this.target_language_header = `${language.lan_name} [${language.lan_code}]`;
            var translations = await TranslationService.getTranslation(language.lan_code);
            translations = translations[language.lan_code];

            // necessary, otherwise we will lose the filtered (non-translated) items
            if(this.showOnlyNonTranslated && this._items !== undefined)
                this.items = this._items;

            this.items.forEach(it => { it.target_language = translations[it.translation_key.key] });

            // apply custom sorting
            this.filterOnlyNonTranslated(this.showOnlyNonTranslated);
            
            this.loading = false;
        },

        onTranslationBoolChanged: async function(item, source_edit)
        {
            const _this = this;

            var _source_language = null;
            this.source_languages.forEach(lang => { if(lang.id == _this.selected_source_language) { _source_language = lang; } });

            var _target_language = null;
            this.target_languages.forEach(lang => { if(lang.id == _this.selected_target_language) { _target_language = lang; } });

            // convert from bool to string
            if(source_edit)
            {
                item.source_language.translation = item.source_language.translation ? "true" : "false";
            }
            else
            {
                item.target_language.translation = item.target_language.translation ? "true" : "false";
            }

            this.translationCtx = {item: item, source_edit: source_edit, selected_source_language: _source_language, selected_target_language: _target_language};
            
            await this.submitTranslation();

            // convert back from string to bool
            if(source_edit)
            {
                item.source_language.translation = item.source_language.translation == "true" ? true : false;
            }
            else
            {
                item.target_language.translation = item.target_language.translation == "true" ? true : false;
            }
        },

        openTranslationDialog: function(item, source_edit)
        {
            const _this = this;

            var _source_language = null;
            this.source_languages.forEach(lang => { if(lang.id == _this.selected_source_language) { _source_language = lang; } });

            var _target_language = null;
            this.target_languages.forEach(lang => { if(lang.id == _this.selected_target_language) { _target_language = lang; } });

            this.translationCtx = {item: item, source_edit: source_edit, selected_source_language: _source_language, selected_target_language: _target_language};
            this.showTranslationDialog = true;

            // auto focus translation field when dialog opens
            setTimeout(t => 
            { 
                if(source_edit)
                {
                    _this.$refs.sourceTranlationField.focus(); 
                }
                else
                {
                    _this.$refs.targetTranlationField.focus(); 
                }
            }, 100);
        },

        submitTranslation: async function()
        {
            // source language translation modified
            if(this.translationCtx.source_edit)
            {
                // update
                if(this.translationCtx.item.source_language.id !== -1)
                {
                    const response = await TranslationService.updateTranslation(this.translationCtx.item.source_language);
                    if(response === null)
                    {
                        AlertQueue.error("Failed to submit new translation progress.");
                    }
                    else if(response == -1)
                    {
                        AlertQueue.warning("Translation has been changed by extern source. Use the refresh icon in the header of the table column to get the latest revisions.");
                    }
                    else
                    {
                        this.translationCtx.item.source_language.revision = response.revision;
                    }
                }
                // create
                else
                {
                    const response = await TranslationService.createTranslation(this.translationCtx.item.source_language, this.translationCtx.item.translation_key.id, this.translationCtx.selected_source_language.id);
                    if(response === null)
                    {
                        AlertQueue.error("Failed to submit new translation progress.");
                    }
                    else
                    {
                        this.translationCtx.item.source_language.id = response.id;
                        this.translationCtx.item.source_language.revision = response.revision;
                    }
                }
            }
            // target language translation modified
            else
            {
                // update
                if(this.translationCtx.item.target_language.id !== -1)
                {
                    const response = await TranslationService.updateTranslation(this.translationCtx.item.target_language);
                    if(response === null)
                    {
                        AlertQueue.error("Failed to submit new translation progress.");
                    }
                    else if(response == -1)
                    {
                        AlertQueue.warning("Translation has been changed by extern source. Use the refresh icon in the header of the table column to get the latest revisions.");
                    }
                    else
                    {
                        this.translationCtx.item.target_language.revision = response.revision;
                    }
                }
                // create
                else
                {
                    const response = await TranslationService.createTranslation(this.translationCtx.item.target_language, this.translationCtx.item.translation_key.id, this.translationCtx.selected_target_language.id);
                    if(response === null)
                    {
                        AlertQueue.error("Failed to submit new translation progress.");
                    }
                    else
                    {
                        this.translationCtx.item.target_language.id = response.id;
                        this.translationCtx.item.target_language.revision = response.revision;
                    }
                }
            }

            // hide translation dialog
            this.showTranslationDialog = false;
        },

        deleteTranslation: async function(item, source_edit)
        {
            const id = source_edit ? item.source_language.id : item.target_language.id;
            const response = await TranslationService.deleteTranslation(id);
            if(response === null)
            {
                AlertQueue.error("Failed to delete translation progress.");
            }
            else
            {
                if(source_edit)
                {
                    item.source_language.id = -1;
                    item.source_language.translation = "";
                }
                else
                {
                    item.target_language.id = -1;
                    item.target_language.translation = "";
                }
            }
        },

        filterOnlyNonTranslated: function(state)
        {
            if(state)
            {
                const _this = this;
                this._items = this.items;

                const filterNonTranslated = function(item)
                {
                    if(_this.selected_source_language === null && _this.selected_target_language === null)
                        return true;

                    if(_this.selected_source_language !== null && item.source_language.translation === "")
                        return true;

                    if(_this.selected_target_language !== null && item.target_language.translation === "")
                        return true;

                    return false;
                }

                this.items = this.items.filter(filterNonTranslated);
            }
            else
            {
                if(this._items !== undefined)
                    this.items = this._items;
            }
        },

        filterSelectLanguage: function(item, queryText, itemText)
        {
            return (item.lan_name.toLocaleLowerCase().indexOf(queryText.toLocaleLowerCase()) > -1) || (item.lan_code.toLocaleLowerCase().indexOf(queryText.toLocaleLowerCase()) > -1) || (item.reg_name.toLocaleLowerCase().indexOf(queryText.toLocaleLowerCase()) > -1) || (item.reg_name.toLocaleLowerCase().indexOf(queryText.toLocaleLowerCase()) > -1);
        }
    }
}
</script>