import base64
from django.core.files import File
from rest_framework import serializers

from ..models import snapshots

class CamerafySnapshotThumbnailSerializer(serializers.ModelSerializer):

    base64_image = serializers.SerializerMethodField()

    def get_base64_image(self, obj):
        """
        Transform raw image file to base64 encoded string.
        """
        with open(obj.image.path, 'rb') as f:
            image = File(f)
            return base64.b64encode(image.read())

    class Meta:
        model = snapshots.CamerafySnapshotThumbnail
        fields = [
            'format', 
            'width',
            'height',
            'base64_image'
        ]

class CamerafySnapshotSerializer(serializers.ModelSerializer):
    
    thumbnail = CamerafySnapshotThumbnailSerializer(many=False)

    class Meta:
        model = snapshots.CamerafySnapshot
        fields = [
            'id',
            'title', 
            'description',
            'thumbnail',
            'created', 
            'format', 
            'width',
            'height',
            'size',
            'seen'
        ]