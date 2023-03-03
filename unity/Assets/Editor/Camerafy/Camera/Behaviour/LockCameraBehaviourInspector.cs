using UnityEditor;
using UnityEngine;

namespace Camerafy.Editor.Camera
{
    using Camerafy.Camera;

    [CustomEditor(typeof(Lock))]
    public class LockCameraBehaviourInspector : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            Lock lockBehavour = target as Lock;

            float radius = 10.0f;
            Vector3 center = Vector3.zero;//lockBehavour.TargetPosition;


            bool reversed = lockBehavour.MinYaw > lockBehavour.MaxYaw;
            float min = reversed ? lockBehavour.MaxYaw : lockBehavour.MinYaw;
            float max = reversed ? lockBehavour.MinYaw : lockBehavour.MaxYaw;

            min = -min;
            max = -max;

            Vector3 minYaw = Quaternion.AngleAxis(min, Vector3.up) * (reversed ? Vector3.back : Vector3.forward);
            Vector3 maxYaw = Quaternion.AngleAxis(max, Vector3.up) * (reversed ? Vector3.back : Vector3.forward);

            Vector3 minPitch = Quaternion.AngleAxis(lockBehavour.MinPitch, Vector3.right) * Vector3.up;
            Vector3 maxPitch = Quaternion.AngleAxis(lockBehavour.MaxPitch, Vector3.right) * Vector3.up;

            Handles.color = Color.green * 0.3f;
            Handles.DrawSolidArc(center, Vector3.up, minYaw, (max - min), radius);

            Handles.color = ((lockBehavour.MinPitch > lockBehavour.MaxPitch) ? Color.red : Color.green) * 0.3f;
            Handles.DrawSolidArc(center, Vector3.right, minPitch, (lockBehavour.MaxPitch - lockBehavour.MinPitch), radius);

            Handles.color = Color.white;
            Handles.DrawLines(new Vector3[] {
                center, center + (minYaw * radius),
                center, center + (maxYaw * radius),
                center, center + (minPitch * radius),
                center, center + (maxPitch * radius)
            });
        }
    }
}