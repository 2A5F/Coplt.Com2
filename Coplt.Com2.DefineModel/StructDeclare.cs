using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Coplt.Com2.DefineModel;

public record struct StructDeclare
{
    public required string Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required StructFlags Flags { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ImmutableArray<string> TypeParams { get; set; }
    public ImmutableArray<FieldDeclare> Fields { get; set; }
}

[Flags]
public enum StructFlags
{
    None,
    Union = 1 << 0,
    ByRef = 1 << 1,
}

public record struct FieldDeclare
{
    public required uint Type { get; set; }
    public required string Name { get; set; }
}
