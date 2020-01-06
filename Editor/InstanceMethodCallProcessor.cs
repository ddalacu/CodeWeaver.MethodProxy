using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEngine;


[InitializeOnLoad]
public class InstanceMethodCallProcessor : IWeaverProcessor
{
    static InstanceMethodCallProcessor()
    {
        CodeWeaver.AddProcessor(new InstanceMethodCallProcessor());
    }

    public static bool AreEqualShifted(MethodDefinition a, MethodDefinition b)
    {
        var parameters = a.Parameters;
        var paramsCount = parameters.Count - 1;

        var methodDefinitionParameters = b.Parameters;
        if (paramsCount != methodDefinitionParameters.Count)
            return false;

        for (var index = 0; index < paramsCount; index++)
            if (CecilUtils.AreEqual(methodDefinitionParameters[index].ParameterType, parameters[index + 1].ParameterType) == false)
                return false;

        if (CecilUtils.AreEqual(a.ReturnType, b.ReturnType) == false)
            return false;

        return true;
    }

    private static bool FindInstanceMethod(MethodDefinition originalDefinition, TypeReference reference, string methodName, out MethodDefinition result)
    {
        var typeDefinition = reference.Resolve();
        foreach (var typeDefinitionMethod in typeDefinition.Methods)
        {
            if (typeDefinitionMethod.IsStatic)
                continue;

            if (typeDefinitionMethod.Name != methodName)
                continue;

            if (AreEqualShifted(originalDefinition, typeDefinitionMethod) == false)
                continue;

            result = typeDefinitionMethod;
            return true;
        }

        result = null;
        return false;
    }

    private static bool FindStaticMethod(MethodDefinition originalDefinition, TypeReference reference, string methodName, out MethodDefinition result)
    {
        var typeDefinition = reference.Resolve();
        foreach (var typeDefinitionMethod in typeDefinition.Methods)
        {
            if (typeDefinitionMethod.IsStatic == false)
                continue;

            if (typeDefinitionMethod.Name != methodName)
                continue;

            if (CecilUtils.AreEqual(originalDefinition, typeDefinitionMethod) == false)
                continue;

            result = typeDefinitionMethod;
            return true;
        }

        result = null;
        return false;
    }


    private static void EmitInstanceCallBody(ModuleDefinition module, MethodDefinition methodDefinition, MethodDefinition toCall)
    {
        methodDefinition.Body = new MethodBody(methodDefinition);

        var ilProcessor = methodDefinition.Body.GetILProcessor();

        var parametersCount = methodDefinition.Parameters.Count;
        for (byte index = 0; index < parametersCount; index++)
            CecilUtils.EmitLoadArgForParam(ilProcessor, methodDefinition.Parameters[index]);

        ilProcessor.Emit(OpCodes.Callvirt, module.ImportReference(toCall));
        ilProcessor.Emit(OpCodes.Ret);
    }
    private static void EmitStaticCallBody(ModuleDefinition module, MethodDefinition methodDefinition, MethodDefinition toCall)
    {
        methodDefinition.Body = new MethodBody(methodDefinition);

        var ilProcessor = methodDefinition.Body.GetILProcessor();

        var parametersCount = methodDefinition.Parameters.Count;
        for (byte index = 0; index < parametersCount; index++)
            CecilUtils.EmitLoadArgForParam(ilProcessor, methodDefinition.Parameters[index]);

        ilProcessor.Emit(OpCodes.Call, module.ImportReference(toCall));
        ilProcessor.Emit(OpCodes.Ret);
    }

    private static string InstanceMethodProxyAttributeName = typeof(InstanceMethodProxyAttribute).FullName;
    private static string StaticMethodProxyAttributeName = typeof(StaticMethodProxyAttribute).FullName;


    public bool Execute(AssemblyDefinition assemblyDefinition)
    {
        var assemblyFullNames = new HashSet<string>
        {
            typeof(InstanceMethodProxyAttribute).Assembly.FullName
        };

        if (CecilUtils.AssemblyHaveReferencesTo(assemblyDefinition, assemblyFullNames) == false)//only process this assembly if it uses our attribute
            return false;

        var changed = false;

        foreach (var (moduleDefinition, _, methodDefinition) in CecilUtils.IterateMethods(assemblyDefinition))
        {
            if (methodDefinition.IsStatic == false)
                continue;

            foreach (var methodDefinitionCustomAttribute in methodDefinition.CustomAttributes)
            {
                if (CheckInstanceProxy(methodDefinitionCustomAttribute, methodDefinition, moduleDefinition))
                    changed = true;
                else
                if (CheckStaticProxy(methodDefinitionCustomAttribute, methodDefinition, moduleDefinition))
                    changed = true;
            }
        }

        return changed;
    }

    private static bool CheckInstanceProxy(CustomAttribute methodDefinitionCustomAttribute, MethodDefinition methodDefinition, ModuleDefinition moduleDefinition)
    {
        if (methodDefinitionCustomAttribute.AttributeType.FullName != InstanceMethodProxyAttributeName)
            return false;

        var methodDefinitionParameters = methodDefinition.Parameters;
        if (methodDefinitionParameters.Count == 0)
            throw new Exception($"{methodDefinition} should have at least the instance parameter!");

        var typeRef = methodDefinitionParameters[0].ParameterType;
        var methodName = (string)methodDefinitionCustomAttribute.ConstructorArguments[0].Value;

        if (FindInstanceMethod(methodDefinition, typeRef, methodName, out var method))
        {
            EmitInstanceCallBody(moduleDefinition, methodDefinition, method);
            Debug.Log($"{methodDefinition} now calls {method}");
            return true;
        }

        throw new Exception($"Could not find target method for {methodDefinition}");
    }


    private static bool CheckStaticProxy(CustomAttribute methodDefinitionCustomAttribute, MethodDefinition methodDefinition, ModuleDefinition moduleDefinition)
    {
        if (methodDefinitionCustomAttribute.AttributeType.FullName != StaticMethodProxyAttributeName)
            return false;

        var typeRef = (TypeReference)methodDefinitionCustomAttribute.ConstructorArguments[0].Value;
        var methodName = (string)methodDefinitionCustomAttribute.ConstructorArguments[1].Value;

        if (FindStaticMethod(methodDefinition, typeRef, methodName, out var method))
        {
            EmitStaticCallBody(moduleDefinition, methodDefinition, method);
            Debug.Log($"{methodDefinition} now calls {method}");
            return true;
        }

        throw new Exception($"Could not find target method for {methodDefinition}");
    }
}