from django.shortcuts import render
from django.db import transaction
from rest_framework import viewsets, mixins, permissions, authentication
from rest_framework.response import Response
from rest_framework.decorators import action, permission_classes

from . import models
from . import serializers

from . import permissions as camerafy_perm


class translationkeys(viewsets.ModelViewSet):
    """
    Translation keys endpoint.
    """
    queryset = models.TranslationKey.objects.all()
    serializer_class = serializers.TranslationKeySerializer
    permission_classes=[camerafy_perm.IsCamerafyLocalizer]

    @action(methods=['GET'], detail=False)
    def progress(self, request, *args, **kwargs):
        """
        Returns the overall translation progress per translation key. If translations
        are missing a list of language codes is returned.
        """
        response_data = {}
        Languages = models.Language.objects.all()
        TKeys = models.TranslationKey.objects.all()
        num_languages = Languages.count()

        for tk in TKeys:
            translated_languages = models.Translation.objects.filter(translation_key=tk.id).values_list('language_id', flat=True)
            response_data[tk.key] = {
                'id': tk.id,
                'progress': float(len(translated_languages) / num_languages)  if num_languages else 0.0,
                'missing': Languages.exclude(id__in=translated_languages).values_list('lan_code', flat=True)
            }

        return Response(response_data, 200)

class languages(viewsets.ModelViewSet):
    """
    Languages endpoint.
    """
    queryset = models.Language.objects.all()
    serializer_class = serializers.LanguageSerializer

    def get_permissions(self):
        """
        Allow anonymous access list method, everything else is only accessible with localizer permission.
        """
        
        if self.action == 'list':
            permission_classes = [permissions.AllowAny]
        else:
            permission_classes = [camerafy_perm.IsCamerafyLocalizer]
        
        return [permission() for permission in permission_classes]

    @action(methods=['GET'], detail=False)
    def progress(self, request, *args, **kwargs):
        """
        Returns the overall translation progress per language. If language is missing a translation a list
        of translation keys is returned.
        """
        response_data = {}
        Languages = models.Language.objects.all()
        TKeys = models.TranslationKey.objects.all()
        num_translation_keys = TKeys.count()

        for lang in Languages:
            translated_keys = models.Translation.objects.filter(language_id=lang.id).values_list('translation_key', flat=True)
            response_data[lang.lan_code] = {
                'id': lang.id,
                'progress': float(len(translated_keys) / num_translation_keys) if num_translation_keys else 0.0,
                'missing': TKeys.exclude(id__in=translated_keys).values_list('key', flat=True)
            }

        return Response(response_data, 200)

class translations(viewsets.ModelViewSet):
    """
    Translations endpoint.
    """
    queryset = models.Translation.objects.all()
    serializer_class = serializers.TranslationSerializer

    def get_permissions(self):
        """
        Allow anonymous access list method, everything else is only accessible with localizer permission.
        """
        
        if self.action == 'list':
            permission_classes = [permissions.AllowAny]
        else:
            permission_classes = [camerafy_perm.IsCamerafyLocalizer]
        
        return [permission() for permission in permission_classes]

    def list(self, request, *args, **kwargs):

        def insert_translation_key(key_str, translation_obj, _dict):
            # split key into its elements
            key_elems = key_str.split('.')
            num_elems = len(key_elems)

            it = _dict
            for key in key_elems:
                # create key path if it does not exist
                if key not in it:
                    it[key] = {}
                it = it[key]

            if translation_obj is not None:
                it['id'] = translation_obj.id
                it['revision'] = translation_obj.revision
                it['value'] = translation_obj.translation
            else:
                it['id'] = -1
                it['revision'] = 0
                it['value'] = ""
            
        response_data = {}

        # get language
        languages = request.query_params['language'] if 'language' in request.query_params else request.data['language'] if 'language' in request.data else None
        supported_languages = models.Language.objects.values_list('lan_code', flat=True)
        if languages is None:
            return Response({'error': "'language' identifier required. Please provide 'language' as url query param '?language=<language>[,<language>]' or as form data '--form language=<language>[,<language>]', where <language> is ISO 639 language tag (e.g. 'de-DE'). Multiple language can be queries by separating them with comma.", 'supported': supported_languages}, 404)

        # should return result as flat structure
        flat = bool(request.query_params['flat']) if 'flat' in request.query_params else bool(request.data['flat']) if 'flat' in request.data else False
        

        # make unique if multiple languages specified
        for language in list(set(languages.split(','))):

            if language not in supported_languages:
                response_data[language] = "Not supoprted."
                continue

            # read all translation keys
            TKeys = models.TranslationKey.objects.values_list('id', 'key')
            # read all translations for keys in given language
            Trans = models.Translation.objects.filter(translation_key__in=[tk[0] for tk in list(TKeys)], language=models.Language.objects.filter(lan_code=language).first().id)
            
            # build translation json
            if flat:
                response_data[language] = {}
                for tkey in TKeys:
                    trans = Trans.filter(translation_key=tkey[0])
                    if trans.count():
                        trans = trans.first()
                        response_data[language][tkey[1]] = {'id': trans.id, 'revision': trans.revision, 'translation': trans.translation}
                    else:
                        response_data[language][tkey[1]] = {'id': -1, 'revision': 0, 'translation': ""}

            else:
                [insert_translation_key(f"{language}.{tkey[1]}", Trans.filter(translation_key=tkey[0]).first() if Trans.filter(translation_key=tkey[0]).count() else None, response_data) for tkey in TKeys]

        return Response(response_data, status=200)
        