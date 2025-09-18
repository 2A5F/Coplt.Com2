using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures.Types;

namespace Coplt.Com2.Symbols;

public class SymbolDb
{
    public ConcurrentDictionary<string, TypeSymbol> Symbols { get; } = new();
    public ConcurrentDictionary<string, StructDeclareSymbol> Structs { get; } = new();
    public ConcurrentDictionary<string, EnumDeclareSymbol> Enums { get; } = new();

    public SymbolDb()
    {
        TryAddSymbol(new("Coplt.Com.HResult") { Kind = TypeKind.HResult });
        TryAddSymbol(new("System.Void") { Kind = TypeKind.Void });
        TryAddSymbol(new("System.Boolean") { Kind = TypeKind.Bool });
        TryAddSymbol(new("System.Byte") { Kind = TypeKind.UInt8 });
        TryAddSymbol(new("System.UInt16") { Kind = TypeKind.UInt16 });
        TryAddSymbol(new("System.UInt32") { Kind = TypeKind.UInt32 });
        TryAddSymbol(new("System.UInt64") { Kind = TypeKind.UInt64 });
        TryAddSymbol(new("System.UInt128") { Kind = TypeKind.UInt128 });
        TryAddSymbol(new("System.UIntPtr") { Kind = TypeKind.UIntPtr });
        TryAddSymbol(new("System.SByte") { Kind = TypeKind.Int8 });
        TryAddSymbol(new("System.Int16") { Kind = TypeKind.Int16 });
        TryAddSymbol(new("System.Int32") { Kind = TypeKind.Int32 });
        TryAddSymbol(new("System.Int64") { Kind = TypeKind.Int64 });
        TryAddSymbol(new("System.Int128") { Kind = TypeKind.Int128 });
        TryAddSymbol(new("System.IntPtr") { Kind = TypeKind.IntPtr });
        TryAddSymbol(new("System.Single") { Kind = TypeKind.Float });
        TryAddSymbol(new("System.Double") { Kind = TypeKind.Double });
        TryAddSymbol(new("System.Char") { Kind = TypeKind.Char16 });
        TryAddSymbol(new("System.Guid") { Kind = TypeKind.Guid });
    }

    private void TryAddSymbol(TypeSymbol symbol) => Symbols.TryAdd(symbol.FullName, symbol);

    public TypeSymbol ExtraType(TypeDefinition type)
    {
        var full_name = type.FullName;
        var symbol = Symbols.GetOrAdd(full_name, name => new(name));
        if (symbol.Kind != TypeKind.Unknown) return symbol;
        // init symbol
        throw new NotImplementedException("Unknown type");
        return null!;
    }

    public TypeSymbol ExtraType(TypeSignature type)
    {
        if (type is PointerTypeSignature pt)
        {
            var target = ExtraType(pt.BaseType);
            var symbol = Symbols.GetOrAdd($"{target.FullName}*", name => new(name));
            if (symbol.Kind != TypeKind.Unknown) return symbol;
            symbol.Kind = TypeKind.Ptr;
            symbol.TargetOrReturn = target;
            return symbol;
        }
        else if (type is TypeDefOrRefSignature tdr)
        {
            var td = tdr.Resolve() ?? throw new Exception($"Resolve failed: {type}");
            return ExtraType(td);
        }
        else throw new NotImplementedException($"Unknown type: {type}");
        return null!;
    }
}

public record TypeSymbol(string FullName)
{
    public string FullName { get; } = FullName;
    public string Name { get; } = FullName.Split('.', '+').Last();
    public TypeKind Kind { get; set; }
    public TypeFlags Flags { get; set; }
    public ADeclareSymbol? Declare { get; set; }
    public TypeSymbol? TargetOrReturn { get; set; }
    public ImmutableArray<TypeSymbol> GenericsOrParams { get; set; }
    public uint Index { get; set; }

    public override string ToString() => Kind is TypeKind.Fn
        ? $"{((Flags & TypeFlags.Const) != 0 ? "const " : "")}{{ {TargetOrReturn} ({string.Join(",", GenericsOrParams)}) }}*"
        : $"{((Flags & TypeFlags.Const) != 0 ? "const " : "")}{Name}{(GenericsOrParams.IsDefaultOrEmpty ? "" : $"<{string.Join(",", GenericsOrParams)}>")}";
}

public enum TypeKind
{
    Unknown,

    // use Declare, Index
    Generic,
    // use Declare
    Struct,
    // use Declare, Generics
    Enum,
    // use Target
    Ptr,
    // use Return Params
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
    None = 0,
    Const = 1 << 1,
}

public record ADeclareSymbol
{
    public required string Name { get; set; }
}

[Flags]
public enum StructFlags
{
    None = 0,
    Union = 1 << 0,
    RefOnly = 1 << 1,
}

public record StructDeclareSymbol : ADeclareSymbol
{
    public required StructFlags Flags { get; set; }
    public required List<string> TypeParams { get; set; }
    public required List<StructField> Fields { get; set; }
}

public record struct StructField
{
    public TypeSymbol Type { get; set; }
    public string Name { get; set; }
}

[Flags]
public enum EnumFlags
{
    None = 0,
    Flags = 1 << 0,
}

public record EnumDeclareSymbol : ADeclareSymbol
{
    public required EnumFlags Flags { get; set; }
    public required TypeSymbol UnderlyingType { get; set; }
    public required List<EnumItem> Items { get; set; }
}

public record struct EnumItem
{
    public required Int128 Value { get; set; }
    public required string Name { get; set; }
}
