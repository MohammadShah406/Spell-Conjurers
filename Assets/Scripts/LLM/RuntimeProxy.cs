using System.Reflection;
using UnityEngine;

public class RuntimeProxy : MonoBehaviour
{
    private object instance;
    private MethodInfo startMethod;
    private MethodInfo updateMethod;
    private MethodInfo runMethod;

    public void Initialize(object scriptInstance)
    {
        instance = scriptInstance;
        var type = instance.GetType();

        startMethod = type.GetMethod("Start");
        updateMethod = type.GetMethod("Update");
        runMethod = type.GetMethod("Run");

        // Call Start() and Run() once on initialization
        startMethod?.Invoke(instance, null);
        runMethod?.Invoke(instance, null);
    }

    void Update()
    {
        updateMethod?.Invoke(instance, null);
    }
}