import CamerafyLib from '../../services/CamerafyLib'

const PointerPhase = {
  None: 0,
  Began: 1,
  Moved: 2,
  Ended: 3,
  Canceled: 4,
  Stationary: 5
}

// This key map is analogus to the unity input-system defined keymap
const Keymap = {
  "Space": 1,
  "Enter": 2,
  "Tab": 3,
  "Backquote": 4,
  "Quote": 5,
  "Semicolon": 6,
  "Comma": 7,
  "Period": 8,
  "Slash": 9,
  "Backslash": 10,
  "LeftBracket": 11,
  "RightBracket": 12,
  "Minus": 13,
  "Equals": 14,
  "KeyA": 15,
  "KeyB": 16,
  "KeyC": 17,
  "KeyD": 18,
  "KeyE": 19,
  "KeyF": 20,
  "KeyG": 21,
  "KeyH": 22,
  "KeyI": 23,
  "KeyJ": 24,
  "KeyK": 25,
  "KeyL": 26,
  "KeyM": 27,
  "KeyN": 28,
  "KeyO": 29,
  "KeyP": 30,
  "KeyQ": 31,
  "KeyR": 32,
  "KeyS": 33,
  "KeyT": 34,
  "KeyU": 35,
  "KeyV": 36,
  "KeyW": 37,
  "KeyX": 38,
  "KeyY": 39,
  "KeyZ": 40,
  "Digit1": 41,
  "Digit2": 42,
  "Digit3": 43,
  "Digit4": 44,
  "Digit5": 45,
  "Digit6": 46,
  "Digit7": 47,
  "Digit8": 48,
  "Digit9": 49,
  "Digit0": 50,
  "ShiftLeft": 51,
  "ShiftRight": 52,
  "AltLeft": 53,
  "AltRight": 54,
  // "AltGr": 54,
  "ControlLeft": 55,
  "ControlRight": 56,
  "MetaLeft": 57,
  "MetaRight": 58,
  // "LeftWindows": 57,
  // "RightWindows": 58,
  // "LeftApple": 57,
  // "RightApple": 58,
  // "LeftCommand": 57,
  // "RightCommand": 58,
  "ContextMenu": 59,
  "Escape": 60,
  "ArrowLeft": 61,
  "ArrowRight": 62,
  "ArrowUp": 63,
  "ArrowDown": 64,
  "Backspace": 65,
  "PageDown": 66,
  "PageUp": 67,
  "Home": 68,
  "End": 69,
  "Insert": 70,
  "Delete": 71,
  "CapsLock": 72,
  "NumLock": 73,
  "PrintScreen": 74,
  "ScrollLock": 75,
  "Pause": 76,
  "NumpadEnter": 77,
  "NumpadDivide": 78,
  "NumpadMultiply": 79,
  "NumpadPlus": 80,
  "NumpadMinus": 81,
  "NumpadPeriod": 82,
  "NumpadEquals": 83,
  "Numpad0": 84,
  "Numpad1": 85,
  "Numpad2": 86,
  "Numpad3": 87,
  "Numpad4": 88,
  "Numpad5": 89,
  "Numpad6": 90,
  "Numpad7": 91,
  "Numpad8": 92,
  "Numpad9": 93,
  "F1": 94,
  "F2": 95,
  "F3": 96,
  "F4": 97,
  "F5": 98,
  "F6": 99,
  "F7": 100,
  "F8": 101,
  "F9": 102,
  "F10": 103,
  "F11": 104,
  "F12": 105,
  // "OEM1": 106,
  // "OEM2": 107,
  // "OEM3": 108,
  // "OEM4": 109,
  // "OEM5": 110,
  // "IMESelected": 111,
};

class InputHandler
{
  RegisterInputEvents(videoPlayer) 
  {
    const _videoPlayer = videoPlayer;

    // Listen to keypoard events
    document.addEventListener('keyup', sendKeyUp, false);
    document.addEventListener('keydown', sendKeyDown, false);

    // Listen to mouse events
    _videoPlayer.addEventListener('click', sendMouse, false);
    _videoPlayer.addEventListener('mousedown', sendMouse, false);
    _videoPlayer.addEventListener('mouseup', sendMouse, false);
    _videoPlayer.addEventListener('mousemove', sendMouse, false);
    _videoPlayer.addEventListener('wheel', sendMouseWheel, false);

    // ios workaround for not allowing auto-play

    // Listen to touch events based on "Touch Events Level1" TR.
    //
    // Touch event Level1 https://www.w3.org/TR/touch-events/
    // Touch event Level2 https://w3c.github.io/touch-events/
    //
    _videoPlayer.addEventListener('touchend', sendTouchEnd, false);
    _videoPlayer.addEventListener('touchstart', sendTouchStart, false);
    _videoPlayer.addEventListener('touchcancel', sendTouchCancel, false);
    _videoPlayer.addEventListener('touchmove', sendTouchMove, false);

    function sendKeyUp(e) { sendKey(e, false); }
    function sendKeyDown(e) { sendKey(e, true); }

    function sendKey(e, isDown) 
    {
      const key = Keymap[e.code];
      const character = e.key.length === 1 ? e.key.charCodeAt(0) : 0;
      //console.log("key down " + key + ", repeat = " + e.repeat + ", character = " + character);
      CamerafyLib.Input.RemoteInputModule.ProcessRemoteKeyboardInput(isDown, e.repeat, key, character);
    }

    function sendTouch(e, phase) {
      const changedTouches = Array.from(e.changedTouches);
      const touches = Array.from(e.touches);
      
      const touchIds = [];
      const phrases = [];
      const xs = [];
      const ys = [];
      const forces = [];

      for (let i = 0; i < changedTouches.length; i++) {
        if (touches.find(function (t) {
          return t.identifier === changedTouches[i].identifier
        }) === undefined) {
          touches.push(changedTouches[i]);
        }
      }

      for (let i = 0; i < touches.length; i++) {
        touches[i].identifier;
        phrases[i] = changedTouches.find(
          function (e) {
            return e.identifier === touches[i].identifier
          }) === undefined ? PointerPhase.Stationary : phase;
      }

      //console.log("touch phase:" + phase + " length:" + changedTouches.length + " pageX" + changedTouches[0].pageX + ", pageX: " + changedTouches[0].pageY + ", force:" + changedTouches[0].force);

      for (let i = 0; i < touches.length; i++) 
      {
        const scale   = _videoPlayer.scale;
        const originX = _videoPlayer.originX;
        const originY = _videoPlayer.originY;

        const x = (touches[i].pageX - originX) / scale;
        const y = _videoPlayer.height - (touches[i].pageY - originY) / scale;

        touchIds.push(touches[i].identifier);
        xs.push(x);
        ys.push(y);
        forces.push(touches[i].force);
      }

      CamerafyLib.Input.RemoteInputModule.ProcessRemoteTouchInput(touches.length, touchIds, phrases, xs, ys, forces);
    }

    function sendTouchMove(e) { sendTouch(e, PointerPhase.Moved); e.preventDefault(); }
    function sendTouchStart(e) { sendTouch(e, PointerPhase.Began); e.preventDefault(); }
    function sendTouchEnd(e) { sendTouch(e, PointerPhase.Ended); e.preventDefault(); }
    function sendTouchCancel(e) { sendTouch(e, PointerPhase.Canceled); e.preventDefault(); }

    function sendMouse(e) 
    {
      const scale   = _videoPlayer.scale;
      const originX = _videoPlayer.originX;
      const originY = _videoPlayer.originY;

      const x = (e.clientX - originX) / scale;
      // According to Unity Coordinate system
      const y = _videoPlayer.height - (e.clientY - originY) / scale;

      //console.log("x: " + x + ", y: " + y + ", scale: " + scale + ", originX: " + originX + ", originY: " + originY + " mouse button:" + e.buttons);
      CamerafyLib.Input.RemoteInputModule.ProcessRemoteMouseInput(x, y, e.buttons);
    }

    function sendMouseWheel(e) {
      //console.log("mouse wheel with delta " + e.wheelDelta);
      // send remote input
      CamerafyLib.Input.RemoteInputModule.ProcessRemoteMouseWheelInput(e.deltaX, e.deltaY);
    }

    // return unregister function
    return function() 
    {
      // Listen to keypoard events
      document.removeEventListener('keyup', sendKeyUp, false);
      document.removeEventListener('keydown', sendKeyDown, false);

      // Listen to mouse events
      _videoPlayer.removeEventListener('click', sendMouse, false);
      _videoPlayer.removeEventListener('mousedown', sendMouse, false);
      _videoPlayer.removeEventListener('mouseup', sendMouse, false);
      _videoPlayer.removeEventListener('mousemove', sendMouse, false);
      _videoPlayer.removeEventListener('wheel', sendMouseWheel, false);

      // ios workaround for not allowing auto-play

      // Listen to touch events based on "Touch Events Level1" TR.
      //
      // Touch event Level1 https://www.w3.org/TR/touch-events/
      // Touch event Level2 https://w3c.github.io/touch-events/
      //
      _videoPlayer.removeEventListener('touchend', sendTouchEnd, false);
      _videoPlayer.removeEventListener('touchstart', sendTouchStart, false);
      _videoPlayer.removeEventListener('touchcancel', sendTouchCancel, false);
      _videoPlayer.removeEventListener('touchmove', sendTouchMove, false);
    }
  }
}

export default new InputHandler();