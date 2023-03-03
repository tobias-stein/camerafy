from rest_framework.permissions import BasePermission

class IsCamerafySession(BasePermission):
    """
    Checks if request is coming from a CamerafySession group user.
    """
    def has_permission(self, request, view):
        return True if request.user.groups.filter(name='CamerafySession').count() else False


class IsCamerafyEditor(BasePermission):
    """
    Checks if request is coming from a Camerafy Unity editor group user.
    """
    def has_permission(self, request, view):
        return True if request.user.groups.filter(name='CamerafyEditor').count() else False