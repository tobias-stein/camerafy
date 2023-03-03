import uuid
from datetime import datetime, timezone
from django.db import models

class SessionManager(models.Manager):
    
    def create_session(self):
        return self.create()

    def terminate_session(self, session_id):
        try:
            session = self.get(session_id=session_id)
            session.terminated = datetime.now(timezone.utc)
            session.save()
            return session
        except:
            return None

    def get_elapsed_session_time(self, session_id):
        try:
            session = self.get(session_id=session_id)
            if session.terminated is not None:
                return (session.terminated - session.created).total_seconds()
            else:
                return (datetime.now(timezone.utc) - session.created).total_seconds()
        except:
            # session does not exists, and is therefore kinda terminated
            return 0

    def is_session_terminated(self, session_id):
        try:
            session = self.get(session_id=session_id)
            return session.terminated is not None
        except:
            # session does not exists, and is therefore kinda terminated
            return True


class Session(models.Model):

    session_id      = models.UUIDField(
                        primary_key=True,
                        default=uuid.uuid4,
                        editable=False,
                        help_text='Unique session identifier.')

    created         = models.DateTimeField(
                        auto_now=False, 
                        auto_now_add=True,
                        help_text='Timestamp when session has been created.')

    terminated      = models.DateTimeField(
                        null=True,
                        auto_now=False, 
                        auto_now_add=False,
                        help_text='Timestamp when session has been terminated. This field may be null, if session is still active.')

    # use custom model manager
    objects         = SessionManager()

    class Meta:
        db_table = 'camfy_sessions'
