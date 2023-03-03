from PIL import Image  
from io import BytesIO

from rest_framework import viewsets, mixins, permissions, authentication
from rest_framework.response import Response
from rest_framework.decorators import action, permission_classes

from .common import verify_request_data
from ..models import camfymodels as modelModels
from ..serializers import camfymodels as modelSerializers
from ..permissions import IsCamerafyEditor, IsCamerafySession


class camfymodels(
    mixins.ListModelMixin,
    mixins.RetrieveModelMixin,
    mixins.DestroyModelMixin,
    viewsets.GenericViewSet):

    # default permission will be CamerafyEditor since this class will mostly managed from the editor
    permission_classes = [IsCamerafyEditor | IsCamerafySession]

    queryset = modelModels.CamfyModel.objects.all()
    serializer_class = modelSerializers.CamfyModelSerializer

    def create(self, request, *args, **kwargs):

        missing_data = verify_request_data(request.data, ['name'])

        # validate input data
        if len(missing_data):
            return Response({'error': f"Missing required data field '{', '.join(missing_data)}'."}, status=400)

        # make sure new model does not already exist
        if modelModels.CamfyModel.objects.filter(name=request.data['name']).count():
            return Response({'error': f"Model with name '{request.data['name']}' already exists."}, status=400)

        # create new model
        instance = modelModels.CamfyModel.objects.create(name=request.data['name'], brief=request.data['brief'] if 'brief' in request.data else "")

        # return resource id
        return Response({'result': {'id': instance.id } })


    def update(self, request, *args, **kwargs):
        instace = self.get_object()

        if 'desc_json' in request.data:
            instace.desc_json = request.data['desc_json']

        # save cahnges
        instace.save()

        return Response({'result': 'Server data updated.' })