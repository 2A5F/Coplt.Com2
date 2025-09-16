using System.Text.Json.Serialization;
using Coplt.Com2.DefineModel;

namespace Coplt.Com2;

[JsonSerializable(typeof(ComDefine))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    UseStringEnumConverter = true
)]
public partial class ComDefineJsonContext : JsonSerializerContext;
