@echo off

:: install python requirements
pip install --user -r requirements.txt

:: install nodejs requirements
pushd frontend & npm install & popd

:: apply initial migrations
python manage.py migrate

:: install default langauges
python manage.py loaddata backend/i18n/default_languages.json
:: install default camerafy permission groups
python manage.py loaddata backend/default_camerafy_groups.json