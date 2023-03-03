using System;
using System.Reflection;
using UnityEngine;

public class AccessRCM : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // Get a Type object.
        Type t = typeof(UnityEngine.EventSystems.BaseRaycaster);
        // Instantiate an Assembly class to the assembly housing the Integer type.
        Assembly assem = Assembly.GetAssembly(t);
        // Display the name of the assembly.
        Debug.LogFormat("Name: {0}", assem.FullName);
        // Get the location of the assembly using the file: protocol.
        Debug.LogFormat("CodeBase: {0}", assem.CodeBase);
    }
}
