<template>
    <div style="z-index: 999">
        <div v-if="state === 0">
            <v-btn text tile right @click="showLoginMask = true;">
                Login
                <v-icon class="pl-2">account_circle</v-icon>
            </v-btn>
        </div>
        <div v-else>
            <v-menu
                transition="slide-y-transition"
                bottom
                offset-y
            >
                <template v-slot:activator="{ on }">
                <v-btn text tile right v-on="on">
                    {{username}}
                    <v-icon class="pl-2">account_circle</v-icon>
                </v-btn>
                </template>

                <v-list>
                    <v-list-item @click="Logout()">
                        <v-list-item-avatar><v-icon>clear</v-icon></v-list-item-avatar>
                        <v-list-item-title>Logout</v-list-item-title>
                    </v-list-item>
                </v-list>
            </v-menu>
        </div>

        <v-dialog v-model="showLoginMask" persistent max-width="400px">
            <v-card>
                <v-card-title primary-title>
                    <v-container fluid>
                        <v-row no-gutters><v-col cols="12"><v-icon class="d-flex justify-center" size="50">account_circle</v-icon></v-col></v-row>
                        <v-row no-gutters><v-col cols="12"><span class="headline d-flex justify-center">User Login</span></v-col></v-row>
                    </v-container>
                </v-card-title>
                <v-card-text>
                    <v-container pa-0 ma-0>
                        <v-alert v-show="failed" type="error">Login failed.</v-alert>
                        <v-alert v-show="state === 1" type="success">Login successful.</v-alert>
                        <v-alert v-show="state === 2" type="info">Logout successful.</v-alert>
                        <v-row no-gutters v-show="state === 0">
                            <v-col cols="12"><v-text-field v-model="username" label="Username" required></v-text-field></v-col>
                            <v-col cols="12"><v-text-field v-model="password" label="Password" type="password" required></v-text-field></v-col>
                        </v-row>
                    </v-container>
                </v-card-text>
                <v-card-actions v-show="state === 0">
                    <v-spacer></v-spacer>
                    <v-btn v-show="!requireLogin" color="grey darken-1" text @click="Cancel()">Cancel</v-btn>
                    <v-btn color="blue darken-1" text @click="Login()">Login</v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>
    </div>
</template>

<script>
import backend from '../../services/backend'
import CamerafyUser from '../../services/user'
import CamerafyConfig from '../../CamerafyConfig'
import EventBus, { EnableRemoteInputTransmitEvent, DisableRemoteInputTransmitEvent } from '../../services/EventBus'

const S_CLEAN = 0;
const S_LOGGEDIN = 1;
const S_LOGGEDOUT = 2;

export default {
    name: "UserLogin",
    data: function() {
        return {
            requireLogin: CamerafyConfig.UserLogin,
            showLoginMask: false,
            state: S_CLEAN,
            failed: false,
            username: "",
            password: ""
        }
    },

    mounted: function() {
        // force user login, if required.
        this.showLoginMask = CamerafyConfig.UserLogin;
    },

    watch: {

        showLoginMask: function(val)
        {
            if(val === true)
            {
                EventBus.$emit(DisableRemoteInputTransmitEvent);
            }
        }
    },

    methods: {

        Login: async function()
        {
            this.failed = false;

            // get user authorizatin token
            const data = await backend.post('api/user-auth/', {username: this.username, password: this.password});          
            if(data !== null && data.token !== undefined && data.userId !== undefined)  
            { 
                // on success create global shared user
                CamerafyUser.login(this.username, data.userId, data.token, data.groups);     

                this.state = S_LOGGEDIN;
                this.failed = false;

                const _this = this;
                setTimeout(function() { _this.showLoginMask = false; }, 1000);
            }
            else
            {
                this.failed = true;
            }
            
            // clear password
            this.password = "";
        },

        Logout: function()
        {
            CamerafyUser.logout();  

            this.state = S_LOGGEDOUT;

            // pop-up logout mask to indicate user logout
            this.showLoginMask = true;
            const _this = this;
            setTimeout(function() 
            { 
                _this.showLoginMask = CamerafyConfig.UserLogin; 
                // hack: prevent user login mask being displayed in clean state while in close transition
                setTimeout(function() { _this.state = S_CLEAN }, 200);
            }, 1000);
        },

        Cancel: function()
        {
            this.showLoginMask=false; 
            this.failed=false;
            
            EventBus.$emit(EnableRemoteInputTransmitEvent);
        }
    }
}
</script>