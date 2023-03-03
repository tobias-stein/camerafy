from django.db import models
from django.contrib.auth.models import User

from .common import *

def snapshots_storage(instance, filename):
    # file will be uploaded to MEDIA_ROOT/snapshots/<user_id>/<filename>
    return f"snapshots/{instance.author.id}/{filename}"

def snapshots_thumbnail_storage(instance, filename):
    # file will be uploaded to MEDIA_ROOT/snapshots/<user_id>/thumbnails/<filename>
    return f"snapshots/{instance.author.id}/thumbnails/{filename}"

class CamerafySnapshot(models.Model):
    # user who took the snapshot
    author = models.ForeignKey(User, on_delete=models.CASCADE)
    # timestamp when snapshot was taken
    created = models.DateField(auto_now_add=True)
    # snapshot titel
    title = models.CharField(unique=True, max_length=256)
    # snapshot description text
    description = models.TextField(max_length=1024)
    # image thumbnail
    thumbnail = models.ForeignKey('CamerafySnapshotThumbnail', on_delete=models.CASCADE)
    # image format
    format = models.CharField(choices=IMAGE_FORMAT_CHOICES, max_length=3)
    # image width
    width = models.PositiveSmallIntegerField()
    # image height
    height = models.PositiveSmallIntegerField()
    # image size in bytes
    size = models.PositiveIntegerField()
    # image data
    image = models.ImageField(upload_to=snapshots_storage)
    # 'seen' flag
    seen = models.BooleanField()
    
    class Meta:
        db_table = 'camfy_snapshot'


class CamerafySnapshotThumbnail(models.Model):
    # user who took the snapshot
    author = models.ForeignKey(User, on_delete=models.CASCADE)
    # image format
    format = models.CharField(choices=IMAGE_FORMAT_CHOICES, max_length=3)
    # image width
    width = models.PositiveSmallIntegerField()
    # image height
    height = models.PositiveSmallIntegerField()
    # image size in bytes
    size = models.PositiveIntegerField()
    # image data
    image = models.ImageField(upload_to=snapshots_thumbnail_storage)

    class Meta:
        db_table = 'camfy_snapshot_thumbnail'