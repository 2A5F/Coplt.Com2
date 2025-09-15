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
