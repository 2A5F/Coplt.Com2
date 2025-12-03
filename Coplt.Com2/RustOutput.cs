using System.Text;
using System.Text.Json.Serialization;
using Coplt.Com2.Symbols;

namespace Coplt.Com2;

public record RustOverride
{
    [JsonPropertyName("Debug")]
    public bool Debug { get; set; } = true;
    [JsonPropertyName("PartialEq")]
    public bool PartialEq { get; set; } = true;
    [JsonPropertyName("PartialOrd")]
    public bool PartialOrd { get; set; } = true;
}

public record RustOutput : AOutput
{
    public Dictionary<string, RustOverride> Override { get; set; } = new();

    internal async ValueTask Output(SymbolDb db)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#![allow(unused)]");
        sb.AppendLine("#![allow(non_snake_case)]");
        sb.AppendLine("#![allow(non_camel_case_types)]");

        sb.AppendLine();
        sb.AppendLine("use cocom::{Guid, HResult, Interface, IUnknown, IWeak};");

        GenInterfaces(db, sb);
        GenTypes(db, sb);

        GenInterfacesDetails(db, sb);
        GenInterfacesImpls(db, sb);

        await File.WriteAllTextAsync(Path, sb.ToString());
    }

    private static string ToRustName(TypeSymbol symbol, bool is_root = true, bool super = false)
    {
        var su = super ? "super::" : "";
       switch (symbol.Kind)
        {
            case TypeKind.Interface:
                return $"{su}{symbol.Name}";
            case TypeKind.Generic:
                return $"T{symbol.Index}";
            case TypeKind.Struct:
                if (!symbol.GenericsOrParams.IsDefaultOrEmpty)
                    return $"{su}{symbol.Name}<{string.Join(", ", symbol.GenericsOrParams.Select(a => ToRustName(a, false, super)))}>";
                return $"{su}{symbol.Name}";
            case TypeKind.Enum:
                return $"{su}{symbol.Name}";
            case TypeKind.Ptr:
            case TypeKind.Ref:
            {
                var c = (symbol.Flags & TypeFlags.Const) != 0 ? "const " : "mut ";
                return $"*{c}{ToRustName(symbol.TargetOrReturn!, false, super)}";
            }
            case TypeKind.Fn:
            {
                var arg = symbol.GenericsOrParams.IsDefaultOrEmpty
                    ? ""
                    : $"{string.Join(", ", symbol.GenericsOrParams.Select(a => ToRustName(a, false, super)))}";
                return $"unsafe extern \"C\" fn({arg}) -> {ToRustName(symbol.TargetOrReturn!, false, super)}";
            }
            case TypeKind.Void:
                return is_root ? "()" : "core::ffi::c_void";
            case TypeKind.Bool:
                return "bool";
            case TypeKind.Int8:
                return "i8";
            case TypeKind.Int16:
                return "i16";
            case TypeKind.Int32:
                return "i32";
            case TypeKind.Int64:
                return "i64";
            case TypeKind.Int128:
                return "i128";
            case TypeKind.IntPtr:
                return "isize";
            case TypeKind.UInt8:
                return "u8";
            case TypeKind.UInt16:
                return "u16";
            case TypeKind.UInt32:
                return "u32";
            case TypeKind.UInt64:
                return "u64";
            case TypeKind.UInt128:
                return "u128";
            case TypeKind.UIntPtr:
                return "usize";
            case TypeKind.Float:
                return "f32";
            case TypeKind.Double:
                return "f64";
            case TypeKind.Char8:
                return "char";
            case TypeKind.Char16:
                return "u16";
            case TypeKind.Guid:
                return "Guid";
            case TypeKind.HResult:
                return "HResult";
            case TypeKind.NSpan:
                break;
            case TypeKind.NRoSpan:
                break;
            case TypeKind.Str8:
                break;
            case TypeKind.Str16:
                break;
            case TypeKind.StrAny:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return "ERROR";
    }

    internal void GenTypes(SymbolDb db, StringBuilder root_sb)
    {
        #region Enums

        var enums = db.Enums
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select(a =>
            {
                var sb = new StringBuilder();
                var name = a.Name.Split('.', '+').Last();
                var underlying = ToRustName(a.UnderlyingType);
                sb.AppendLine();
                // todo flags
                var value_cache = new Dictionary<string, string>();
                sb.AppendLine($"#[repr({underlying})]");
                sb.AppendLine($"#[derive(Debug, Clone, Copy, PartialEq, PartialOrd)]");
                sb.AppendLine($"pub enum {name} {{");
                foreach (var item in a.Items)
                {
                    if (value_cache.TryAdd(item.Value, item.Name))
                    {
                        sb.AppendLine($"    {item.Name} = {item.Value},");
                    }
                }
                sb.AppendLine($"}}");
                return sb.ToString();
            }).ToList();
        root_sb.AppendJoin("", enums);

        #endregion

        #region Structs

        var structs = db.Structs
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select(a =>
            {
                var sb = new StringBuilder();
                var name = a.Name.Split('.', '+').Last().Split('`').First();
                var is_union = (a.Flags & StructFlags.Union) != 0;
                sb.AppendLine();
                sb.AppendLine($"#[repr(C)]");
                sb.Append($"#[derive(Clone, Copy");
                if (!is_union)
                {
                    Override.TryGetValue(name, out var ov);
                    if (ov is null || ov.Debug) sb.Append($", Debug");
                    if (ov is null || ov.PartialEq) sb.Append($", PartialEq");
                    if (ov is null || ov.PartialOrd) sb.Append(", PartialOrd");
                }
                sb.AppendLine($")]");
                sb.Append($"pub {(is_union ? "union" : "struct")} {name}");
                if (a.TypeParams.Count > 0)
                {
                    sb.Append($"<");
                    var inc = 0;
                    foreach (var param in a.TypeParams)
                    {
                        var i = inc++;
                        if (i != 0) sb.Append(", ");
                        sb.Append($"T{i} /* {param} */");
                    }
                    sb.Append($">");
                }
                sb.AppendLine($" {{");
                foreach (var field in a.Fields)
                {
                    sb.AppendLine($"    pub {field.Name}: {ToRustName(field.Type)},");
                }
                sb.AppendLine($"}}");
                return sb.ToString();
            }).ToList();
        root_sb.AppendJoin("", structs);

        #endregion
    }

    internal void GenInterfaces(SymbolDb db, StringBuilder root_sb)
    {
        #region Interfaces

        var interfaces = db.Interfaces
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select(a =>
            {
                var sb = new StringBuilder();
                var name = a.Name;
                var parent = a.Parent?.Name ?? "IUnknown";
                sb.AppendLine();
                sb.AppendLine($"#[cocom::interface(\"{a.Guid:D}\")]");
                sb.AppendLine($"pub trait {name} : {parent} {{");
                foreach (var method in a.Methods)
                {
                    sb.Append($"    fn {method.Name}(&{((method.Flags & MethodFlags.Const) != 0 ? "" : "mut ")}self");
                    foreach (var param in method.Params)
                    {
                        sb.Append(", ");
                        var o = (param.Flags & ParamFlags.Out) != 0 ? "/* out */ " : "";
                        sb.Append($"{o}{param.Name}: {ToRustName(param.Type)}");
                    }
                    sb.AppendLine($") -> {ToRustName(method.ReturnType)};");
                }
                sb.AppendLine($"}}");
                return sb.ToString();
            }).ToList();
        root_sb.AppendJoin("", interfaces);

        #endregion
    }

    internal void GenInterfacesDetails(SymbolDb db, StringBuilder root_sb)
    {
        root_sb.AppendLine();
        root_sb.AppendLine("pub mod details {");
        root_sb.AppendLine("    pub use cocom::details::*;");
        root_sb.AppendLine("    use super::*;");

        #region Interfaces

        var interfaces = db.Interfaces
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select(a =>
            {
                var sb = new StringBuilder();
                var name = a.Name;
                var parent = a.Parent?.Name ?? "IUnknown";
                sb.AppendLine();
                sb.AppendLine($"    #[repr(C)]");
                sb.AppendLine($"    #[derive(Debug)]");
                sb.AppendLine($"    pub struct VitualTable_{name} {{");
                sb.AppendLine($"        b: <{parent} as Interface>::VitualTable,");
                sb.AppendLine();
                foreach (var method in a.Methods)
                {
                    sb.Append(
                        $"        pub f_{method.Name}: unsafe extern \"C\" fn(this: *const {name}");
                    foreach (var param in method.Params)
                    {
                        sb.Append(", ");
                        var o = (param.Flags & ParamFlags.Out) != 0 ? "/* out */ " : "";
                        sb.Append($"{o}{param.Name}: {ToRustName(param.Type)}");
                    }
                    sb.AppendLine($") -> {ToRustName(method.ReturnType)},");
                }
                sb.AppendLine($"    }}");
                return sb.ToString();
            }).ToList();
        root_sb.AppendJoin("", interfaces);

        #endregion

        root_sb.AppendLine("}");
    }

    internal void GenInterfacesImpls(SymbolDb db, StringBuilder root_sb)
    {
        root_sb.AppendLine();
        root_sb.AppendLine("pub mod impls {");
        root_sb.AppendLine("    pub use cocom::impls::*;");
        root_sb.AppendLine("    use cocom::{Guid, HResult};");

        #region Interfaces

        var interfaces = db.Interfaces
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select(a =>
            {
                var sb = new StringBuilder();
                var name = a.Name;
                var parent = a.Parent?.Name ?? "IUnknown";
                sb.AppendLine();
                sb.AppendLine($"    pub trait {name} : {parent} {{");
                foreach (var method in a.Methods)
                {
                    sb.Append(
                        $"        fn {method.Name}(&{((method.Flags & MethodFlags.Const) != 0 ? "" : "mut")} self");
                    foreach (var param in method.Params)
                    {
                        sb.Append(", ");
                        var o = (param.Flags & ParamFlags.Out) != 0 ? "/* out */ " : "";
                        sb.Append($"{o}{param.Name}: {ToRustName(param.Type, super: true)}");
                    }
                    sb.AppendLine($") -> {ToRustName(method.ReturnType, super: true)};");
                }
                sb.AppendLine($"    }}");
                return sb.ToString();
            }).ToList();
        root_sb.AppendJoin("", interfaces);

        #endregion

        root_sb.AppendLine("}");
    }
}
