from django.contrib import admin
from django.urls import path, include
from django.conf.urls import url
from rest_framework.routers import DefaultRouter

from . import views

# Create a router and register our viewsets with it.
router = DefaultRouter()
router.register('translations', views.translations)
router.register('translationkeys', views.translationkeys)
router.register('languages', views.languages)

urlpatterns = router.urls