from django.http import HttpResponse
from rest_framework import viewsets, status
from rest_framework.decorators import action
from rest_framework.response import Response

from .services import launch_local_session, launch_remote_session
from .models import Session
from .serializers import SessionSerializer


class SessionView(viewsets.ViewSet):

    def list(self, request):
        return Response(SessionSerializer(Session.objects.all(), many=True).data)

    def create(self, request):
        # create a new session
        new_session = Session.objects.create_session()
        # launch new environment
        if request.META['REMOTE_ADDR'] in ['127.0.0.1', 'localhost']:
            launch_local_session(new_session)
        else:
            launch_remote_session(new_session)

        # return serialized new session data
        return Response(SessionSerializer(new_session).data)

    def retrieve(self, request, pk=None):
        try:
            session = Session.objects.get(session_id=pk)
            return Response(SessionSerializer(session).data)
        except:
            return Response({}, status=status.HTTP_404_NOT_FOUND)

    def destroy(self, request, pk=None):
        session = Session.objects.terminate_session(pk)
        if session is not None:
            return Response(SessionSerializer(session).data)
        else:
            return Response({}, status=status.HTTP_400_BAD_REQUEST)

    @action(detail=True, methods=['GET'])
    def join(self, request, pk=None):

        # check if session is terminated, is so stop right here
        if Session.objects.is_session_terminated(pk):
            return Response({}, status=status.HTTP_404_NOT_FOUND)
        # else try find session
        try:
            session = Session.objects.get(session_id=pk)
            
            response = HttpResponse()
            response.set_cookie('camfy_broker_url', 'ws://localhost:15674/ws')
            response.set_cookie('camfy_broker_usr', 'guest')
            response.set_cookie('camfy_broker_pwd', 'guest')
            response.set_cookie('camfy_session_id', session.session_id)

            return response
        # session does not exists
        except:
            return Response({}, status=status.HTTP_404_NOT_FOUND)