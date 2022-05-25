using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HeckedV2ToV3
{
    internal class TruncatedDoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDouble();
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(Math.Round(value, 4));
        }
    }
}
