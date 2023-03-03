import requests
import json
import time
import os

from django.conf import settings
from django.shortcuts import render
from django.db import transaction
from django.contrib.auth.models import User
from rest_framework import viewsets, mixins, permissions, authentication
from rest_framework.response import Response
from rest_framework.decorators import action, permission_classes
from rest_framework.authtoken.views import ObtainAuthToken
from rest_framework.authtoken.models import Token


class userauth(ObtainAuthToken):
    """
    Endpoint for user authentication with username and password.
    """
    
    # allow user to claim token with basic auth
    authentication_classes = [authentication.BasicAuthentication]
    permission_classes = [permissions.AllowAny]

    def post(self, request, *args, **kwargs):
        serializer = self.serializer_class(data=request.data, context={'request': request})
        serializer.is_valid(raise_exception=True)
        user = serializer.validated_data['user']
        token, created = Token.objects.get_or_create(user=user)
        return Response(
        { 
            'token': token.key, 
            'userId': user.pk,
            'groups': user.groups.values_list('name', flat=True)
        })