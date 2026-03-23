using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KGV.Core.Utilities
{
    public sealed class PostgresDateOnlyJsonConverter : JsonConverter<DateTime>
    {
        private static readonly string[] ReadFormats =
        {
            "yyyy-MM-dd",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF",
            "O"
        };

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var raw = reader.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                    return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified);

                if (DateTime.TryParseExact(raw, ReadFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    return CreateDateOnly(parsed.Year, parsed.Month, parsed.Day);

                if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var offsetValue))
                    return CreateDateOnly(offsetValue.Year, offsetValue.Month, offsetValue.Day);

                if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                    return CreateDateOnly(parsed.Year, parsed.Month, parsed.Day);
            }

            throw new JsonException("Ungültiger date-Wert.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(CreateDateOnly(value.Year, value.Month, value.Day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        private static DateTime CreateDateOnly(int year, int month, int day)
        {
            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
        }
    }

    public sealed class PostgresTimestampWithoutTimeZoneJsonConverter : JsonConverter<DateTime>
    {
        private static readonly string[] ReadFormats =
        {
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF",
            "O"
        };

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var raw = reader.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                    return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified);

                if (DateTime.TryParseExact(raw, ReadFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    return CreateTimestamp(parsed);

                if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var offsetValue))
                    return CreateTimestamp(offsetValue.Year, offsetValue.Month, offsetValue.Day, offsetValue.Hour, offsetValue.Minute, offsetValue.Second, offsetValue.Millisecond);

                if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                    return CreateTimestamp(parsed);
            }

            throw new JsonException("Ungültiger timestamp-Wert.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(CreateTimestamp(value).ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
        }

        private static DateTime CreateTimestamp(DateTime value)
        {
            return CreateTimestamp(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond);
        }

        private static DateTime CreateTimestamp(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            return new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Unspecified);
        }
    }

    public sealed class NullablePostgresTimestampWithoutTimeZoneJsonConverter : JsonConverter<DateTime?>
    {
        private readonly PostgresTimestampWithoutTimeZoneJsonConverter _inner = new();

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType == JsonTokenType.Null
                ? null
                : _inner.Read(ref reader, typeof(DateTime), options);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            _inner.Write(writer, value.Value, options);
        }
    }
}
