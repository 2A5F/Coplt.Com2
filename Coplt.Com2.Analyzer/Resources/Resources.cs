using System.Resources;
using Microsoft.CodeAnalysis;

namespace Coplt.Com2.Analyzer.Resources;

public class Strings
{
    private static ResourceManager? resourceManager;
    public static ResourceManager ResourceManager => resourceManager ??= new ResourceManager(typeof(Strings));

    public static LocalizableResourceString Get(string name) =>
        new(name, ResourceManager, typeof(Strings));

    public static LocalizableResourceString Get(string name, params string[] formatArgs) =>
        new(name, ResourceManager, typeof(Strings), formatArgs);
}
