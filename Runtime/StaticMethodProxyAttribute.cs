using System;

/// <summary>
/// Params of the method you mark need to correspond to the target method params
/// Method you mark with this attribute needs to be static
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class StaticMethodProxyAttribute : Attribute
{
    public Type Type { get; }
    public string MethodName { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type">The type which contains the method</param>
    /// <param name="methodName">The method the marked method should call</param>
    public StaticMethodProxyAttribute(Type type, string methodName)
    {
        Type = type;
        MethodName = methodName;
    }
}