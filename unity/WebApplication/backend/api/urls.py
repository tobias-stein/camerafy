from django.contrib import admin
from django.urls import path, include
from django.conf.urls import url
from rest_framework.routers import DefaultRouter

from .views.auth import userauth
from .views.snapshots import snapshots
from .views.environments import environments
from .views.camfymodels import camfymodels
from .views.touchpoints import touchpoints

# Create a router and register our viewsets with it.
router = DefaultRouter()
router.register('snapshots', snapshots)
router.register('environments', environments)
router.register('models', camfymodels)
router.register('touchpoints', touchpoints)

urlpatterns = router.urls
urlpatterns += [
    url(r'^user-auth/', userauth.as_view())
]