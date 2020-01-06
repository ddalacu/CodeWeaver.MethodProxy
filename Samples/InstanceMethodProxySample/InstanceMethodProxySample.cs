using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstanceMethodProxySample : MonoBehaviour
{
    public class ExampleClass
    {
        public int NonPublicMethod(string message)
        {
            Debug.Log($"I don t know how you called this but here is your message {message}");
        }
    }

    [InstanceMethodProxyAttribute("NonPublicMethod")]
    private static extern int AnyDesiredName(ExampleClass exampleClass, string message);

    [InstanceMethodProxyAttribute("NonPublicMethod")]
    private static int AnyDesiredName2(ExampleClass exampleClass, string message)
    {
        throw new Exception("This should have been patched!");
    }

    private void Start()
    {
        var instance = new ExampleClass();

        AnyDesiredName(instance, "this works!!");
        AnyDesiredName2(instance, "it really does!!");
    }

}
