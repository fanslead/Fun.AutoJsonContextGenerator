using System.Text.Json.Serialization;

namespace AutoJsonContextGenerator.Sample
{
    [JsonSourceGenerationOptions(WriteIndented = false)]
    [JsonSerializable(typeof(System.Collections.Generic.List<global::AutoJsonContextGenerator.Sample.Users>))]
    [JsonSerializable(typeof(global::AutoJsonContextGenerator.Sample.Users[]))]
    [JsonSerializable(typeof(System.Collections.Generic.Dictionary<string, global::AutoJsonContextGenerator.Sample.Users>))]
    internal partial class AutoJsonContext : JsonSerializerContext
    {
    }
}
