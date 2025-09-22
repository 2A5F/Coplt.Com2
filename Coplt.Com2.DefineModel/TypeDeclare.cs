using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Coplt.Com2.DefineModel;

public record struct TypeDeclare
{
    public required TypeKind Kind { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint? Index { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TypeFlags Flags { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ImmutableArray<uint> Params { get; set; }
}

public enum TypeKind : uint
{
    Unknown,

    /// <summary>
    /// <see cref="TypeDeclare.Index"/> means index in struct
    /// </summary>
    Generic,
    /// <summary>
    /// <see cref="TypeDeclare.Index"/> means struct index
    /// <see cref="TypeDeclare.Params"/> means type arguments
    /// </summary>
    Struct,
    /// <summary>
    /// <see cref="TypeDeclare.Index"/> means enum index
    /// </summary>
    Enum,
    /// <summary>
    /// <see cref="TypeDeclare.Index"/> means target type index
    /// </summary>
    Ptr,
    /// <summary>
    /// <see cref="TypeDeclare.Index"/> means return type index
    /// <see cref="TypeDeclare.Params"/> means function arguments
    /// </summary>
    Fn,

    Void,
    Bool,
    Int8,
    Int16,
    Int32,
    Int64,
    Int128,
    IntPtr,
    UInt8,
    UInt16,
    UInt32,
    UInt64,
    UInt128,
    UIntPtr,
    Float,
    Double,
    Char8,
    Char16,
    Guid,
    HResult,
    NSpan,
    NRoSpan,
    Str8,
    Str16,
    StrAny,
}

[Flags]
public enum TypeFlags
{
    None,
    Const,
}
