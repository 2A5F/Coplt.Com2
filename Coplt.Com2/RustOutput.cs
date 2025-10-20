using System.Text;
using Coplt.Com2.Symbols;

namespace Coplt.Com2;

public record RustOutput : AOutput
{
    internal async ValueTask Output(SymbolDb db)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#![allow(unused)]");
        sb.AppendLine("#![allow(non_snake_case)]");
        
        GenTypes(db, sb);
        GenInterfaces(db, sb);

        await File.WriteAllTextAsync(Path, sb.ToString());
    }

    internal static string ToRustName(TypeSymbol symbol)
    {
        switch (symbol.Kind)
        {
            case TypeKind.Interface:
                return symbol.Name;
            case TypeKind.Generic:
                return $"T{symbol.Index}";
            case TypeKind.Struct:
                if (!symbol.GenericsOrParams.IsDefaultOrEmpty)
                    return $"{symbol.Name}<{string.Join(", ", symbol.GenericsOrParams.Select(ToRustName))}>";
                return $"{symbol.Name}";
            case TypeKind.Enum:
                return $"{symbol.Name}";
            case TypeKind.Ptr:
            case TypeKind.Ref:
            {
                var c = (symbol.Flags & TypeFlags.Const) != 0 ? "const " : "mut ";
                return $"*{c}{ToRustName(symbol.TargetOrReturn!)}";
            }
            case TypeKind.Fn:
            {
                var arg = symbol.GenericsOrParams.IsDefaultOrEmpty ? "" : $", {string.Join(", ", symbol.GenericsOrParams.Select(ToRustName))}";
                return $"fn({arg}) -> {ToRustName(symbol.TargetOrReturn!)}";
            }
            case TypeKind.Void:
                return "()";
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
                return "::cocom::Guid";
            case TypeKind.HResult:
                return "::cocom::HResult";
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
                sb.AppendLine($"#[repr({underlying})]");
                sb.AppendLine($"#[derive(Debug, Clone, Copy, PartialEq, PartialOrd)]");
                sb.AppendLine($"pub enum {name} {{");
                foreach (var item in a.Items)
                {
                    sb.AppendLine($"    {item.Name} = {item.Value},");
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
                sb.AppendLine();
                sb.AppendLine($"#[repr(C)]");
                sb.AppendLine($"#[derive(Debug, Clone, Copy, PartialEq, PartialOrd)]");
                sb.Append($"pub {((a.Flags & StructFlags.Union) != 0 ? "union" : "struct")} {name}");
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
                    sb.AppendLine($">");
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
                sb.AppendLine($"#[repr(C)]");
                sb.AppendLine($"pub struct {name} {{");
                sb.AppendLine($"}}");
                sb.AppendLine();
                sb.AppendLine($"impl {name} {{");
                sb.AppendLine($"}}");
                // sb.AppendLine($"COPLT_COM_INTERFACE({name}, \"{a.Guid:D}\", {parent})");
                // sb.AppendLine($"{{");
                // sb.AppendLine($"    COPLT_COM_INTERFACE_BODY_{Namespace}_{name}");
                // if (a.Methods.Count > 0) sb.AppendLine();
                // foreach (var method in a.Methods)
                // {
                //     sb.Append($"    COPLT_COM_METHOD({method.Name}, {ToCppName(method.ReturnType, ns_pre)}, (");
                //     var first = true;
                //     foreach (var param in method.Params)
                //     {
                //         if (first) first = false;
                //         else sb.Append(", ");
                //         var o = (param.Flags & ParamFlags.Out) != 0 ? "COPLT_OUT " : "";
                //         sb.Append($"{o}{ToCppName(param.Type, ns_pre)} {param.Name}");
                //     }
                //     sb.Append($"){((method.Flags & MethodFlags.Const) != 0 ? " const" : "")}");
                //     foreach (var param in method.Params)
                //     {
                //         sb.Append($", {param.Name}");
                //     }
                //     sb.AppendLine($");");
                // }
                // sb.AppendLine($"}};");
                return sb.ToString();
            }).ToList();
        root_sb.AppendJoin("", interfaces);

        #endregion
    }
}
