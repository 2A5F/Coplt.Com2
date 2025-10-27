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
    public List<AOutput> Outputs { get; set; } =
    [
        new JsonOutput
        {
            Path = "./path.to.define.json",
        },
        new CppOutput
        {
            Path = "./path.to.header.file.dir",
        },
        new RustOutput
        {
            Path = "./path.to.mod.file.rs",
        }
    ];
}

[JsonDerivedType(typeof(JsonOutput), typeDiscriminator: "json")]
[JsonDerivedType(typeof(CppOutput), typeDiscriminator: "cpp")]
[JsonDerivedType(typeof(RustOutput), typeDiscriminator: "rust")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
public abstract record AOutput
{
    public required string Path { get; set; } = null!;
}

public record JsonOutput : AOutput { }
