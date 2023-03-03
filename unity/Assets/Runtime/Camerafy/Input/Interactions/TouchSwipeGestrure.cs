using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Camerafy.Input.Interactions
{

    #if UNITY_EDITOR
    [InitializeOnLoad]
    #endif
    public class TouchSwipeGestureComposite : InputBindingComposite<Vector2>
    {
        [InputControl(layout = "Button")]
        public int touch0;
        [InputControl(layout = "Double")]
        public int touch0_x;
        [InputControl(layout = "Double")]
        public int touch0_y;

        [InputControl(layout = "Button")]
        public int touch1;
      
        static TouchSwipeGestureComposite()
        {
            InputSystem.RegisterBindingComposite<TouchSwipeGestureComposite>();
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            // Will execute the static constructor as a side effect.
        }



        public override Vector2 ReadValue(ref InputBindingCompositeContext ctx)
        {
            // if two touches are registered
            if (ctx.ReadValueAsButton(this.touch0) && !ctx.ReadValueAsButton(this.touch1))
            {
                return new Vector2(ctx.ReadValue<float>(this.touch0_x), ctx.ReadValue<float>(this.touch0_y));
            }
            // else no swipe gesture
            else
            {
                return Vector2.zero;
            }
        }
    }
}
