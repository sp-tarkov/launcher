using System.Text.Json;
using System.Text.Json.Serialization;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Semver;

public class SemVerConverter : JsonConverter<Version>
{
    public override Version? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return Version.Parse(str);
    }

    public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
