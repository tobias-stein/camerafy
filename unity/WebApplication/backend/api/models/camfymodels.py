from django.db import models
from .common import *

def thumbnail_storage(instance, filename):
    # file will be uploaded to MEDIA_ROOT/models/thumbnails/<filename>
    return f"snapshots/models/thumbnails/{filename}"


class CamfyModelThumbnail(models.Model):
    # image format
    format = models.CharField(choices=IMAGE_FORMAT_CHOICES, max_length=3)
    # image width
    width = models.PositiveSmallIntegerField()
    # image height
    height = models.PositiveSmallIntegerField()
    # image size in bytes
    size = models.PositiveIntegerField()
    # image data
    image = models.ImageField(upload_to=thumbnail_storage)

    class Meta:
        db_table = 'camfy_model_thumbs'


class CamfyModel(models.Model):
    # model name
    name = models.CharField(unique=True, max_length=256)
    # model brief description
    brief = models.CharField(max_length=512)
    # timestamp when created
    created = models.DateField(auto_now_add=True)
    # timestamp when last changed
    updated = models.DateField(auto_now=True)
    # model thumbnail
    thumbnail = models.ForeignKey('CamfyModelThumbnail', blank=True, null=True, on_delete=models.CASCADE)
    # model details as json
    desc_json = models.TextField()

    class Meta:
        db_table = 'camfy_models'

