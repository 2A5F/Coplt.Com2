using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Coplt.Com2.DefineModel;

public struct InterfaceDeclare
{
    public required string Name { get; set; }
    public required Guid Guid { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Guid? Parent { get; set; }
    public required ImmutableArray<MethodDeclare> Methods { get; set; }
}

public record struct MethodDeclare
{
    public required string Name { get; set; }
    public required uint Index { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required MethodFlags Flags { get; set; }
    public required uint ReturnType { get; set; }
    public required ImmutableArray<ParameterDeclare> Parameters { get; set; }
}

[Flags]
public enum MethodFlags
{
    None = 0,
    Const = 1 << 0,
    ReturnByRef = 1 << 1,
}

public record struct ParameterDeclare
{
    public required string Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required ParameterFlags Flags { get; set; }
    public required uint Type { get; set; }
}

[Flags]
public enum ParameterFlags
{
    None = 0,
    In = 1 << 0,
    Out = 1 << 1,
}
