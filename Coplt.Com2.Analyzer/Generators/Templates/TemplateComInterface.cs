using Coplt.Analyzers.Generators.Templates;
using Coplt.Analyzers.Utilities;
using Microsoft.CodeAnalysis;

namespace Coplt.Com2.Analyzer.Generators.Templates;

public class TemplateComInterface(InterfaceGenerator.Varying varying) : ATemplate(varying.GenBase)
{
    private string DataClassName = $"__Data__{varying.GenBase.FileFullName.Replace(".", "_")}";
    private const string Unsafe = "global::System.Runtime.CompilerServices.Unsafe";
    private const string ComUtils = "global::Coplt.Com.ComUtils";
    private const string IComInterface = "global::Coplt.Com.IComInterface";
    private const string InterfaceMember = "global::Coplt.Com.InterfaceMemberAttribute";
    private const string AggressiveInlining = "global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)";

    protected override void DoGenAfterUsing()
    {
        sb.AppendLine($"#pragma warning disable CS8826");
        sb.AppendLine();
        sb.AppendLine($"[assembly: global::Coplt.Com.MarkInterface(typeof(global::{GenBase.RawFullName}))]");
        sb.AppendLine();
    }

    protected override void DoGen()
    {
        sb.AppendLine(GenBase.Target.Code.Replace("partial", "unsafe partial"));
        sb.AppendLine($"    : {IComInterface}, {IComInterface}<{varying.name}>, {TypeName}.IComInterface");
        if (!varying.isIUnknown)
        {
            var parent = varying.parent ?? "global::Coplt.Com.IUnknown";
            sb.AppendLine($"    , {IComInterface}<{parent}>");
        }
        sb.AppendLine("{");

        GenGuidField();
        GenInterface();
        GenVPtr();
        GenVTable();
        GenMembers();

        if (!varying.isIUnknown) GenIUnknown();

        sb.AppendLine();
        sb.AppendLine("}");
    }

    private void GenGuidField()
    {
        sb.AppendLine();
        sb.AppendLine($"    static ref readonly Guid {IComInterface}.Guid => ref {DataClassName}.Guid;");
    }

    private void GenVPtr()
    {
        sb.AppendLine();
        sb.AppendLine($"    public VirtualTable* LpVtbl;");
    }

    private void GenInterface()
    {
        sb.AppendLine();
        sb.AppendLine($"    public interface IComInterface");
        sb.AppendLine($"        : {IComInterface}<{varying.name}>");
        if (!varying.isIUnknown)
        {
            var parent = varying.parent ?? "global::Coplt.Com.IUnknown";
            sb.AppendLine($"        , {parent}.IComInterface");
        }
        sb.AppendLine($"    ;");
    }

    private void GenVTable()
    {
        sb.AppendLine();
        sb.AppendLine($"    public struct VirtualTable");
        sb.AppendLine($"    {{");
        if (!varying.isIUnknown)
        {
            var parent = varying.parent ?? "global::Coplt.Com.IUnknown";
            sb.AppendLine($"        public {parent}.VirtualTable {parent.Split('.').Last()};");
        }
        foreach (var member in varying.members)
        {
            if (member.Kind is InterfaceGenerator.MemberKind.Method)
            {
                sb.AppendLine();
                var rr = member.ReturnRefKind is not RefKind.None ? "*" : "";
                sb.Append($"        public delegate* unmanaged[Cdecl]<{varying.name}*");
                foreach (var param in member.Params)
                {
                    var pr = param.RefKind is not RefKind.None ? "*" : "";
                    sb.Append($", {param.Type}{pr}");
                }
                sb.AppendLine($", {member.ReturnType}{rr}> {member.Name};");
            }
            else
            {
                sb.AppendLine();
                var rr = member.ReturnRefKind is not RefKind.None ? "*" : "";
                if ((member.Flags & InterfaceGenerator.MemberFlags.Get) != 0)
                {
                    sb.AppendLine($"        public delegate* unmanaged[Cdecl]<{varying.name}*, {member.ReturnType}{rr}> get_{member.Name};");
                }
                if ((member.Flags & InterfaceGenerator.MemberFlags.Set) != 0)
                {
                    sb.AppendLine($"        public delegate* unmanaged[Cdecl]<{varying.name}*, {member.ReturnType}{rr}, void> set_{member.Name};");
                }
            }
        }
        sb.AppendLine($"    }}");
    }

    private void GenMembers()
    {
        var mi = 0;
        foreach (var member in varying.members)
        {
            if (member.Kind is InterfaceGenerator.MemberKind.Method)
            {
                sb.AppendLine();
                sb.AppendLine($"    [{InterfaceMember}({mi++}), {AggressiveInlining}]");
                var ro = (member.Flags & InterfaceGenerator.MemberFlags.Readonly) != 0 ? "readonly " : "";
                var rr = RefOnRet(member.ReturnRefKind);
                sb.Append($"    public {ro}partial {rr}{member.ReturnType} {member.Name}(");
                var pi = 0;
                foreach (var param in member.Params)
                {
                    var i = pi++;
                    if (i != 0) sb.Append(", ");
                    var pr = RefOnParam(param.RefKind);
                    sb.Append($"{pr}{param.Type} p{i}");
                }
                sb.AppendLine($")");
                sb.AppendLine($"    {{");
                pi = 0;
                var fi = 0;
                foreach (var param in member.Params)
                {
                    var i = pi++;
                    if (param.RefKind is RefKind.None) continue;
                    sb.AppendLine($"        fixed({param.Type}* f{fi++} = &p{i})");
                }
                var has_fixed = fi > 0;
                var space = has_fixed ? "            " : "        ";
                if (has_fixed) sb.AppendLine($"        {{");
                var re = member.ReturnRefKind is RefKind.None ? "return " : "return ref *";
                sb.Append($"{space}{re}this.LpVtbl->{member.Name}(ComUtils.AsPointer(in this)");
                pi = 0;
                fi = 0;
                foreach (var param in member.Params)
                {
                    var i = pi++;
                    var n = param.RefKind is RefKind.None ? $"p{i}" : $"f{fi++}";
                    sb.Append($", {n}");
                }
                sb.AppendLine($");");
                if (has_fixed) sb.AppendLine($"        }}");
                sb.AppendLine($"    }}");
            }
            else
            {
                var nth = mi;
                sb.AppendLine();
                sb.AppendLine($"    [{InterfaceMember}({nth})]");
                var rr = RefOnRet(member.ReturnRefKind);
                sb.AppendLine($"    public partial {rr}{member.ReturnType} {member.Name}");
                sb.AppendLine($"    {{");
                if ((member.Flags & InterfaceGenerator.MemberFlags.Get) != 0)
                {
                    mi++;
                    var re = member.ReturnRefKind is RefKind.None ? "" : "ref *";
                    sb.AppendLine($"        [{AggressiveInlining}]");
                    sb.AppendLine($"        get => {re}this.LpVtbl->get_{member.Name}(ComUtils.AsPointer(in this));");
                }
                if ((member.Flags & InterfaceGenerator.MemberFlags.Set) != 0)
                {
                    mi++;
                    sb.AppendLine($"        [{AggressiveInlining}]");
                    sb.AppendLine($"        set => this.LpVtbl->set_{member.Name}(ComUtils.AsPointer(in this), value);");
                }
                sb.AppendLine($"    }}");
            }
        }
    }

    public static string RefOnRet(RefKind kind) => kind switch
    {
        RefKind.None => "",
        RefKind.Ref => "ref ",
        RefKind.Out => "",
        RefKind.In => "ref readonly ",
        RefKind.RefReadOnlyParameter => "",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public static string RefOnParam(RefKind kind) => kind switch
    {
        RefKind.None => "",
        RefKind.Ref => "ref ",
        RefKind.Out => "out ",
        RefKind.In => "in ",
        RefKind.RefReadOnlyParameter => "ref readonly ",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public static string RefOnArg(RefKind kind) => kind switch
    {
        RefKind.None => "",
        RefKind.Ref => "ref ",
        RefKind.Out => "out ",
        RefKind.In => "in ",
        RefKind.RefReadOnlyParameter => "in ",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private void GenIUnknown()
    {
        sb.AppendLine();
        sb.AppendLine($"    [{AggressiveInlining}]");
        sb.AppendLine(
            $"    public readonly global::Coplt.Com.HResult QueryInterface<T>(out global::Coplt.Com.Rc<T> obj) where T : struct, IComInterface<global::Coplt.Com.IUnknown>");
        sb.AppendLine($"        => ((global::Coplt.Com.IUnknown*)global::Coplt.Com.ComUtils.AsPointer(in this))->QueryInterface(out obj);");
        sb.AppendLine();
        sb.AppendLine($"    [{AggressiveInlining}]");
        sb.AppendLine($"    public readonly global::Coplt.Com.Rc<T> TryCast<T>() where T : struct, IComInterface<global::Coplt.Com.IUnknown>");
        sb.AppendLine($"        => ((global::Coplt.Com.IUnknown*)global::Coplt.Com.ComUtils.AsPointer(in this))->TryCast<T>();");
    }

    protected override void DoGenAfterType()
    {
        sb.AppendLine();
        sb.AppendLine($"public static unsafe partial class {varying.name}Extensions");
        sb.AppendLine($"{{");

        #region extension T

        sb.AppendLine();
        sb.AppendLine($"    extension<T>(ref T self) where T : struct, IComInterface<{varying.name}>");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        public {varying.name}* As{varying.name}");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            [{AggressiveInlining}]");
        sb.AppendLine($"            get => ({varying.name}*){ComUtils}.AsPointer(in self);");
        sb.AppendLine($"        }}");

        foreach (var member in varying.members)
        {
            if (member.Kind is InterfaceGenerator.MemberKind.Method)
            {
                sb.AppendLine();
                var rr = RefOnRet(member.ReturnRefKind);
                sb.AppendLine($"        [{AggressiveInlining}]");
                sb.Append($"        public {rr}{member.ReturnType} {member.Name}(");
                var pi = 0;
                foreach (var param in member.Params)
                {
                    var i = pi++;
                    if (i != 0) sb.Append(", ");
                    var pr = RefOnParam(param.RefKind);
                    sb.Append($"{pr}{param.Type} p{i}");
                }
                sb.Append($") => self.As{varying.name}->{member.Name}(");
                pi = 0;
                foreach (var param in member.Params)
                {
                    var i = pi++;
                    if (i != 0) sb.Append(", ");
                    var pr = RefOnArg(param.RefKind);
                    sb.Append($"{pr}p{i}");
                }
                sb.AppendLine($");");
            }
            else
            {
                sb.AppendLine();
                var rr = RefOnRet(member.ReturnRefKind);
                sb.AppendLine($"        public {rr}{member.ReturnType} {member.Name}");
                sb.AppendLine($"        {{");
                if ((member.Flags & InterfaceGenerator.MemberFlags.Get) != 0)
                {
                    var re = member.ReturnRefKind is RefKind.None ? "" : "ref *";
                    sb.AppendLine($"            [{AggressiveInlining}]");
                    sb.AppendLine($"            get => {re}self.As{varying.name}->{member.Name};");
                }
                if ((member.Flags & InterfaceGenerator.MemberFlags.Set) != 0)
                {
                    sb.AppendLine($"            [{AggressiveInlining}]");
                    sb.AppendLine($"            set => self.As{varying.name}->{member.Name} = value;");
                }
                sb.AppendLine($"        }}");
            }
        }

        sb.AppendLine($"    }}");

        #endregion

        #region extension Rc<T>

        sb.AppendLine();
        sb.AppendLine($"    extension<T>(ref readonly Rc<T> self) where T : struct, IComInterface<{varying.name}>");
        sb.AppendLine($"    {{");

        foreach (var member in varying.members)
        {
            if (member.Kind is InterfaceGenerator.MemberKind.Method)
            {
                sb.AppendLine();
                var rr = RefOnRet(member.ReturnRefKind);
                sb.AppendLine($"        [{AggressiveInlining}]");
                sb.Append($"        public {rr}{member.ReturnType} {member.Name}(");
                var pi = 0;
                foreach (var param in member.Params)
                {
                    var i = pi++;
                    if (i != 0) sb.Append(", ");
                    var pr = RefOnParam(param.RefKind);
                    sb.Append($"{pr}{param.Type} p{i}");
                }
                sb.Append($") => (({varying.name}*)self.Handle)->{member.Name}(");
                pi = 0;
                foreach (var param in member.Params)
                {
                    var i = pi++;
                    if (i != 0) sb.Append(", ");
                    var pr = RefOnArg(param.RefKind);
                    sb.Append($"{pr}p{i}");
                }
                sb.AppendLine($");");
            }
            else
            {
                sb.AppendLine();
                var rr = RefOnRet(member.ReturnRefKind);
                sb.AppendLine($"        public {rr}{member.ReturnType} {member.Name}");
                sb.AppendLine($"        {{");
                if ((member.Flags & InterfaceGenerator.MemberFlags.Get) != 0)
                {
                    var re = member.ReturnRefKind is RefKind.None ? "" : "ref *";
                    sb.AppendLine($"            [{AggressiveInlining}]");
                    sb.AppendLine($"            get => {re}(({varying.name}*)self.Handle)->{member.Name};");
                }
                if ((member.Flags & InterfaceGenerator.MemberFlags.Set) != 0)
                {
                    sb.AppendLine($"            [{AggressiveInlining}]");
                    sb.AppendLine($"            set => (({varying.name}*)self.Handle)->{member.Name} = value;");
                }
                sb.AppendLine($"        }}");
            }
        }

        sb.AppendLine($"    }}");

        #endregion

        sb.AppendLine();
        sb.AppendLine($"}}");
    }

    protected override void DoGenFileScope()
    {
        sb.AppendLine();
        sb.AppendLine($"file class {DataClassName}");
        sb.AppendLine("{");

        GenGuidData();

        sb.AppendLine();
        sb.AppendLine("}");
    }

    private void GenGuidData()
    {
        sb.AppendLine();
        sb.AppendLine($"    public static readonly Guid Guid = new(\"{varying.guid:D}\");");
    }
}
