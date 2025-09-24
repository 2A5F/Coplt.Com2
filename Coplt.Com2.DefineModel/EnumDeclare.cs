using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Coplt.Com2.Json;

namespace Coplt.Com2.DefineModel;

public record struct EnumDeclare
{
    public required string Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required EnumFlags Flags { get; set; }
    /// <summary>
    /// Index of type
    /// </summary>
    public required uint Underlying { get; set; }
    public required ImmutableArray<EnumItemDeclare> Items { get; set; }
}

[Flags]
[JsonConverter(typeof(SnakeCaseLower_JsonStringEnumConverter1<EnumFlags>))]
public enum EnumFlags
{
    None = 0,
    Flags = 1 << 0,
}

public record struct EnumItemDeclare
{
    public string Name { get; set; }
    public string Value { get; set; }
}
