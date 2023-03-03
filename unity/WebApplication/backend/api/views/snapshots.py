import requests
import json
import time
import base64
import os

from django.conf import settings
from django.shortcuts import render
from django.db import transaction
from django.contrib.auth.models import User
from django.core.files import File
from rest_framework import viewsets, mixins, permissions, authentication
from rest_framework.response import Response
from rest_framework.decorators import action, permission_classes

from PIL import Image  
from io import BytesIO

from .common import verify_request_data

from ..models import snapshots as snapshotModels
from ..serializers import snapshots as snapshotSerializers
from ..permissions import IsCamerafySession

class snapshots(
    mixins.RetrieveModelMixin,
    mixins.DestroyModelMixin,
    viewsets.GenericViewSet):
    """
    Snaphot endpoint.
    """

    queryset = snapshotModels.CamerafySnapshot.objects.all()
    serializer_class = snapshotSerializers.CamerafySnapshotSerializer

    def list(self, request, *args, **kwargs):
        queryset = snapshotModels.CamerafySnapshot.objects.filter(author=request.user) if request.user.is_authenticated else []
        serializer = self.get_serializer(queryset, many=True)
        return Response(serializer.data)

    def perform_destroy(self, instance):
        """
        Delete media files before removing instance.
        """
        # delete thumbnail
        os.remove(snapshotModels.CamerafySnapshotThumbnail.objects.get(id=instance.thumbnail.id).image.path)
        # deleta snapshot
        os.remove(instance.image.path)
        
        # remove entity from database
        instance.delete()

    @action(methods=['get'], detail=True)
    def download(self, request, *args, **kwargs):
        """
        Downloads an image as base64 encoded string.
        """
        snapshot = self.get_object()

        base64_image = ""
        with open(snapshot.image.path, 'rb') as f:
            image = File(f)
            base64_image = base64.b64encode(image.read())

        # mark this snapshot as 'seen'
        snapshot.seen = True
        snapshot.save()

        return Response({'result': 
        {
            'title': snapshot.title,
            'width': snapshot.width,
            'hegith': snapshot.height,
            'format': snapshot.format,
            'base64_image': base64_image,
        }})

    @action(methods=['post'], detail=False, permission_classes=[IsCamerafySession])
    def upload(self, request):
        """
        Allows the Camerafy application to upload a snapshot for a specific user. This method will
        automatically derive a thumbnail from the uploaded snapshot.
        """

        missing_data = verify_request_data(request.data, [
            'userid',
            'file', 
            'width', 
            'height',
            'format'
        ])

        if len(missing_data):
            return Response({'error': f"Missing required data field '{', '.join(missing_data)}'."}, status=400)
        
        try:
            user_id = request.data['userid']
            snapshot = request.data['file']
            title = f"snapshot-{int(time.time())}"
            format = request.data['format']
            width = request.data['width']
            height = request.data['height']

            # retrieve snapshow author
            user = User.objects.get(id=user_id)
            
            # create downscaled thumbnail version of image
            thumbnail_img = Image.open(snapshot).resize((160, 90))
            in_memory_file = BytesIO()
            thumbnail_img.save(in_memory_file, format)
            in_memory_file.seek(0)
            thumbnail_size = in_memory_file.getbuffer().nbytes
            thumbnail_img = File(in_memory_file, f"{title}.{format}")

            with transaction.atomic():
                # create the thumbnail image
                thumbnail = models.CamerafySnapshotThumbnail.objects.create(
                    author=user,
                    format=format,
                    width=160,
                    height=90,
                    size=thumbnail_size,
                    image=thumbnail_img
                )

                # rename save file for uploaded snapshot
                snapshot.name = f"{title}.{format}"

                snapshotObj = snapshotModels.CamerafySnapshot.objects.create(
                    author=user,
                    title=title,
                    description=request.data['description'] if 'description' in request.data else '',
                    image=snapshot,
                    thumbnail=thumbnail,
                    format=format,
                    width=width,
                    height=height,
                    size=len(snapshot),
                    seen=False
                )
        except User.DoesNotExist:
            return Response({'error': f"User id '{user_id}' is unknown."}, status=400)
        except Exception as e:
            return Response({'error': str(e)}, status=400)

        return Response({'result': {'id': snapshotObj.id}})
