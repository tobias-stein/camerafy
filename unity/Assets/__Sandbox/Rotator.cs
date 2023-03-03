using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    public Vector3 Euler = Vector3.zero;

    void LateUpdate()
    {
        // rotate degrees/sec
        this.gameObject.transform.Rotate(this.Euler * Time.deltaTime);
    }
}
