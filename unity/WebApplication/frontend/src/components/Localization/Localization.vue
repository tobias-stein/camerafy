<template>
    <div >
        <v-menu transition="slide-y-transition" bottom offset-y>
            <template v-slot:activator="{ on }">
            <v-btn text tile right v-on="on">
                {{localizer.active_language.lan_code}}
                <v-icon class="pl-2">language</v-icon>
            </v-btn>
            </template>

            <v-list 
                dense 
                two-line 
                width="150px" 
                class="overflow-y-auto" 
                style="max-height: 60vh"> 
                <v-list-item-group v-model="selected_langauge" color="primary">
                    <v-list-item v-for="(item, index) in localizer.supported_languages" :key="index" @click="onLanguageChanged(index)">
                        <v-list-item-content>
                            <v-list-item-title>{{item.lan_name}}</v-list-item-title>
                            <v-list-item-subtitle>{{item.lan_code}}</v-list-item-subtitle>
                        </v-list-item-content>
                    </v-list-item>
                </v-list-item-group>
            </v-list>
        </v-menu>
    </div>
</template>

<script>
import Localizer from './Localizer'

export default {
    name: "Localization",
    data: function() {
        return {
            selected_langauge: 0,
            localizer: Localizer
        }
    },

    mounted: function() 
    {
        this.selected_langauge = this.localizer.activeLanguageIndex;
    },

    methods: {

        onLanguageChanged: async function(index)
        {
            this.localizer.activateLanguageByIndex(index);
        }
    }
}
</script>