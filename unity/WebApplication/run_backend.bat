@echo off

set PORT=%1
set -DCAMERAFY_API=%2
set -DCAMERAFY_ALLOW_ANONYMOUS=%3

python manage.py runserver %PORT%