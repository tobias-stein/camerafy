using UnityEngine;

class DebugCursor : MonoBehaviour
{
    public RectTransform UITransform;
    public Camerafy.User.User User;

    private void Update()
    {
        if (this.User.Input.Touch.press.isPressed)
        {
            UITransform.anchoredPosition = this.User.Input.Touch.position.ReadValue();
        }
        else
        {
            UITransform.anchoredPosition = this.User.Input.Mouse.position.ReadValue();
        }
    }
}
