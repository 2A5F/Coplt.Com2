using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace Coplt.Com2.Symbols;

internal class SymbolDb
{
    public ConcurrentDictionary<string, TypeSymbol> Symbols { get; } = new();
    public ConcurrentDictionary<string, StructDeclareSymbol> Structs { get; } = new();
    public ConcurrentDictionary<string, EnumDeclareSymbol> Enums { get; } = new();
    public ConcurrentDictionary<string, InterfaceDeclareSymbol> Interfaces { get; } = new();

    public static readonly FrozenDictionary<string, ComType> ComTypes;

    static SymbolDb()
    {
        Dictionary<string, ComType> com_types = new()
        {
            { "Coplt.Com.Ptr`1", ComType.Ptr },
            { "Coplt.Com.ConstPtr`1", ComType.ConstPtr },
            { "Coplt.Com.NonNull`1", ComType.NonNull },
            { "Coplt.Com.ConstNonNull`1", ComType.ConstNonNull },
        };
        ComTypes = com_types.ToFrozenDictionary();
    }

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
    
    public void Load(string path)
    {
        var asm = AssemblyDefinition.FromFile(path);
        var interface_marks = asm.FindCustomAttributes("Coplt.Com", "MarkInterfaceAttribute")
            .Select(mark => ((TypeDefOrRefSignature)mark.Signature!.FixedArguments[0].Element!).Resolve()!)
            .ToList();

        foreach (var type in interface_marks)
        {
            ExtraInterface(type, true);
        }
    }

    public InterfaceDeclareSymbol ExtraInterface(TypeDefinition type, bool export)
    {
        var name = $"{type.Name}";
        _ = Guid.TryParse($"{type.FindCustomAttributes("System.Runtime.InteropServices", "GuidAttribute").FirstOrDefault()
            ?.Signature!.FixedArguments[0].Element!}", out var guid);
        List<InterfaceMethod> methods = new();
        foreach (var method in type.Methods)
        {
            var member_attr = method.FindCustomAttributes("Coplt.Com", "InterfaceMemberAttribute").FirstOrDefault();
            if (member_attr == null) continue;
            var member_index = (uint)member_attr.Signature!.FixedArguments[0].Element!;
            var member_name = $"{method.Name}";
            var sig = method.Signature!;
            var ret_type = ExtraType(sig.ReturnType);
            List<InterfaceMethodParam> imp = new();
            // todo props
            foreach (var p in method.Parameters)
            {
                var p_type = ExtraType(p);
                var p_name = p.Name;
                var flags = ExtraRef(p);
                imp.Add(new()
                {
                    Name = p_name,
                    Type = p_type,
                    Flags = flags,
                });
            }
            {
                methods.Add(new()
                {
                    Name = member_name,
                    Index = member_index,
                    Flags = MethodFlags.None,
                    ReturnType = ret_type,
                    Params = imp,
                });
            }
        }
        return Interfaces.AddOrUpdate(name,
            static (name, a) => new() { Name = name, Guid = a.guid, Methods = a.methods, Export = a.export },
            static (_, old, a) =>
            {
                if (a.export) old.Export = a.export;
                return old;
            },
            (guid, methods, export)
        );
    }

    private static ParamFlags ExtraRef(Parameter parameter) => ExtraRef(parameter.Definition!);

    private static ParamFlags ExtraRef(ParameterDefinition parameter)
    {
        if ((parameter.Attributes & ParameterAttributes.In) != 0) return ParamFlags.In;
        if ((parameter.Attributes & ParameterAttributes.Out) != 0) return ParamFlags.Out;
        return ParamFlags.None;
    }

    private TypeSymbol ExtraType(TypeDefinition type)
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
        switch (type)
        {
            case TypeDefOrRefSignature or CorLibTypeSignature:
            {
                var td = type.Resolve() ?? throw new Exception($"Resolve failed: {type}");
                return ExtraType(td);
            }
            case PointerTypeSignature pt:
            {
                var target = ExtraType(pt.BaseType);
                var symbol = Symbols.GetOrAdd($"{target.FullName}*", name => new(name));
                if (symbol.Kind != TypeKind.Unknown) return symbol;
                symbol.Kind = TypeKind.Ptr;
                symbol.TargetOrReturn = target;
                return symbol;
            }
            case GenericInstanceTypeSignature git:
            {
                var gt = git.GenericType;
                if (ComTypes.TryGetValue(gt.FullName, out var com_type))
                {
                    if (com_type == ComType.Unknown) throw new NotSupportedException($"Unknown type: {type}");
                    if (com_type is ComType.Ptr or ComType.ConstPtr)
                    {
                        var target = ExtraType(git.TypeArguments[0]);
                        var symbol = Symbols.GetOrAdd($"const {target.FullName}*", name => new(name));
                        if (symbol.Kind != TypeKind.Unknown) return symbol;
                        symbol.Kind = TypeKind.Ptr;
                        symbol.TargetOrReturn = target;
                        symbol.Flags |= TypeFlags.Const;
                        return symbol;
                    }
                }
                throw new NotImplementedException($"Unknown type: {type}");
            }
            default:
                throw new NotImplementedException($"Unknown type: {type}");
        }
        return null!;
    }

    public TypeSymbol ExtraType(Parameter parameter)
    {
        var com_type = parameter.Definition?.FindCustomAttributesNoGeneric("Coplt.Com", "ComTypeAttribute`1").FirstOrDefault();
        if (com_type != null)
        {
            var attr_type = (GenericInstanceTypeSignature)com_type.Constructor?.DeclaringType!.ToTypeSignature()!;
            var target = attr_type.TypeArguments[0];
            return ExtraType(target);
        }
        return ExtraType(parameter.ParameterType);
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

public enum ComType
{
    Unknown,
    Ptr,
    ConstPtr,
    NonNull,
    ConstNonNull,
}

public enum TypeKind
{
    Unknown,

    // use Index
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

public record InterfaceDeclareSymbol : ADeclareSymbol
{
    public required bool Export { get; set; }
    public required Guid Guid { get; set; }
    public required List<InterfaceMethod> Methods { get; set; }
}

[Flags]
public enum ParamFlags
{
    None = 0,
    In,
    Out,
}

[Flags]
public enum MethodFlags
{
    None = 0,
    Const = 1 << 0,
    ReturnByRef = 1 << 1,
}

public record struct InterfaceMethod
{
    public string Name { get; set; }
    public uint Index { get; set; }
    public MethodFlags Flags { get; set; }
    public TypeSymbol ReturnType { get; set; }
    public required List<InterfaceMethodParam> Params { get; set; }
}

public record struct InterfaceMethodParam
{
    public TypeSymbol Type { get; set; }
    public ParamFlags Flags { get; set; }
    public string Name { get; set; }
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
