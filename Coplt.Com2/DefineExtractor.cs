using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types;
using Coplt.Com2.Symbols;

namespace Coplt.Com2;

public static class DefineExtractor
{
    public static void Load(string path)
    {
        var asm = AssemblyDefinition.FromFile(path);
        var interface_marks = asm.FindCustomAttributes("Coplt.Com", "MarkInterfaceAttribute")
            .Select(mark => ((TypeDefOrRefSignature)mark.Signature!.FixedArguments[0].Element!).Resolve()!)
            .ToList();

        var db = new SymbolDb();

        foreach (var type in interface_marks)
        {
            ExtraInterface(db, type);
        }
    }

    public static void ExtraInterface(SymbolDb db, TypeDefinition type)
    {
        var name = $"{type.Name}";
        _ = Guid.TryParse($"{type.FindCustomAttributes("System.Runtime.InteropServices", "GuidAttribute").FirstOrDefault()
            ?.Signature!.FixedArguments[0].Element!}", out var guid);
        foreach (var method in type.Methods)
        {
            var member_attr = method.FindCustomAttributes("Coplt.Com", "InterfaceMemberAttribute").FirstOrDefault();
            if (member_attr == null) continue;
            var member_index = (uint)member_attr.Signature!.FixedArguments[0].Element!;
            var member_name = $"{method.Name}";
            var sig = method.Signature!;
            var ret_type = db.ExtraType(sig.ReturnType.Resolve() ?? throw new Exception($"Resolve failed: {sig.ReturnType}"));
            foreach (var p in method.Parameters)
            {
                var p_type = db.ExtraType(p.ParameterType);
                var p_name = p.Name;
            }
        }
    }
}
