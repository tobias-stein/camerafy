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
    public class TouchPinchGestureComposite : InputBindingComposite<float>
    {
        [InputControl(layout = "Button")]
        public int touch0;
        [InputControl(layout = "Double")]
        public int touch0_x;
        [InputControl(layout = "Double")]
        public int touch0_y;

        [InputControl(layout = "Button")]
        public int touch1;
        [InputControl(layout = "Double")]
        public int touch1_x;
        [InputControl(layout = "Double")]
        public int touch1_y;


        float LastDistance = -1.0f;
        float LastZoom = 0.0f;
      
        static TouchPinchGestureComposite()
        {
            InputSystem.RegisterBindingComposite<TouchPinchGestureComposite>();
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            // Will execute the static constructor as a side effect.
        }



        public override float ReadValue(ref InputBindingCompositeContext ctx)
        {
            // if two touches are registered
            if (ctx.ReadValueAsButton(this.touch0) && ctx.ReadValueAsButton(this.touch1))
            {
                Vector2 a = new Vector2(ctx.ReadValue<float>(this.touch0_x), ctx.ReadValue<float>(this.touch0_y));
                Vector2 b = new Vector2(ctx.ReadValue<float>(this.touch1_x), ctx.ReadValue<float>(this.touch1_y));

                float dist = Vector2.Distance(a, b);
                float zoom = this.LastZoom;
                {
                    if (this.LastDistance > -1.0f)
                    {
                        if (dist < this.LastDistance)
                        {
                            zoom = -1.0f;
                        }
                        else if (dist > this.LastDistance)
                        {
                            zoom = +1.0f;
                        }
                    }
                }
                this.LastDistance = dist;
                this.LastZoom = zoom;

                return zoom;
            }
            // else no pinch gesture
            else
            {
                this.LastDistance = -1.0f;
                this.LastZoom = 0.0f;
                return 0.0f;
            }
        }
    }
}
