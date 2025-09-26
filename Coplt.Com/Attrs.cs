using System.Runtime.InteropServices;

namespace Coplt.Com;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class OpaqueAttribute : Attribute;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class TransparentAttribute : Attribute;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property)]
public sealed class ConstAttribute(bool IsConst) : Attribute
{
    public bool IsConst { get; } = IsConst;

    public ConstAttribute() : this(true) { }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class ComTypeAttribute<T> : Attribute;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class RefOnlyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class InterfaceAttribute(Type? Extend = null) : Attribute
{
    public Type? Extend { get; } = Extend;
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class MarkInterfaceAttribute(Type Type) : Attribute
{
    public Type Type { get; } = Type;
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public sealed class InterfaceMemberAttribute(uint Index) : Attribute
{
    public uint Index { get; } = Index;
}

[AttributeUsage(AttributeTargets.Struct)]
public sealed class ComMarshalAsAttribute(ComUnmanagedType Type) : Attribute
{
    public ComUnmanagedType Type { get; } = Type;
}

public enum ComUnmanagedType
{
    I8,
    U8,
    I16,
    U16,
    I32,
    ISize,
    U32,
    I64,
    U64,
    USize,
    F4,
    F8,
}
