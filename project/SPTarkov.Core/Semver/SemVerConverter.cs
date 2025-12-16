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

public class SemVerDictConverter : JsonConverter<Dictionary<string, Version>>
{
    public override Dictionary<string, Version>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject");
        }

        var result = new Dictionary<string, Version>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName");
            }

            var key = reader.GetString();
            if (key is null)
            {
                throw new JsonException("Null property name");
            }

            reader.Read(); // Move to value

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected string value for SemVer");
            }

            var semverString = reader.GetString();
            if (semverString is null)
            {
                throw new JsonException("Null SemVer value");
            }

            result[key] = Version.Parse(semverString);
        }

        throw new JsonException("Unexpected end of JSON");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, Version> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            writer.WriteStringValue(kvp.Value.ToString());
        }

        writer.WriteEndObject();
    }
}
