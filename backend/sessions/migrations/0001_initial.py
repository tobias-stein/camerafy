# Generated by Django 3.1.1 on 2020-10-10 14:35

from django.db import migrations, models
import uuid


class Migration(migrations.Migration):

    initial = True

    dependencies = [
    ]

    operations = [
        migrations.CreateModel(
            name='Session',
            fields=[
                ('session_id', models.UUIDField(default=uuid.uuid4, editable=False, help_text='Unique session identifier.', primary_key=True, serialize=False)),
                ('created', models.DateTimeField(auto_now_add=True, help_text='Timestamp when session has been created.')),
                ('terminated', models.DateTimeField(help_text='Timestamp when session has been terminated. This field may be null, if session is still active.', null=True)),
            ],
            options={
                'db_table': 'camfy_sessions',
            },
        ),
    ]
