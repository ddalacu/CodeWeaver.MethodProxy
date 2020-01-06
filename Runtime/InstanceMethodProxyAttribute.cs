using System;


/// <summary>
/// The method you mark with this attribute requires at least one parameter that parameter will be the target type on which we search the target method, the rest of the params need to correspond to the target method params
/// Method you mark with this attribute needs to be static
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class InstanceMethodProxyAttribute : Attribute
{
    public string MethodName { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodName">The method the marked method should call</param>
    public InstanceMethodProxyAttribute(string methodName)
    {
        MethodName = methodName;
    }
}