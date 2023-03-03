from django.db import models
from django.core.validators import RegexValidator


class TranslationKey(models.Model):
    STRING = 'S'
    BOOLEAN = 'B'
    TYPE_CHOICES = (
        (STRING, 'string'),
        (BOOLEAN, 'bool')
    )

    # the uniuqe translation key
    key = models.CharField(
        unique=True,
        max_length=256,
        validators=[
            RegexValidator(regex='^[a-zA-Z0-9._]*(?<![_.])$', message='Translation key may only contain alpha-numeric letters, underscore or periode sings.')
        ])
    # localization key type
    type = models.CharField(max_length=1, choices=TYPE_CHOICES, default=STRING)

    class Meta:
        db_table = 'i18n_translation_keys'

class Language(models.Model):
    # uniuqe language code, e.g. de-DE
    lan_code = models.CharField(unique=True, max_length=8)
    # language name, e.g. german
    lan_name = models.CharField(max_length=128)
    # region code, e.g. DE
    reg_code = models.CharField(max_length=4)
    # region name, e.g. Germany
    reg_name = models.CharField(max_length=128)

    class Meta:
        db_table = 'i18n_languages'

class Translation(models.Model):
    # the translation key this translation belongs to
    translation_key = models.ForeignKey(TranslationKey, on_delete=models.CASCADE)
    # the language this translation is for
    language = models.ForeignKey(Language, on_delete=models.CASCADE)
    # the actual translation string
    translation = models.TextField()
    # revision number
    revision = models.BigIntegerField()

    class Meta:
        db_table = 'i18n_translations'