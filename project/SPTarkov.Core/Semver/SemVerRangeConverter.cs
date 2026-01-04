using System.Text.Json;
using System.Text.Json.Serialization;
using Range = SemanticVersioning.Range;

namespace SPTarkov.Core.Semver;

public class SemVerRangeConverter : JsonConverter<Range>
{
    public override Range? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return Range.Parse(stringValue);
    }

    public override void Write(Utf8JsonWriter writer, Range value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
