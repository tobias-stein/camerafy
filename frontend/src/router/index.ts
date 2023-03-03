import Vue from "vue";
import VueRouter, { RouteConfig } from "vue-router";
import VueCookies from 'vue-cookies'

import api from "@/api/api";

/** VIEWS */
import Home from "../views/Home.vue";
import NoPermission from "../views/NoPermission.vue";
import Error from "../views/Error.vue";
import CamerafyStream from "../views/CamerafyStream.vue";
import CamerafySessions from "../views/CamerafySessions.vue";


Vue.use(VueCookies)
Vue.use(VueRouter);

const routes: Array<RouteConfig> = [
  {
    path: "/",
    name: "Home",
    component: Home,
    meta: { requiresAuth: true }
  },
  {
    path: "/noPermission",
    name: "NoPermission",
    component: NoPermission,
    meta: { requiresAuth: false }
  }
  ,
  {
    path: "/error",
    name: "Error",
    component: Error,
    meta: { requiresAuth: false }
  },
  {
    path: "/sessions",
    name: "CamerafySessions",
    component: CamerafySessions,
    meta: { requiresAuth: true }
  },
  {
    path: "/stream",
    name: "CamerafyStream",
    component: CamerafyStream,
    meta: { requiresAuth: true },

    beforeEnter: (to, from, next) => {

      const requiredCookies = [
        'camfy_broker_url',
        'camfy_broker_usr',
        'camfy_broker_pwd',
        'camfy_session_id',
      ];

      // make sure we have all required cookies, otherwise we do not need to go to /stream view
      requiredCookies.forEach(c => { if(window.$cookies.get(c) === null) { next({ name: 'Error'}); return; } });

      // all cookies available
      next();
    }
  }
];

// create router 
const router = new VueRouter({ 
  mode: 'history',
  routes: routes
 });

// enforce only authenticated users can use the app
router.beforeEach(async (to, from, next) => 
{
  // Make sure user is authenticated
  if (to.meta?.requiresAuth && !(await api.auth.IsAuthenticated())) { await api.auth.Authenticate(to.fullPath); }
  // Only authenticated users.
  else next();
})

export default router;
