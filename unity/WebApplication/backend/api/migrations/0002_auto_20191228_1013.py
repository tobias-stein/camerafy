# Generated by Django 2.2.6 on 2019-12-28 09:13

import backend.api.models
from django.db import migrations, models


class Migration(migrations.Migration):

    dependencies = [
        ('api', '0001_create_snapshots_tables'),
    ]

    operations = [
        migrations.AlterField(
            model_name='camerafysnapshotthumbnail',
            name='image',
            field=models.ImageField(upload_to=backend.api.models.snapshots.snapshots_thumbnail_storage),
        ),
    ]
