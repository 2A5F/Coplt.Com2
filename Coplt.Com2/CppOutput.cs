using System.Text;
using Coplt.Com2.Symbols;

namespace Coplt.Com2;

public record CppOutput : AOutput
{
    public string CoComPath { get; set; } = "CoCom.h";
    public string ProjName { get; set; } = "ProjectName";
    public string? Namespace { get; set; }

    internal async ValueTask Output(SymbolDb db)
    {
        Directory.CreateDirectory(Path);
        await GenTypes(db);
    }

    internal static string ToCppName(TypeSymbol symbol)
    {
        switch (symbol.Kind)
        {
            case TypeKind.Generic:
                return $"T{symbol.Index}";
            case TypeKind.Struct:
            case TypeKind.Enum:
                return symbol.Name;
            case TypeKind.Ptr:
                return $"{((symbol.Flags & TypeFlags.Const) != 0 ? "const" : "")}{ToCppName(symbol.TargetOrReturn!)}*";
            case TypeKind.Fn:
                break;
            case TypeKind.Void:
                return "void";
            case TypeKind.Bool:
                return "bool";
            case TypeKind.Int8:
                return "::Coplt::i8";
            case TypeKind.Int16:
                return "::Coplt::i16";
            case TypeKind.Int32:
                return "::Coplt::i32";
            case TypeKind.Int64:
                return "::Coplt::i64";
            case TypeKind.Int128:
                return "::Coplt::i128";
            case TypeKind.IntPtr:
                return "::Coplt::isize";
            case TypeKind.UInt8:
                return "::Coplt::u8";
            case TypeKind.UInt16:
                return "::Coplt::u16";
            case TypeKind.UInt32:
                return "::Coplt::u32";
            case TypeKind.UInt64:
                return "::Coplt::u64";
            case TypeKind.UInt128:
                return "::Coplt::u128";
            case TypeKind.UIntPtr:
                return "::Coplt::usize";
            case TypeKind.Float:
                return "::Coplt::float";
            case TypeKind.Double:
                return "::Coplt::double";
            case TypeKind.Char8:
                return "::Coplt::char8";
            case TypeKind.Char16:
                return "::Coplt::char16";
            case TypeKind.Guid:
                return "::Coplt::Guid";
            case TypeKind.HResult:
                return "::Coplt::HResult";
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

    internal async ValueTask GenTypes(SymbolDb db)
    {
        var root_sb = new StringBuilder();
        root_sb.AppendLine("#pragma once");
        root_sb.AppendLine($"#ifndef {ProjName}_TYPES_H");
        root_sb.AppendLine($"#define {ProjName}_TYPES_H");
        root_sb.AppendLine();
        root_sb.AppendLine($"#include \"{CoComPath}\"");
        root_sb.AppendLine();
        if (Namespace is not null) root_sb.AppendLine($"namespace {Namespace} {{");

        var space = Namespace is null ? "" : "    ";

        #region Enums

        var enums = db.Enums
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select(a =>
            {
                var sb = new StringBuilder();
                var name = a.Name.Split('.', '+').Last();
                var underlying = ToCppName(a.UnderlyingType);
                sb.AppendLine();
                sb.AppendLine($"{space}enum class {name} : {underlying}");
                sb.AppendLine($"{space}{{");
                foreach (var item in a.Items)
                {
                    sb.AppendLine($"{space}    {item.Name} = {item.Value},");
                }
                sb.AppendLine($"{space}}};");
                return sb.ToString();
            }).ToList();
        root_sb.AppendJoin("", enums);

        #endregion

        #region Structs pre define

        var structs_pre_define = db.Structs
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select(a =>
            {
                var sb = new StringBuilder();
                var name = a.Name.Split('.', '+').Last().Split('`').First();
                sb.AppendLine();
                if (a.TypeParams.Count > 0)
                {
                    sb.Append($"{space}template <");
                    var inc = 0;
                    foreach (var param in a.TypeParams)
                    {
                        var i = inc++;
                        if (i != 0) sb.Append(", ");
                        sb.Append($"class T{i} /* {param} */");
                    }
                    sb.AppendLine($">");
                }
                sb.AppendLine($"{space}{((a.Flags & StructFlags.Union) != 0 ? "union" : "struct")} {name};");
                return sb.ToString();
            }).ToList();
        root_sb.AppendJoin("", structs_pre_define);

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
                if (a.TypeParams.Count > 0)
                {
                    sb.Append($"{space}template <");
                    var inc = 0;
                    foreach (var param in a.TypeParams)
                    {
                        var i = inc++;
                        if (i != 0) sb.Append(", ");
                        sb.Append($"class T{i} /* {param} */");
                    }
                    sb.AppendLine($">");
                }
                sb.AppendLine($"{space}{((a.Flags & StructFlags.Union) != 0 ? "union" : "struct")} {name}");
                sb.AppendLine($"{space}{{");
                foreach (var field in a.Fields)
                {
                    sb.AppendLine($"{space}    {ToCppName(field.Type)} {field.Name};");
                }
                sb.AppendLine($"{space}}};");
                return sb.ToString();
            }).ToList();
        root_sb.AppendJoin("", structs);

        #endregion

        if (Namespace is not null) root_sb.AppendLine($"\n}} // namespace {Namespace}");
        root_sb.AppendLine();
        root_sb.AppendLine($"#endif //{ProjName}_TYPES_H");
        await File.WriteAllTextAsync($"{Path}/Types.h", root_sb.ToString(), Encoding.UTF8);
    }
}
