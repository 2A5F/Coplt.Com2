using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coplt.Com2;

[JsonSerializable(typeof(Config))]
[JsonSourceGenerationOptions(
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    UseStringEnumConverter = true
)]
public partial class ConfigLoadJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(Config))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    UseStringEnumConverter = true
)]
public partial class ConfigSaveJsonContext : JsonSerializerContext;

public record Config
{
    public List<string> Inputs { get; set; } =
    [
        "./path.to.dll.or.json"
    ];
    public List<Output> Outputs { get; set; } =
    [
        new()
        {
            Type = OutputType.Json,
            Path = "./path.to.define.json",
        },
        new()
        {
            Type = OutputType.Cpp,
            Path = "./path.to.header.file.dir",
        }
    ];
}

public enum OutputType
{
    Json,
    Cpp,
}

public record Output
{
    public OutputType Type { get; set; }
    public string Path { get; set; } = null!;
}
