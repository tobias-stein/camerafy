from rest_framework import serializers

from ..models import touchpoints

class TouchpointSerializer(serializers.ModelSerializer):
    
    desc_json = serializers.JSONField()
    
    class Meta:
        model = touchpoints.Touchpoint
        fields = [
            'id',
            'name', 
            'brief',
            'created', 
            'updated', 
            'desc_json'
        ]