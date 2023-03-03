import os
import platform

from .models import Session

def launch_local_session(session):
    # utility method to convert an input tuple into 'camfy.' prefixed environment variable
    def set_camfy_ev(name, value):
        sys = platform.system()
        if sys == 'Windows':
            os.system(f"setx camfy.{name} {value}")
        # if not windows, we assume a unix system
        else:
            os.system(f"export camfy.{name}={value}")

    # set necessary local environment variables for camerafy session
    set_camfy_ev('SessionId', session.session_id)

def launch_remote_session(session):
    pass
