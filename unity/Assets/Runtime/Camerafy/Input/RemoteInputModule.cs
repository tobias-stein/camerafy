using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Camerafy.Input
{
    using Camerafy.Application.Mode;
    using Camerafy.Event;
    using Camerafy.User;
    using System.Collections;
    using TouchPhase = UnityEngine.InputSystem.TouchPhase;

    enum KeyboardEventType
    {
        KeyUp       = 0,
        KeyDown     = 1,
    }
    enum EventType
    {
        None               = 0,
        KeyboardAndMouse   = 1,
        Touch              = 2
    }

    /// <summary>
    /// Enforce script execution order to be highest priority to ensure to process all remote inputs before any other script execution.
    /// </summary>
    [DefaultExecutionOrder(1)]
    public class RemoteInputModule : MonoBehaviour
    {
        public bool IsInitialized { get; private set; } = false;

        public User                         User = null;
        public PlayerInput                  PlayerInput = null;

        public Keyboard                     Keyboard            { get; private set; } = null;
        public Mouse                        Mouse               { get; private set; } = null;
        public Touchscreen                  Touch               { get; private set; } = null;

        /// <summary>
        /// Lasted tracked mouse state.
        /// </summary>
        public MouseState                   LastMousePosition   { get; private set; } = default;

        /// <summary>
        /// Last tracked touch states
        /// </summary>
        public Dictionary<int, TouchState>  LastTouchState      { get; private set; } = new Dictionary<int, TouchState>();

        /// <summary>
        /// Input queue. This queue is necessary since input is send async from main-thread, hence we need
        /// a way to make sure it is only enqueued to the InputSystem while in main-thread.
        /// </summary>
        private Queue<Action>               InputQueue = new Queue<Action>();
       
        /// <summary>
        /// Last received event type.
        /// </summary>
        private EventType                   LastReceivedEventType = EventType.None;

        private void Start()
        {
            this.PlayerInput.neverAutoSwitchControlSchemes = true;
            this.PlayerInput.user.UnpairDevices();

            // create input devices for user
            this.Keyboard = InputSystem.AddDevice<Keyboard>();
            this.Mouse = InputSystem.AddDevice<Mouse>();
            this.Touch = InputSystem.AddDevice<Touchscreen>();

            this.IsInitialized = true;
            this.InputQueue.Clear();

            // register for event: user mode activated
            this.User.ApplicationMode.OnUserApplicationModeActivated += this.OnUserApplicationModeActivated;
        }

        private void OnDestroy()
        {
            // unregister events
            this.User.ApplicationMode.OnUserApplicationModeActivated -= this.OnUserApplicationModeActivated;

            InputSystem.RemoveDevice(Mouse);
            InputSystem.RemoveDevice(Keyboard);
            InputSystem.RemoveDevice(Touch);

            this.Mouse = null;
            this.Keyboard = null;
            this.Touch = null;
            
            this.IsInitialized = false;
            this.InputQueue.Clear();
        }

        [CamerafyEvent]
        void ProcessRemoteKeyboardInput(bool IsKeyDown, bool IsRepeat, byte KeyCode, char Char)
        {
            this.InputQueue.Enqueue(new Action(delegate { ProcessKeyEventDefered(IsKeyDown, IsRepeat, KeyCode, Char); }));
        }

        [CamerafyEvent]
        void ProcessRemoteMouseInput(short Xpos, short Ypos, byte Button)
        {
            this.InputQueue.Enqueue(new Action(delegate { ProcessMouseMoveEventDefered(Xpos, Ypos, Button); }));
        }

        [CamerafyEvent]
        void ProcessRemoteMouseWheelInput(float ScrollX, float ScrollY)
        {
            this.InputQueue.Enqueue(new Action(delegate { ProcessMouseWheelEventDefered(ScrollX, ScrollY); }));
        }

        [CamerafyEvent]
        void ProcessRemoteTouchInput(int NumTouches, int[] TouchId, byte[] Phase, short[] XPos, short[] YPos, float[] Force)
        {
            var touches = new TouchState[NumTouches];
            for (int i = 0; i < NumTouches; i++)
            {
                const int INPUTSYSTEM_ZERO_ID_GUARD = 128; //ID 0 is reserved by inputsystem
                int ID = TouchId[i] + INPUTSYSTEM_ZERO_ID_GUARD;
                var position = new Vector2Int(XPos[i], YPos[i]);
                var delta = new Vector2Int(0, 0);

                TouchState lastState;
                if (!this.LastTouchState.TryGetValue(ID, out lastState))
                {
                    // there was no previous state
                    lastState = new TouchState
                    {
                        startPosition = position,
                        startTime = 0.0,
                        position = position
                    };
                }

                var state = new TouchState
                {
                    startTime = lastState.startTime,
                    startPosition = lastState.startPosition,
                    touchId = ID,
                    phase = (TouchPhase)Phase[i],
                    position = position,
                    delta = position - lastState.position,
                    pressure = Force[i]
                };

                // set current state
                touches[i] = state;

                // remember last touch state
                this.LastTouchState[ID] = state;
            }

            // buffer input action
            this.InputQueue.Enqueue(new Action(delegate { ProcessTouchMoveEventDefered(touches); }));
            if (Touch.touches.Count > NumTouches)
            {
                // buffer input action
                this.InputQueue.Enqueue(new Action(delegate { ChangeEndStateUnusedTouches(touches); }));
            }
        }

        private void Update()
        {
            // execute all buffered inputs since last frame
            while (this.InputQueue.Count > 0) { this.InputQueue.Dequeue()(); }
        }

        public void Reset()
        {
            this.InputQueue.Clear();
            InputSystem.QueueStateEvent(Mouse, new MouseState());
            InputSystem.QueueStateEvent(Keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(Touch, new TouchState());
            InputSystem.Update();
        }

        void ProcessKeyEventDefered(bool keyDown, bool repeat, byte keyCode, char character)
        {
            // switch input layout if necessay
            this.SwitchCurrentControlScheme(EventType.KeyboardAndMouse);

            if (keyDown)
            {
                if (!repeat)
                {
                    InputSystem.QueueStateEvent(Keyboard, new KeyboardState((Key)keyCode));
                }
                if (character != 0)
                {
                    InputSystem.QueueTextEvent(Keyboard, character);
                }
            }
            else
            {
                InputSystem.QueueStateEvent(Keyboard, new KeyboardState());
            }
        }

        void ProcessMouseMoveEventDefered(short x, short y, byte button)
        {
            // switch input layout if necessay
            this.SwitchCurrentControlScheme(EventType.KeyboardAndMouse);

            var position = new Vector2Int(x, y);

            var state = new MouseState
            {
                position = position,
                delta = position - LastMousePosition.position,
                buttons = button,
            };

            InputSystem.QueueStateEvent(Mouse, state);

            // remember last state
            LastMousePosition = state;
        }

        void ProcessMouseWheelEventDefered(float scrollX, float scrollY)
        {
            // switch input layout if necessay
            this.SwitchCurrentControlScheme(EventType.KeyboardAndMouse);

            InputSystem.QueueStateEvent(Mouse, new MouseState { scroll = new Vector2(scrollX, scrollY) });
        }

        void ProcessTouchMoveEventDefered(TouchState[] touches)
        {
            // switch input layout if necessay
            this.SwitchCurrentControlScheme(EventType.Touch);

            for (var i = 0; i < touches.Length; i++)
            {
                // hack: steint, 14/10/19: since we cannot use Unity Time.time in ProcessRemoteInput method
                if (touches[i].phase == TouchPhase.Began)
                {
                    touches[i].startTime = Time.time;
                    TouchState lastState;
                    this.LastTouchState.TryGetValue(touches[i].touchId, out lastState);
                    lastState.startTime = Time.time;
                }

                InputSystem.QueueStateEvent(Touch, touches[i]);
            }
        }

        void ChangeEndStateUnusedTouches(TouchState[] touches)
        {
            // switch input layout if necessay
            this.SwitchCurrentControlScheme(EventType.Touch);

            int touchCount = Touch.touches.Count;
            for (var i = 0; i < touchCount; i++)
            {
                int touchId = Touch.touches[i].touchId.ReadValue();
                if (!Array.Exists(touches, v => v.touchId == touchId))
                {
                    if (Touch.touches[i].phase.ReadValue() == TouchPhase.Ended)
                    {
                        this.LastTouchState.Remove(touchId);
                        continue;
                    }

                    InputSystem.QueueStateEvent(Touch, new TouchState
                    {
                        touchId = touchId,
                        phase = TouchPhase.Ended,
                        position = Touch.touches[i].position.ReadValue()
                    });
                }
            }
        }

        private void OnUserApplicationModeActivated(IUserApplicationMode mode)
        {
            this.PlayerInput.SwitchCurrentActionMap(mode.ApplicationModeActionMap());
        }

        private void SwitchCurrentControlScheme(EventType ExpectedScheme)
        {
            // switch input layout if necessay
            if (this.LastReceivedEventType != ExpectedScheme)
            {
                switch (ExpectedScheme)
                {
                    case EventType.KeyboardAndMouse:
                    {
                        this.PlayerInput.defaultControlScheme = "Keyboard&Mouse";
                        this.PlayerInput.SwitchCurrentControlScheme(this.Keyboard, this.Mouse);
                        break;
                    }

                    case EventType.Touch:
                    {
                        this.PlayerInput.defaultControlScheme = "Touch";
                        this.PlayerInput.SwitchCurrentControlScheme(this.Touch);
                        break;
                    }
                }
            }
            this.LastReceivedEventType = ExpectedScheme;
        }
    }
}
