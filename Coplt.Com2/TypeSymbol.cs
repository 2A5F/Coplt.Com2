using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using Coplt.Com2.DefineModel;

namespace Coplt.Com2.Symbols;

internal class SymbolDb
{
    public ConcurrentDictionary<string, TypeSymbol> Symbols { get; } = new();
    public ConcurrentDictionary<string, StructDeclareSymbol> Structs { get; } = new();
    public ConcurrentDictionary<string, EnumDeclareSymbol> Enums { get; } = new();
    public ConcurrentDictionary<string, InterfaceDeclareSymbol> Interfaces { get; } = new();

    public static readonly FrozenDictionary<string, TypeSymbol> StaticSymbols;
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

        Dictionary<string, TypeSymbol> dict = new();
        TryAddSymbol(dict, new("Coplt.Com.HResult") { Kind = TypeKind.HResult });
        TryAddSymbol(dict, new("Coplt.Com.OpaqueTypes.ComVoid") { Kind = TypeKind.Void });
        TryAddSymbol(dict, new("System.Void") { Kind = TypeKind.Void });
        TryAddSymbol(dict, new("System.Boolean") { Kind = TypeKind.Bool });
        TryAddSymbol(dict, new("System.Byte") { Kind = TypeKind.UInt8 });
        TryAddSymbol(dict, new("System.UInt16") { Kind = TypeKind.UInt16 });
        TryAddSymbol(dict, new("System.UInt32") { Kind = TypeKind.UInt32 });
        TryAddSymbol(dict, new("System.UInt64") { Kind = TypeKind.UInt64 });
        TryAddSymbol(dict, new("System.UInt128") { Kind = TypeKind.UInt128 });
        TryAddSymbol(dict, new("System.UIntPtr") { Kind = TypeKind.UIntPtr });
        TryAddSymbol(dict, new("System.SByte") { Kind = TypeKind.Int8 });
        TryAddSymbol(dict, new("System.Int16") { Kind = TypeKind.Int16 });
        TryAddSymbol(dict, new("System.Int32") { Kind = TypeKind.Int32 });
        TryAddSymbol(dict, new("System.Int64") { Kind = TypeKind.Int64 });
        TryAddSymbol(dict, new("System.Int128") { Kind = TypeKind.Int128 });
        TryAddSymbol(dict, new("System.IntPtr") { Kind = TypeKind.IntPtr });
        TryAddSymbol(dict, new("System.Single") { Kind = TypeKind.Float });
        TryAddSymbol(dict, new("System.Double") { Kind = TypeKind.Double });
        TryAddSymbol(dict, new("System.Char") { Kind = TypeKind.Char16 });
        TryAddSymbol(dict, new("System.Guid") { Kind = TypeKind.Guid });
        StaticSymbols = dict.ToFrozenDictionary();
    }

    private TypeSymbol GetStaticSymbol(string name)
    {
        return Symbols.GetOrAdd(name, name => StaticSymbols.TryGetValue(name, out var r) ? r : new(name));
    }

    private static void TryAddSymbol(Dictionary<string, TypeSymbol> dict, TypeSymbol symbol) => dict.TryAdd(symbol.FullName, symbol);

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

        foreach (var sds in Structs.Values)
        {
            DeepStruct(sds, 0);
        }
    }

    private void DeepStruct(StructDeclareSymbol sds, int deep)
    {
        sds.Deep = Math.Max(sds.Deep, deep);
        foreach (var field in sds.Fields)
        {
            if (field.Type is { Kind: TypeKind.Struct, Declare: StructDeclareSymbol child })
            {
                DeepStruct(child, deep + 1);
            }
        }
    }

    public ComDefine ToComDefine()
    {
        #region Types Tmp

        var types_tmp = Symbols
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select((a, i) =>
            {
                a.Id = (uint)i;
                return a;
            })
            .ToList();

        #endregion

        #region Struct Tmp

        var struct_tmp = Structs
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select((a, i) =>
            {
                a.Id = (uint)i;
                return a;
            })
            .ToList();

        #endregion

        #region Enum Tmp

        var enum_tmp = Enums
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select((a, i) =>
            {
                a.Id = (uint)i;
                return a;
            })
            .ToList();

        #endregion

        #region Interface

        var interfaces = Interfaces
            .AsParallel()
            .Where(a => a.Value.Export)
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select((a, i) =>
            {
                a.Id = (uint)i;
                return a;
            })
            .Select(a => new InterfaceDeclare
            {
                Name = a.Name,
                Guid = a.Guid,
                Parent = a.Parent?.Guid,
                Methods =
                [
                    ..a.Methods.Select(m => new MethodDeclare
                    {
                        Name = m.Name,
                        Index = m.Index,
                        Flags = ToComDefine(m.Flags),
                        ReturnType = m.ReturnType.Id,
                        Parameters =
                        [
                            ..m.Params.Select(p => new ParameterDeclare
                            {
                                Name = p.Name,
                                Flags = ToComParam(p.Flags),
                                Type = p.Type.Id,
                            })
                        ],
                    })
                ],
            })
            .ToImmutableArray();

        #endregion

        #region Types

        var types = types_tmp.AsParallel().AsOrdered().Select(a =>
        {
            switch (a.Kind)
            {
                case TypeKind.Interface:
                    return new TypeDeclare
                    {
                        Kind = DefineModel.TypeKind.Interface,
                        Index = a.Declare!.Id,
                        Flags = ToComDefine(a.Flags),
                    };
                case TypeKind.Generic:
                    return new TypeDeclare
                    {
                        Kind = DefineModel.TypeKind.Generic,
                        Index = a.Index,
                        Flags = ToComDefine(a.Flags),
                    };
                case TypeKind.Struct:
                    return new TypeDeclare
                    {
                        Kind = DefineModel.TypeKind.Struct,
                        Index = a.Declare!.Id,
                        Flags = ToComDefine(a.Flags),
                        Params = a.GenericsOrParams.IsDefaultOrEmpty ? default : [..a.GenericsOrParams.Select(static a => a.Id)],
                    };
                case TypeKind.Enum:
                    return new TypeDeclare
                    {
                        Kind = DefineModel.TypeKind.Enum,
                        Index = a.Declare!.Id,
                        Flags = ToComDefine(a.Flags),
                    };
                case TypeKind.Ptr:
                    return new TypeDeclare
                    {
                        Kind = DefineModel.TypeKind.Ptr,
                        Index = a.TargetOrReturn!.Id,
                        Flags = ToComDefine(a.Flags),
                    };
                case TypeKind.Ref:
                    return new TypeDeclare
                    {
                        Kind = DefineModel.TypeKind.Ref,
                        Index = a.TargetOrReturn!.Id,
                        Flags = ToComDefine(a.Flags),
                    };
                case TypeKind.Fn:
                    return new TypeDeclare
                    {
                        Kind = DefineModel.TypeKind.Fn,
                        Flags = ToComDefine(a.Flags),
                        Index = a.TargetOrReturn!.Id,
                        Params = [..a.GenericsOrParams.Select(static a => a.Id)],
                    };
                default:
                    return new TypeDeclare
                    {
                        Kind = a.Kind switch
                        {
                            TypeKind.Void => DefineModel.TypeKind.Void,
                            TypeKind.Bool => DefineModel.TypeKind.Bool,
                            TypeKind.Int8 => DefineModel.TypeKind.Int8,
                            TypeKind.Int16 => DefineModel.TypeKind.Int16,
                            TypeKind.Int32 => DefineModel.TypeKind.Int32,
                            TypeKind.Int64 => DefineModel.TypeKind.Int64,
                            TypeKind.Int128 => DefineModel.TypeKind.Int128,
                            TypeKind.IntPtr => DefineModel.TypeKind.IntPtr,
                            TypeKind.UInt8 => DefineModel.TypeKind.UInt8,
                            TypeKind.UInt16 => DefineModel.TypeKind.UInt16,
                            TypeKind.UInt32 => DefineModel.TypeKind.UInt32,
                            TypeKind.UInt64 => DefineModel.TypeKind.UInt64,
                            TypeKind.UInt128 => DefineModel.TypeKind.UInt128,
                            TypeKind.UIntPtr => DefineModel.TypeKind.UIntPtr,
                            TypeKind.Float => DefineModel.TypeKind.Float,
                            TypeKind.Double => DefineModel.TypeKind.Double,
                            TypeKind.Char8 => DefineModel.TypeKind.Char8,
                            TypeKind.Char16 => DefineModel.TypeKind.Char16,
                            TypeKind.Guid => DefineModel.TypeKind.Guid,
                            TypeKind.HResult => DefineModel.TypeKind.HResult,
                            TypeKind.NSpan => DefineModel.TypeKind.NSpan,
                            TypeKind.NRoSpan => DefineModel.TypeKind.NRoSpan,
                            TypeKind.Str8 => DefineModel.TypeKind.Str8,
                            TypeKind.Str16 => DefineModel.TypeKind.Str16,
                            TypeKind.StrAny => DefineModel.TypeKind.StrAny,
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        Flags = ToComDefine(a.Flags),
                    };
            }
        }).ToImmutableArray();

        #endregion

        #region Structs

        var structs = struct_tmp.AsParallel().AsOrdered()
            .Select(a => new StructDeclare
            {
                Name = a.Name.Split('`').First().Split('.', '+').Last(),
                Flags = ToComDefine(a.Flags),
                TypeParams = a.TypeParams.Count == 0 ? default : [..a.TypeParams],
                Fields =
                [
                    ..a.Fields.Select(f => new FieldDeclare
                    {
                        Type = f.Type.Id,
                        Name = f.Name,
                    })
                ],
                MarshalAs = a.MarshalAs?.Id,
            }).ToImmutableArray();

        #endregion

        #region Enums

        var enums = enum_tmp.AsParallel().AsOrdered()
            .Select(a => new EnumDeclare
            {
                Name = a.Name.Split('.', '+').Last(),
                Flags = ToComDefine(a.Flags),
                Underlying = a.UnderlyingType.Id,
                Items =
                [
                    ..a.Items.Select(f => new EnumItemDeclare
                    {
                        Name = f.Name,
                        Value = f.Value,
                    })
                ]
            })
            .ToImmutableArray();

        #endregion

        return new()
        {
            Types = types,
            Structs = structs,
            Enums = enums,
            Interfaces = interfaces,
        };
    }

    public InterfaceDeclareSymbol ExtraInterface(TypeDefinition type, bool export)
    {
        var name = $"{type.Name}";
        var decl = Interfaces.GetOrAdd(name, _ => new() { Name = null! });
        if (export) decl.Export = true;
        if (decl.Name != null!) return decl;
        _ = Guid.TryParse($"{type.FindCustomAttributes("System.Runtime.InteropServices", "GuidAttribute").FirstOrDefault()
            ?.Signature!.FixedArguments[0].Element!}", out var guid);
        var interface_attr = type.FindCustomAttributes("Coplt.Com", "InterfaceAttribute").FirstOrDefault();
        List<InterfaceMethod> methods = new();
        foreach (var method in type.Methods)
        {
            var member_attr = method.FindCustomAttributes("Coplt.Com", "InterfaceMemberAttribute").FirstOrDefault();
            if (member_attr == null) continue;
            var member_index = (uint)member_attr.Signature!.FixedArguments[0].Element!;
            var member_name = $"{method.Name}";
            var ret_type = ExtraType(method);
            List<InterfaceMethodParam> imp = new();
            var is_readonly = method.FindCustomAttributes("System.Runtime.CompilerServices", "IsReadOnlyAttribute").FirstOrDefault();
            var method_flags = is_readonly is null ? MethodFlags.None : MethodFlags.Const;
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
            methods.Add(new()
            {
                Name = member_name,
                Index = member_index,
                Flags = method_flags,
                ReturnType = ret_type,
                Params = imp,
            });
        }
        foreach (var property in type.Properties)
        {
            var member_attr = property.FindCustomAttributes("Coplt.Com", "InterfaceMemberAttribute").FirstOrDefault();
            if (member_attr == null) continue;
            var member_index = (uint)member_attr.Signature!.FixedArguments[0].Element!;
            var member_type = ExtraType(property);
            var get_method = property.GetMethod;
            var set_method = property.SetMethod;
            var inc = 0u;
            if (get_method != null)
            {
                var is_readonly = get_method.FindCustomAttributes("System.Runtime.CompilerServices", "IsReadOnlyAttribute").FirstOrDefault();
                var member_name = $"get_{property.Name}";
                var method_flags = MethodFlags.Getter;
                method_flags |= is_readonly is null ? MethodFlags.None : MethodFlags.Const;
                methods.Add(new()
                {
                    Name = member_name,
                    Index = member_index + inc++,
                    Flags = method_flags,
                    ReturnType = member_type,
                    Params = [],
                });
            }
            if (set_method != null)
            {
                var is_readonly = set_method.FindCustomAttributes("System.Runtime.CompilerServices", "IsReadOnlyAttribute").FirstOrDefault();
                var member_name = $"set_{property.Name}";
                var method_flags = MethodFlags.Setter;
                method_flags |= is_readonly is null ? MethodFlags.None : MethodFlags.Const;
                methods.Add(new()
                {
                    Name = member_name,
                    Index = member_index + inc,
                    Flags = method_flags,
                    ReturnType = GetStaticSymbol("System.Void"),
                    Params =
                    [
                        new()
                        {
                            Type = member_type,
                            Flags = ParamFlags.None,
                            Name = "value"
                        }
                    ],
                });
            }
        }
        methods.Sort((a, b) => a.Index.CompareTo(b.Index));
        decl.Guid = guid;
        decl.Methods = methods;
        var parent = interface_attr?.Signature?.FixedArguments is [{ Element: TypeSignature par }]
            ? ExtraInterface(par.Resolve() ?? throw new Exception($"Resolve failed: {par}"), false)
            : null;
        decl.Parent = parent;
        decl.Name = name;
        return decl;
    }

    private static ParamFlags ExtraRef(Parameter parameter) => ExtraRef(parameter.Definition!);

    private static ParamFlags ExtraRef(ParameterDefinition parameter)
    {
        if ((parameter.Attributes & ParameterAttributes.In) != 0) return ParamFlags.In;
        if ((parameter.Attributes & ParameterAttributes.Out) != 0) return ParamFlags.Out;
        return ParamFlags.None;
    }

    private ADeclareSymbol ExtraDeclare(TypeDefinition type)
    {
        var full_name = type.FullName;
        if (type.IsEnum)
        {
            var decl = Enums.GetOrAdd(full_name, _ => new()
            {
                Name = null!,
                Flags = EnumFlags.None,
                UnderlyingType = null!,
                Items = []
            });
            if (decl.Name != null!) return decl;
            decl.Name = full_name;
            decl.UnderlyingType = ExtraType(type.GetEnumUnderlyingType()!);
            if (type.FindCustomAttributes("System", "FlagsAttribute").Any()) decl.Flags |= EnumFlags.Flags;
            foreach (var field in type.Fields)
            {
                var attr = field.Attributes;
                const FieldAttributes fa = FieldAttributes.Static | FieldAttributes.Literal;
                if ((attr & fa) != fa) continue;
                var val = $"{field.Constant!.Value!.InterpretData(field.Constant.Type)}";
                decl.Items.Add(new()
                {
                    Value = val,
                    Name = $"{field.Name}",
                });
            }
            return decl;
        }
        else
        {
            var decl = Structs.GetOrAdd(full_name, _ => new()
            {
                Name = null!,
                Flags = StructFlags.None,
                TypeParams = [],
                Fields = [],
            });
            if (decl.Name != null!) return decl;
            decl.Name = full_name;
            decl.TypeParams = type.GenericParameters.Select(a => $"{a.Name}").ToList();
            if ((type.Attributes & TypeAttributes.ExplicitLayout) != 0) decl.Flags |= StructFlags.Union;
            if (type.FindCustomAttributes("Coplt.Com", "RefOnlyAttribute").Any()) decl.Flags |= StructFlags.RefOnly;
            foreach (var field in type.Fields)
            {
                if ((field.Attributes & FieldAttributes.Static) != 0) continue;
                var ft = ExtraType(field);
                decl.Fields.Add(new()
                {
                    Type = ft,
                    Name = $"{field.Name}",
                });
            }
            return decl;
        }
    }

    private TypeSymbol ExtraType(TypeDefinition type)
    {
        var full_name = type.FullName;
        if (!type.IsValueType) throw new NotSupportedException($"Reference type is not support: {type}");
        if (type.IsByRefLike) throw new NotSupportedException($"ByRef type is not support: {type}");
        if (StaticSymbols.TryGetValue(full_name, out var symbol)) return symbol;
        symbol = Symbols.GetOrAdd(full_name, name => StaticSymbols.TryGetValue(name, out var r) ? r : new(name));
        if (symbol.Kind != TypeKind.Unknown) return symbol;
        if (type.FindCustomAttributes("Coplt.Com", "InterfaceAttribute").Any())
        {
            symbol.Kind = TypeKind.Interface;
            symbol.Declare = ExtraInterface(type, false);
            return symbol;
        }
        symbol.Kind = type.IsEnum ? TypeKind.Enum : TypeKind.Struct;
        symbol.Declare = ExtraDeclare(type);
        return symbol;
    }

    private TypeSymbol ExtraType(TypeSignature type)
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
            case ByReferenceTypeSignature brt:
            {
                var target = ExtraType(brt.BaseType);
                var symbol = Symbols.GetOrAdd($"{target.FullName}*", name => new(name));
                if (symbol.Kind != TypeKind.Unknown) return symbol;
                symbol.Kind = TypeKind.Ref;
                symbol.TargetOrReturn = target;
                return symbol;
            }
            case GenericInstanceTypeSignature git:
            {
                var gt = git.GenericType;
                if (ComTypes.TryGetValue(gt.FullName, out var com_type))
                {
                    if (com_type == ComType.Unknown) break;
                    if (com_type is ComType.Ptr or ComType.ConstPtr)
                    {
                        var target = ExtraType(git.TypeArguments[0]);
                        var symbol = Symbols.GetOrAdd($"const {target.FullName}*", name => new(name));
                        if (symbol.Kind != TypeKind.Unknown) return symbol;
                        symbol.Kind = TypeKind.Ptr;
                        symbol.TargetOrReturn = target;
                        if (com_type is ComType.ConstPtr) symbol.Flags |= TypeFlags.Const;
                        return symbol;
                    }
                }
                if (gt is TypeDefinition td)
                {
                    var decl = ExtraDeclare(td);
                    var args = git.TypeArguments.Select(ExtraType).ToImmutableArray();
                    var name = $"{decl.Name}<{string.Join(", ", args.Select(a => a.FullName))}>";
                    var symbol = Symbols.GetOrAdd(name, static name => new(name));
                    if (symbol.Kind != TypeKind.Unknown) return symbol;
                    symbol.Kind = TypeKind.Struct;
                    symbol.Declare = decl;
                    symbol.GenericsOrParams = args;
                    return symbol;
                }
                break;
            }
            case GenericParameterSignature gp:
            {
                var name = $"<>::{gp.Name}";
                var symbol = Symbols.GetOrAdd(name, static name => new(name));
                if (symbol.Kind != TypeKind.Unknown) return symbol;
                symbol.Kind = TypeKind.Generic;
                symbol.Index = (uint)gp.Index;
                return symbol;
            }
            case FunctionPointerTypeSignature fpt:
            {
                var symbol = Symbols.GetOrAdd($"*::{fpt.FullName}*", name => new(name));
                if (symbol.Kind != TypeKind.Unknown) return symbol;
                symbol.Kind = TypeKind.Fn;
                var sig = fpt.Signature;
                symbol.TargetOrReturn = ExtraType(sig.ReturnType);
                symbol.GenericsOrParams = [..sig.ParameterTypes.Select(ExtraType)];
                return symbol;
            }
        }
        throw new NotSupportedException($"Unknown type: {type}");
    }

    private TypeSymbol ExtraType(MethodDefinition method)
    {
        var com_type = method.Parameters.ReturnParameter.Definition?.FindCustomAttributesNoGeneric("Coplt.Com", "ComTypeAttribute`1").FirstOrDefault();
        if (com_type != null)
        {
            var attr_type = (GenericInstanceTypeSignature)com_type.Constructor?.DeclaringType!.ToTypeSignature()!;
            var target = attr_type.TypeArguments[0];
            return ExtraType(target);
        }
        var sig = method.Signature!;
        return ExtraType(sig.ReturnType);
    }

    private TypeSymbol ExtraType(Parameter parameter)
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

    private TypeSymbol ExtraType(PropertyDefinition property)
    {
        var com_type = property.FindCustomAttributesNoGeneric("Coplt.Com", "ComTypeAttribute`1").FirstOrDefault();
        if (com_type != null)
        {
            var attr_type = (GenericInstanceTypeSignature)com_type.Constructor?.DeclaringType!.ToTypeSignature()!;
            var target = attr_type.TypeArguments[0];
            return ExtraType(target);
        }
        return ExtraType(property.Signature!.ReturnType);
    }

    private TypeSymbol ExtraType(FieldDefinition field)
    {
        var com_type = field.FindCustomAttributesNoGeneric("Coplt.Com", "ComTypeAttribute`1").FirstOrDefault();
        if (com_type != null)
        {
            var attr_type = (GenericInstanceTypeSignature)com_type.Constructor?.DeclaringType!.ToTypeSignature()!;
            var target = attr_type.TypeArguments[0];
            return ExtraType(target);
        }
        return ExtraType(field.Signature!.FieldType);
    }

    private static DefineModel.TypeFlags ToComDefine(TypeFlags input)
    {
        var output = DefineModel.TypeFlags.None;
        if ((input & TypeFlags.Const) != 0) output |= DefineModel.TypeFlags.Const;
        return output;
    }
    private static DefineModel.MethodFlags ToComDefine(MethodFlags input)
    {
        var output = DefineModel.MethodFlags.None;
        if ((input & MethodFlags.Const) != 0) output |= DefineModel.MethodFlags.Const;
        if ((input & MethodFlags.ReturnByRef) != 0) output |= DefineModel.MethodFlags.ReturnByRef;
        if ((input & MethodFlags.Getter) != 0) output |= DefineModel.MethodFlags.Getter;
        if ((input & MethodFlags.Setter) != 0) output |= DefineModel.MethodFlags.Setter;
        return output;
    }

    private static DefineModel.StructFlags ToComDefine(StructFlags input)
    {
        var output = DefineModel.StructFlags.None;
        if ((input & StructFlags.Union) != 0) output |= DefineModel.StructFlags.Union;
        if ((input & StructFlags.RefOnly) != 0) output |= DefineModel.StructFlags.RefOnly;
        return output;
    }

    private static DefineModel.EnumFlags ToComDefine(EnumFlags input)
    {
        var output = DefineModel.EnumFlags.None;
        if ((input & EnumFlags.Flags) != 0) output |= DefineModel.EnumFlags.Flags;
        return output;
    }

    private static ParameterFlags ToComParam(ParamFlags input)
    {
        var output = ParameterFlags.None;
        if ((input & ParamFlags.In) != 0) output |= ParameterFlags.In;
        if ((input & ParamFlags.Out) != 0) output |= ParameterFlags.Out;
        return output;
    }
}

public record TypeSymbol(string FullName)
{
    public uint Id { get; set; }
    public string FullName { get; } = FullName;
    public string Name { get; } = FullName.Split('`').First().Split('.', '+').Last();
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

    Interface,
    // use Index
    Generic,
    // use Declare, Generics
    Struct,
    // use Declare
    Enum,
    // use Target
    Ptr,
    // use Target
    Ref,
    // use Return, Params
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

public static class TypeExtensions
{
    extension(TypeKind kind)
    {
        public bool IsStruct => kind switch
        {
            TypeKind.Struct => true,
            TypeKind.Int128 or
                TypeKind.UInt128 or
                TypeKind.Guid or
                TypeKind.HResult or
                TypeKind.NSpan or
                TypeKind.NRoSpan or
                TypeKind.Str8 or
                TypeKind.Str16 or
                TypeKind.StrAny => true,
            _ => false,
        };
    }
}

[Flags]
public enum TypeFlags
{
    None = 0,
    Const = 1 << 1,
}

public record ADeclareSymbol
{
    public uint Id { get; set; }
    public required string Name { get; set; }
}

public record InterfaceDeclareSymbol : ADeclareSymbol
{
    public bool Export { get; set; }
    public Guid Guid { get; set; }
    public InterfaceDeclareSymbol? Parent { get; set; }
    public List<InterfaceMethod> Methods { get; set; }
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
    Getter = 1 << 2,
    Setter = 1 << 3,
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
    public TypeSymbol? MarshalAs { get; set; }
    public int Deep;
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
    public required string Value { get; set; }
    public required string Name { get; set; }
}
