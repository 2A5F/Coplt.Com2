using System.Collections.Immutable;

namespace Coplt.Com2.DefineModel;

public record struct EnumDeclare
{
    public required string Name { get; set; }
    public required EnumFlags Flags { get; set; }
    /// <summary>
    /// Index of type
    /// </summary>
    public required uint Underlying { get; set; }
    public required ImmutableArray<EnumItemDeclare> Items { get; set; }
}

[Flags]
public enum EnumFlags
{
    None = 0,
    Flags = 1 << 0,
}

public record struct EnumItemDeclare
{
    public string Name { get; set; }
    public Int128 Value { get; set; }
}
