using System.Text;
using System.Text.Json.Serialization;
using Coplt.Com2.DefineModel;
using Coplt.Com2.Symbols;
using EnumFlags = Coplt.Com2.Symbols.EnumFlags;
using MethodFlags = Coplt.Com2.Symbols.MethodFlags;
using StructFlags = Coplt.Com2.Symbols.StructFlags;
using TypeFlags = Coplt.Com2.Symbols.TypeFlags;
using TypeKind = Coplt.Com2.Symbols.TypeKind;

namespace Coplt.Com2;

public record RustOverride
{
    [JsonPropertyName("Debug")]
    public bool Debug { get; set; } = true;
    [JsonPropertyName("Copy")]
    public bool Copy { get; set; } = true;
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
        sb.AppendLine("#![allow(non_upper_case_globals)]");

        sb.AppendLine();
        sb.AppendLine("use cocom::{Guid, HResult, HResultE, Interface, IUnknown, IWeak, ComPtr};");

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
                return $"unsafe extern \"C\" fn({arg}) -> {ToRustName(symbol.TargetOrReturn!, true, super)}";
            }
            case TypeKind.ComPtr:
            {
                return $"ComPtr<{ToRustName(symbol.TargetOrReturn!, false, super)}>";
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
                if ((a.Flags & EnumFlags.Flags) != 0)
                {
                    var value_cache = new Dictionary<string, string>();
                    sb.AppendLine($"bitflags::bitflags! {{");
                    sb.AppendLine($"    #[repr(transparent)]");
                    sb.AppendLine($"    #[derive(Debug, Clone, Copy, PartialEq, PartialOrd)]");
                    sb.AppendLine($"    pub struct {name} : {underlying} {{");
                    foreach (var item in a.Items)
                    {
                        if (value_cache.TryAdd(item.Value, item.Name))
                        {
                            sb.AppendLine($"        const {item.Name} = {item.Value};");
                        }
                    }
                    sb.AppendLine($"        const _ = !0;");
                    sb.AppendLine($"    }}");
                    sb.AppendLine($"}}");
                    sb.AppendLine();
                    sb.AppendLine($"impl {name} {{");
                    sb.AppendLine($"    #[inline(always)]");
                    sb.AppendLine($"    pub const fn has_flags(self, value: {name}) -> bool {{");
                    sb.AppendLine($"        (self.bits() & value.bits()) == value.bits()");
                    sb.AppendLine($"    }}");
                    sb.AppendLine($"    #[inline(always)]");
                    sb.AppendLine($"    pub const fn has_any_flags(self, value: {name}) -> bool {{");
                    sb.AppendLine($"        (self.bits() & value.bits()) != 0");
                    sb.AppendLine($"    }}");
                    sb.AppendLine($"    #[inline(always)]");
                    sb.AppendLine($"    pub const fn has_flags_only(self, value: {name}) -> bool {{");
                    sb.AppendLine($"        (self.bits() & !(value.bits())) == 0");
                    sb.AppendLine($"    }}");
                    sb.AppendLine($"}}");
                    sb.AppendLine();
                    sb.AppendLine($"impl From<{underlying}> for {name} {{");
                    sb.AppendLine($"    fn from(value: {underlying}) -> Self {{");
                    sb.AppendLine($"        Self::from_bits_retain(value)");
                    sb.AppendLine($"    }}");
                    sb.AppendLine($"}}");
                    sb.AppendLine();
                    sb.AppendLine($"impl From<{name}> for {underlying} {{");
                    sb.AppendLine($"    fn from(value: {name}) -> Self {{");
                    sb.AppendLine($"        value.bits()");
                    sb.AppendLine($"    }}");
                    sb.AppendLine($"}}");
                }
                else
                {
                    var value_cache = new Dictionary<string, string>();
                    sb.AppendLine($"#[repr(transparent)]");
                    sb.AppendLine($"#[derive(Debug, Clone, Copy, PartialEq, PartialOrd)]");
                    sb.AppendLine($"pub struct {name}(pub {underlying});");
                    sb.AppendLine();
                    sb.AppendLine($"impl {name} {{");
                    foreach (var item in a.Items)
                    {
                        if (value_cache.TryAdd(item.Value, item.Name))
                        {
                            sb.AppendLine($"    pub const {item.Name}: Self = Self({item.Value});");
                        }
                    }
                    sb.AppendLine($"}}");
                    sb.AppendLine();
                    sb.AppendLine($"impl core::fmt::Display for {name} {{");
                    sb.AppendLine($"    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {{");
                    sb.AppendLine($"        match *self {{");
                    value_cache.Clear();
                    foreach (var item in a.Items)
                    {
                        if (value_cache.TryAdd(item.Value, item.Name))
                        {
                            sb.AppendLine($"            Self::{item.Name} => f.write_str(\"{item.Name}\"),");
                        }
                    }
                    sb.AppendLine($"            Self(v) => f.write_fmt(format_args!(\"{{v}}\")),");
                    sb.AppendLine($"        }}");
                    sb.AppendLine($"    }}");
                    sb.AppendLine($"}}");
                }

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
                var name = a.Name;
                var is_union = (a.Flags & StructFlags.Union) != 0;
                sb.AppendLine();
                var field_has_type_param = false;
                foreach (var field in a.Fields)
                {
                    if (a.TypeParams.Count > 0 && field.Type.HasTypeParam) field_has_type_param = true;
                }
                var can_derive = field_has_type_param || a.TypeParams.Count == 0 || a.TypeParams.All(static a => a.Phantom is not Phantom.Ptr);
                var align = a.Align > 0 ? $", align({a.Align})" : "";
                sb.AppendLine($"#[repr(C{align})]");
                if (can_derive)
                {
                    sb.Append($"#[derive(Clone");
                    if (is_union)
                    {
                        sb.Append($", Copy");
                    }
                    else
                    {
                        Override.TryGetValue(name, out var ov);
                        if (ov is null || ov.Copy) sb.Append($", Copy");
                        if (ov is null || ov.Debug) sb.Append($", Debug");
                        if (ov is null || ov.PartialEq) sb.Append($", PartialEq");
                        if (ov is null || ov.PartialOrd) sb.Append(", PartialOrd");
                    }
                    sb.AppendLine($")]");
                }
                else
                {
                    if (!is_union)
                    {
                        Override.TryGetValue(name, out var ov);
                        if (ov is null || ov.Debug) sb.AppendLine($"#[derive(Debug)]");
                    }
                }
                sb.Append($"pub {(is_union ? "union" : "struct")} {name}");
                if (a.TypeParams.Count > 0)
                {
                    sb.Append($"<");
                    var inc = 0;
                    foreach (var param in a.TypeParams)
                    {
                        var i = inc++;
                        if (i != 0) sb.Append(", ");
                        sb.Append($"T{i} /* {param.Name} */");
                    }
                    sb.Append($">");
                }
                sb.AppendLine($" {{");
                foreach (var field in a.Fields)
                {
                    sb.AppendLine($"    pub {field.Name}: {ToRustName(field.Type)},");
                }
                if (a.TypeParams.Count > 0 && !field_has_type_param)
                {
                    sb.Append($"    pub _p: core::marker::PhantomData<(");
                    var inc = 0;
                    foreach (var param in a.TypeParams)
                    {
                        var i = inc++;
                        sb.Append($"*mut T{i} /* {param.Name} */,");
                    }
                    sb.AppendLine($")>,");
                }
                sb.AppendLine($"}}");
                if (is_union)
                {
                    sb.AppendLine();
                    sb.AppendLine($"impl core::fmt::Debug for {name} {{");
                    sb.AppendLine($"    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {{");
                    sb.AppendLine($"        f.debug_struct(\"{name}\")");
                    sb.AppendLine($"            .finish_non_exhaustive()");
                    sb.AppendLine($"    }}");
                    sb.AppendLine($"}}");
                }
                if (!can_derive)
                {
                    Override.TryGetValue(name, out var ov);
                    var copy = ov is null || ov.Copy;

                    if (copy)
                    {
                        string GenImplGeneric()
                        {
                            var sb = new StringBuilder();
                            sb.Append($"<");
                            var inc = 0;
                            foreach (var param in a.TypeParams)
                            {
                                var i = inc++;
                                if (i != 0) sb.Append(", ");
                                sb.Append($"T{i} /* {param.Name} */");
                            }
                            sb.Append($">");
                            return sb.ToString();
                        }

                        var generic = GenImplGeneric();

                        sb.AppendLine();
                        sb.AppendLine($"impl{generic} core::marker::Copy for {name}{generic} {{}}");
                        sb.AppendLine($"impl{generic} core::clone::Clone for {name}{generic} {{");
                        sb.AppendLine($"    fn clone(&self) -> Self {{");
                        sb.AppendLine($"        *self");
                        sb.AppendLine($"    }}");
                        sb.AppendLine($"}}");

                        sb.AppendLine();
                        sb.AppendLine($"impl{generic} core::cmp::PartialEq for {name}{generic} {{");
                        sb.AppendLine($"    fn eq(&self, other: &Self) -> bool {{");
                        var first = true;
                        foreach (var field in a.Fields)
                        {
                            sb.Append($"        ");
                            if (first) first = false;
                            else sb.Append(" && ");
                            sb.Append($"self.{field.Name} == other.{field.Name}");
                            sb.AppendLine();
                        }
                        sb.AppendLine($"    }}");
                        sb.AppendLine($"}}");

                        sb.AppendLine();
                        sb.AppendLine($"impl{generic} core::cmp::PartialOrd for {name}{generic} {{");
                        sb.AppendLine($"    fn partial_cmp(&self, other: &Self) -> Option<core::cmp::Ordering> {{");
                        foreach (var field in a.Fields)
                        {
                            sb.AppendLine($"        match self.{field.Name}.partial_cmp(&other.{field.Name})? {{ core::cmp::Ordering::Equal => (), ord => return Some(ord) }}");
                            sb.AppendLine($"        Some(core::cmp::Ordering::Equal)");
                        }
                        sb.AppendLine($"    }}");
                        sb.AppendLine($"}}");
                    }
                }
                return sb.ToString();
            }).ToList();
        root_sb.AppendJoin("", structs);

        #endregion
    }

    internal string BuildParentList(InterfaceDeclareSymbol a)
    {
        if (a.Parent == null || a.Parent.Name == "IUnknown") return "IUnknown";
        return $"{a.Parent.Name} + {BuildParentList(a.Parent)}";
    }

    internal bool IsWeak(InterfaceDeclareSymbol a)
    {
        while (true)
        {
            if (a.Parent == null) return false;
            if (a.Parent.Name == "IWeak") return true;
            a = a.Parent;
        }
    }

    internal void GenInterfaces(SymbolDb db, StringBuilder root_sb)
    {
        #region Interfaces

        var interfaces = db.Interfaces
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Where(a => a.Export)
            .Select(a =>
            {
                var sb = new StringBuilder();
                var name = a.Name;
                var parent = BuildParentList(a);
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
        root_sb.AppendLine();
        root_sb.AppendLine("    struct VT<T, V, O>(core::marker::PhantomData<(T, V, O)>);");

        #region Interfaces

        var interfaces = db.Interfaces
            .AsParallel()
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .Select(a => a.Value)
            .Where(a => a.Export)
            .Select(a =>
            {
                var sb = new StringBuilder();
                var name = a.Name;
                var is_weak = IsWeak(a);
                var ObjectBoxWeak = is_weak ? " + impls::ObjectBoxWeak" : "";
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
                sb.AppendLine();
                sb.AppendLine($"    impl<T: impls::{name} + impls::Object, O: impls::ObjectBox<Object = T>{ObjectBoxWeak}> VT<T, {name}, O>");
                sb.AppendLine($"    where");
                sb.AppendLine($"        T::Interface: details::QuIn<T::Interface, T, O>,");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        pub const VTBL: VitualTable_{name} = VitualTable_{name} {{");
                sb.AppendLine($"            b: <{parent} as Vtbl<O>>::VTBL,");
                foreach (var method in a.Methods)
                {
                    sb.AppendLine($"            f_{method.Name}: Self::f_{method.Name},");
                }
                sb.AppendLine($"        }};");
                sb.AppendLine();
                foreach (var method in a.Methods)
                {
                    sb.Append(
                        $"        unsafe extern \"C\" fn f_{method.Name}(this: *const {name}");
                    foreach (var param in method.Params)
                    {
                        sb.Append(", ");
                        var o = (param.Flags & ParamFlags.Out) != 0 ? "/* out */ " : "";
                        sb.Append($"{o}{param.Name}: {ToRustName(param.Type)}");
                    }
                    sb.AppendLine($") -> {ToRustName(method.ReturnType)} {{");
                    sb.Append($"            unsafe {{ (*O::GetObject(this as _)).{method.Name}(");
                    var first = true;
                    foreach (var param in method.Params)
                    {
                        if (first) first = false;
                        else sb.Append(", ");
                        sb.Append($"{param.Name}");
                    }
                    sb.AppendLine($") }}");
                    sb.AppendLine($"        }}");
                }
                sb.AppendLine($"    }}");
                sb.AppendLine();
                sb.AppendLine($"    impl<T: impls::{name} + impls::Object, O: impls::ObjectBox<Object = T>{ObjectBoxWeak}> Vtbl<O> for {name}");
                sb.AppendLine($"    where");
                sb.AppendLine($"        T::Interface: details::QuIn<T::Interface, T, O>,");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        const VTBL: <{name} as Interface>::VitualTable = VT::<T, {name}, O>::VTBL;");
                sb.AppendLine();
                sb.AppendLine($"        fn vtbl() -> &'static Self::VitualTable {{");
                sb.AppendLine($"            &<Self as Vtbl<O>>::VTBL");
                sb.AppendLine($"        }}");
                sb.AppendLine($"    }}");
                sb.AppendLine();
                sb.AppendLine($"    impl<T: impls::{name} + impls::Object, O: impls::ObjectBox<Object = T>> QuIn<{name}, T, O> for {name} {{");
                sb.AppendLine($"        #[inline(always)]");
                sb.AppendLine($"        unsafe fn QueryInterface(");
                sb.AppendLine($"            this: *mut T,");
                sb.AppendLine($"            guid: Guid,");
                sb.AppendLine($"            out: *mut *mut core::ffi::c_void,");
                sb.AppendLine($"        ) -> HResult {{");
                sb.AppendLine($"            unsafe {{");
                sb.AppendLine($"                static GUID: Guid = {name}::GUID;");
                sb.AppendLine($"                if guid == GUID {{");
                sb.AppendLine($"                    *out = this as _;");
                sb.AppendLine($"                    O::AddRef(this as _);");
                sb.AppendLine($"                    return HResultE::Ok.into();");
                sb.AppendLine($"                }}");
                sb.AppendLine($"                <{parent} as QuIn<{parent}, T, O>>::QueryInterface(this, guid, out)");
                sb.AppendLine($"            }}");
                sb.AppendLine($"        }}");
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
            .Where(a => a.Export)
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
