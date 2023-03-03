from rest_framework.permissions import BasePermission

class IsCamerafyLocalizer(BasePermission):
    """
    Checks if request is coming from an authorized localization user.
    """
    def has_permission(self, request, view):
        return True if request.user.groups.filter(name='CamerafyLocalizationManager').count() else False