using AsmResolver.DotNet;

namespace Coplt.Com2;

public static class Utils
{
    public static async ValueTask LogError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        await Console.Error.WriteLineAsync(msg);
        Console.ResetColor();
    }
    
    public static IEnumerable<CustomAttribute> FindCustomAttributesNoGeneric(
        this IHasCustomAttribute self,
        string ns,
        string name)
    {
        for (int i = 0; i < self.CustomAttributes.Count; ++i)
        {
            CustomAttribute customAttribute = self.CustomAttributes[i];
            ITypeDefOrRef? declaringType = customAttribute.Constructor?.DeclaringType;
            if (declaringType != null && declaringType.IsTypeOfNoGeneric(ns, name))
                yield return customAttribute;
        }
    }
    
    public static bool IsTypeOfNoGeneric(this ITypeDescriptor type, string ns, string name)
    {
        if (type.Name == null) return false;
        if (type.Namespace != ns) return false;
        var i = type.Name.IndexOf(name, StringComparison.Ordinal);
        if (i < 0) return false;
        var sub = type.Name.AsSpan(i + name.Length);
        if (sub.IsEmpty) return true;
        return sub[0] == '<';
    }
}
