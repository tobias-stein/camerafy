# Generated by Django 3.0.2 on 2020-02-15 21:40

from django.db import migrations, models


class Migration(migrations.Migration):

    dependencies = [
        ('api', '0004_add_env_and_model'),
    ]

    operations = [
        migrations.AlterField(
            model_name='environment',
            name='brief',
            field=models.CharField(max_length=512),
        ),
    ]
