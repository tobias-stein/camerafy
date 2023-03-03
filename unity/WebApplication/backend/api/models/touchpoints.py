from django.db import models

from .common import *


class Touchpoint(models.Model):
    # touchpoint name
    name = models.CharField(unique=True, max_length=256)
    # touchpoint brief description
    brief = models.CharField(max_length=512)
    # timestamp when created
    created = models.DateField(auto_now_add=True)
    # timestamp when last changed
    updated = models.DateField(auto_now=True)
    # touchpoint details as json
    desc_json = models.TextField()

    class Meta:
        db_table = 'camfy_touchpoints'

