import time
from datetime import datetime, timezone
from django.test import TestCase

from .models import Session

class SessionTestCase(TestCase):

    def test_create_new_session(self):
        """ Create a new session. """
        new_session = Session.objects.create_session()
        self.assertTrue(new_session.session_id is not None)
        self.assertTrue(new_session.created is not None)
        self.assertTrue(new_session.terminated is None)

    def test_is_terminated(self):
        """ Test if session is terminated. """
        new_session = Session.objects.create_session()
        self.assertFalse(Session.objects.is_session_terminated(new_session.session_id))
        # terminate session
        Session.objects.terminate_session(new_session.session_id)
        self.assertTrue(Session.objects.is_session_terminated(new_session.session_id))

    def test_elapsed_session_time(self):
        """ Test the elapsed session time. """
        new_session = Session.objects.create_session()
        # sleep one second
        time.sleep(1)
        self.assertTrue(Session.objects.get_elapsed_session_time(new_session.session_id) < 2)
        # terminate session
        Session.objects.terminate_session(new_session.session_id)
        # sleep another second
        time.sleep(1)
        self.assertTrue(Session.objects.get_elapsed_session_time(new_session.session_id) < 2)
