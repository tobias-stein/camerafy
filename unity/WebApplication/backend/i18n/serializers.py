from rest_framework import serializers
from . import models

class TranslationKeySerializer(serializers.ModelSerializer):
    class Meta:
        model = models.TranslationKey
        fields = '__all__'

class LanguageSerializer(serializers.ModelSerializer):
    class Meta:
        model = models.Language
        fields = '__all__'

class TranslationSerializer(serializers.ModelSerializer):
    class Meta:
        model = models.Translation
        fields = '__all__'

    