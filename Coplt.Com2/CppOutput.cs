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
        await Task.WhenAll(GenTypes(db), GenDetails(db), GenInterfaces(db));
    }

    internal static string ToCppName(TypeSymbol symbol, string ns)
    {
        switch (symbol.Kind)
        {
            case TypeKind.Interface:
                return symbol.Name;
            case TypeKind.Generic:
                return $"T{symbol.Index}";
            case TypeKind.Struct:
                if (!symbol.GenericsOrParams.IsDefaultOrEmpty)
                    return $"{ns}{symbol.Name}<{string.Join(", ", symbol.GenericsOrParams.Select(a => ToCppName(a, ns)))}>";
                return $"{ns}{symbol.Name}";
            case TypeKind.Enum:
                return $"{ns}{symbol.Name}";
            case TypeKind.Ptr:
            {
                var c = (symbol.Flags & TypeFlags.Const) != 0 ? "const" : "";
                return $"{c}{ToCppName(symbol.TargetOrReturn!, ns)}*";
            }
            case TypeKind.Fn:
            {
                var c = (symbol.Flags & TypeFlags.Const) != 0 ? "const" : "";
                var arg = symbol.GenericsOrParams.IsDefaultOrEmpty ? "" : $", {string.Join(", ", symbol.GenericsOrParams.Select(a => ToCppName(a, ns)))}";
                return $"{c}::Coplt::Func<{ToCppName(symbol.TargetOrReturn!, ns)}{arg}>*";
            }
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

    internal async Task GenTypes(SymbolDb db)
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
        var ns_pre = Namespace is null ? "" : $"::{Namespace}::";

        #region Enums

        var enums = db.Enums
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select(a =>
            {
                var sb = new StringBuilder();
                var name = a.Name.Split('.', '+').Last();
                var underlying = ToCppName(a.UnderlyingType, ns_pre);
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
                    sb.AppendLine($"{space}    {ToCppName(field.Type, ns_pre)} {field.Name};");
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

    internal async Task GenDetails(SymbolDb db)
    {
        var root_sb = new StringBuilder();
        root_sb.AppendLine("#pragma once");
        root_sb.AppendLine($"#ifndef {ProjName}_DETAILS_H");
        root_sb.AppendLine($"#define {ProjName}_DETAILS_H");
        root_sb.AppendLine();
        root_sb.AppendLine($"#include \"{CoComPath}\"");
        root_sb.AppendLine($"#include \"./Types.h\"");
        root_sb.AppendLine();
        if (Namespace is not null) root_sb.AppendLine($"namespace {Namespace} {{");

        var space = Namespace is null ? "" : "    ";

        root_sb.AppendLine();
        root_sb.AppendLine($"{space}using IUnknown = ::Coplt::IUnknown;");
        root_sb.AppendLine($"{space}using IWeak = ::Coplt::IWeak;");

        #region Pre Defines

        var pre_defines = db.Interfaces
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select(a =>
            {
                var sb = new StringBuilder();
                var name = a.Name;
                sb.AppendLine($"{space}struct {name};");
                return sb.ToString();
            }).ToList();
        root_sb.AppendLine();
        root_sb.AppendJoin("", pre_defines);

        #endregion

        if (Namespace is not null) root_sb.AppendLine($"\n}} // namespace {Namespace}");

        var ns_pre = Namespace is null ? "" : $"::{Namespace}::";

        #region Details

        var details = db.Interfaces
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Select(a =>
            {
                var sb = new StringBuilder();
                var name = a.Name;
                var parent = a.Parent?.Name ?? "IUnknown";
                sb.AppendLine();

                sb.AppendLine($"template <>");
                sb.AppendLine($"struct ::Coplt::Internal::VirtualTable<{ns_pre}{name}>");
                sb.AppendLine($"{{");
                sb.AppendLine($"    VirtualTable<{ns_pre}{parent}> b;");
                foreach (var method in a.Methods)
                {
                    sb.Append($"    {ToCppName(method.ReturnType, ns_pre)} (*const f_{method.Name})(");
                    if ((method.Flags & MethodFlags.Const) != 0) sb.Append($"const ");
                    sb.Append($"{ns_pre}{name}*");
                    foreach (var param in method.Params)
                    {
                        var o = (param.Flags & ParamFlags.Out) != 0 ? "COPLT_OUT " : "";
                        sb.Append($", {o}{ToCppName(param.Type, ns_pre)} {param.Name}");
                    }
                    sb.AppendLine($");");
                }
                sb.AppendLine($"}};");

                sb.AppendLine();
                sb.AppendLine($"template <>");
                sb.AppendLine($"struct ::Coplt::Internal::ComProxy<{ns_pre}{name}>");
                sb.AppendLine($"{{");
                sb.AppendLine($"    using VirtualTable = VirtualTable<{ns_pre}{name}>;");

                sb.AppendLine();
                sb.AppendLine($"    static COPLT_FORCE_INLINE constexpr inline const ::Coplt::Guid& get_Guid()");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        static ::Coplt::Guid s_guid(\"{a.Guid:D}\");");
                sb.AppendLine($"        return s_guid;");
                sb.AppendLine($"    }}");

                sb.AppendLine();
                sb.AppendLine($"    template <class Self>");
                sb.AppendLine($"    COPLT_FORCE_INLINE");
                sb.AppendLine($"    static HResult QueryInterface(const Self* self, const ::Coplt::Guid& guid, COPLT_OUT void*& object)");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        if (guid == guid_of<{ns_pre}{name}>())");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            object = const_cast<void*>(static_cast<const void*>(static_cast<const {ns_pre}{name}*>(self)));");
                sb.AppendLine($"            return ::Coplt::HResultE::Ok;");
                sb.AppendLine($"        }}");
                sb.AppendLine($"        return ComProxy<{ns_pre}{parent}>::QueryInterface(self, guid, object);");
                sb.AppendLine($"    }}");

                sb.AppendLine();
                sb.AppendLine($"    template <std::derived_from<{ns_pre}{name}> Base = {ns_pre}{name}>");
                sb.AppendLine($"    struct Proxy : ComProxy<{ns_pre}{parent}>::Proxy<Base>");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        using Super = ComProxy<{ns_pre}{parent}>::Proxy<Base>;");
                sb.AppendLine($"        using Self = Proxy;");
                sb.AppendLine();
                sb.AppendLine($"    protected:");
                sb.AppendLine($"        virtual ~Proxy() = default;");
                sb.AppendLine();
                sb.AppendLine($"        COPLT_FORCE_INLINE");
                sb.AppendLine($"        static const VirtualTable& GetVtb()");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            static VirtualTable vtb");
                sb.AppendLine($"            {{");
                sb.AppendLine($"                .b = Super::GetVtb(),");
                foreach (var method in a.Methods)
                {
                    sb.Append($"                .f_{method.Name} = [](");
                    if ((method.Flags & MethodFlags.Const) != 0) sb.Append($"const ");
                    sb.Append($"{ns_pre}{name}* self");
                    var inc = 0;
                    foreach (var param in method.Params)
                    {
                        var i = inc++;
                        var o = (param.Flags & ParamFlags.Out) != 0 ? "COPLT_OUT " : "";
                        sb.Append($", {o}{ToCppName(param.Type, ns_pre)} p{i}");
                    }
                    sb.AppendLine($")");
                    sb.AppendLine($"                {{");
                    sb.AppendLine($"                    #ifdef COPLT_COM_BEFORE_VIRTUAL_CALL");
                    sb.AppendLine($"                    COPLT_COM_BEFORE_VIRTUAL_CALL({ns_pre}{name}, {method.Name})");
                    sb.AppendLine($"                    #endif");
                    sb.Append($"                    return static_cast<");
                    if ((method.Flags & MethodFlags.Const) != 0) sb.Append($"const ");
                    sb.Append($"Self*>(self)->Impl_{method.Name}(");
                    inc = 0;
                    foreach (var _ in method.Params)
                    {
                        var i = inc++;
                        if (i != 0) sb.Append($", ");
                        sb.Append($"p{i}");
                    }
                    sb.AppendLine($");");
                    sb.AppendLine($"                    #ifdef COPLT_COM_AFTER_VIRTUAL_CALL");
                    sb.AppendLine($"                    COPLT_COM_AFTER_VIRTUAL_CALL({ns_pre}{name}, {method.Name})");
                    sb.AppendLine($"                    #endif");
                    sb.AppendLine($"                }},");
                }
                sb.AppendLine($"            }};");
                sb.AppendLine($"            return vtb;");
                sb.AppendLine($"        }};");

                sb.AppendLine();
                sb.AppendLine($"        explicit Proxy(const ::Coplt::Internal::VirtualTable<Base>* vtb) : Base(vtb) {{}}");
                sb.AppendLine();
                sb.AppendLine($"        explicit Proxy() : Super(&GetVtb()) {{}}");

                if (a.Methods.Count > 0) sb.AppendLine();
                foreach (var method in a.Methods)
                {
                    sb.Append($"        virtual {ToCppName(method.ReturnType, ns_pre)} Impl_{method.Name}(");
                    var first = true;
                    foreach (var param in method.Params)
                    {
                        if (first) first = false;
                        else sb.Append(", ");
                        var o = (param.Flags & ParamFlags.Out) != 0 ? "COPLT_OUT " : "";
                        sb.Append($"{o}{ToCppName(param.Type, ns_pre)} {param.Name}");
                    }
                    sb.AppendLine($"){((method.Flags & MethodFlags.Const) != 0 ? " const" : "")} = 0;");
                }

                sb.AppendLine($"    }};");

                sb.AppendLine($"}};");

                sb.AppendLine();
                sb.AppendLine($"#define COPLT_COM_INTERFACE_BODY_{Namespace}_{name}\\");
                sb.AppendLine($"    using Super = {ns_pre}{parent};\\");
                sb.AppendLine($"    using Self = {ns_pre}{name};\\");
                sb.AppendLine($"\\");
                sb.AppendLine($"    explicit {name}(const ::Coplt::Internal::VirtualTable<Self>* vtbl) : Super(&vtbl->b) {{}}");

                return sb.ToString();
            }).ToList();
        root_sb.AppendJoin("", details);

        #endregion

        root_sb.AppendLine();
        root_sb.AppendLine($"#endif //{ProjName}_DETAILS_H");
        await File.WriteAllTextAsync($"{Path}/Details.h", root_sb.ToString(), Encoding.UTF8);
    }

    internal async Task GenInterfaces(SymbolDb db)
    {
        var root_sb = new StringBuilder();
        root_sb.AppendLine("#pragma once");
        root_sb.AppendLine($"#ifndef {ProjName}_INTERFACE_H");
        root_sb.AppendLine($"#define {ProjName}_INTERFACE_H");
        root_sb.AppendLine();
        root_sb.AppendLine($"#include \"{CoComPath}\"");
        root_sb.AppendLine($"#include \"./Types.h\"");
        root_sb.AppendLine($"#include \"./Details.h\"");
        root_sb.AppendLine();
        if (Namespace is not null) root_sb.AppendLine($"namespace {Namespace} {{");

        var space = Namespace is null ? "" : "    ";

        var ns_pre = Namespace is null ? "" : $"::{Namespace}::";

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
                sb.AppendLine($"{space}COPLT_COM_INTERFACE({name}, \"{a.Guid:D}\", {ns_pre}{parent})");
                sb.AppendLine($"{space}{{");
                sb.AppendLine($"{space}    COPLT_COM_INTERFACE_BODY_{Namespace}_{name}");
                if (a.Methods.Count > 0) sb.AppendLine();
                foreach (var method in a.Methods)
                {
                    sb.Append($"{space}    COPLT_COM_METHOD({method.Name}, {ToCppName(method.ReturnType, ns_pre)}, (");
                    var first = true;
                    foreach (var param in method.Params)
                    {
                        if (first) first = false;
                        else sb.Append(", ");
                        var o = (param.Flags & ParamFlags.Out) != 0 ? "COPLT_OUT " : "";
                        sb.Append($"{o}{ToCppName(param.Type, ns_pre)} {param.Name}");
                    }
                    sb.Append($"){((method.Flags & MethodFlags.Const) != 0 ? " const" : "")}");
                    foreach (var param in method.Params)
                    {
                        sb.Append($", {param.Name}");
                    }
                    sb.AppendLine($")");
                }
                sb.AppendLine($"{space}}};");
                return sb.ToString();
            }).ToList();
        root_sb.AppendJoin("", interfaces);

        #endregion


        if (Namespace is not null) root_sb.AppendLine($"\n}} // namespace {Namespace}");
        root_sb.AppendLine();
        root_sb.AppendLine($"#endif //{ProjName}_INTERFACE_H");
        await File.WriteAllTextAsync($"{Path}/Interface.h", root_sb.ToString(), Encoding.UTF8);
    }
}
