using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Coplt.Com2.DefineModel;

public record struct ComDefine
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required ImmutableArray<TypeDeclare> Types { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required ImmutableArray<StructDeclare> Structs { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required ImmutableArray<EnumDeclare> Enums { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required ImmutableArray<InterfaceDeclare> Interfaces { get; set; }
}
