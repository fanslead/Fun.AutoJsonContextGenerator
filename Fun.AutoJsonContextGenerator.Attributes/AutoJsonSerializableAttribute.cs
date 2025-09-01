using System;

namespace Fun.AutoJsonContextGenerator;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
public class AutoJsonSerializableAttribute : Attribute
{
}
