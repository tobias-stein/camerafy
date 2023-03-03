from rest_framework import serializers

from ..models import environments

class EnvironmentThumbnailSerializer(serializers.ModelSerializer):

    base64_image = serializers.SerializerMethodField()

    def get_base64_image(self, obj):
        """
        Transform raw image file to base64 encoded string.
        """
        with open(obj.image.path, 'rb') as f:
            image = File(f)
            return base64.b64encode(image.read())

    class Meta:
        model = environments.EnvironmentThumbnail
        fields = [
            'format', 
            'width',
            'height',
            'base64_image'
        ]

class EnvironmentSerializer(serializers.ModelSerializer):
    
    thumbnail = EnvironmentThumbnailSerializer(many=False)
    desc_json = serializers.JSONField()
    
    class Meta:
        model = environments.Environment
        fields = [
            'id',
            'name', 
            'brief',
            'thumbnail',
            'created', 
            'updated', 
            'desc_json'
        ]