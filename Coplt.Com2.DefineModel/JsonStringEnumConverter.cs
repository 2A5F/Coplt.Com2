using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coplt.Com2.Json;

public class SnakeCaseLower_JsonStringEnumConverter1<T>() : JsonStringEnumConverter<T>(JsonNamingPolicy.SnakeCaseLower)
    where T : struct, Enum;
