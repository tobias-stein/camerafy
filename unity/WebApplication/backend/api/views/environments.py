from PIL import Image  
from io import BytesIO

from rest_framework import viewsets, mixins, permissions, authentication
from rest_framework.response import Response
from rest_framework.decorators import action, permission_classes

from .common import verify_request_data
from ..models import environments as environmentModels
from ..serializers import environments as environmentSerializers
from ..permissions import IsCamerafyEditor, IsCamerafySession


class environments(
    mixins.ListModelMixin,
    mixins.RetrieveModelMixin,
    mixins.DestroyModelMixin,
    viewsets.GenericViewSet):

    # default permission will be CamerafyEditor since this class will mostly managed from the editor
    permission_classes = [IsCamerafyEditor | IsCamerafySession]

    queryset = environmentModels.Environment.objects.all()
    serializer_class = environmentSerializers.EnvironmentSerializer

    def create(self, request, *args, **kwargs):

        missing_data = verify_request_data(request.data, ['name'])

        # validate input data
        if len(missing_data):
            return Response({'error': f"Missing required data field '{', '.join(missing_data)}'."}, status=400)

        # make sure new environment does not already exist
        if environmentModels.Environment.objects.filter(name=request.data['name']).count():
            return Response({'error': f"Environment with name '{request.data['name']}' already exists."}, status=400)

        # create new environment
        instance = environmentModels.Environment.objects.create(name=request.data['name'], brief=request.data['brief'] if 'brief' in request.data else "")

        # return resource id
        return Response({'result': {'id': instance.id } })


    def update(self, request, *args, **kwargs):
        instace = self.get_object()

        if 'desc_json' in request.data:
            instace.desc_json = request.data['desc_json']

        # save cahnges
        instace.save()

        return Response({'result': 'Server data updated.' })