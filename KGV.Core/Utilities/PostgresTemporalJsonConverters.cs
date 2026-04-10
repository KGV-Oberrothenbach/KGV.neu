using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using STJ = System.Text.Json;
using STJSerialization = System.Text.Json.Serialization;
using NSJ = Newtonsoft.Json;

namespace KGV.Core.Utilities
{
    internal static class PostgresDateOnlyConverterHelper
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

        public static DateTime ParseRequired(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return CreateDateOnly(DateTime.MinValue.Year, DateTime.MinValue.Month, DateTime.MinValue.Day);

            if (DateTime.TryParseExact(raw, ReadFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return CreateDateOnly(parsed.Year, parsed.Month, parsed.Day);

            if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var offsetValue))
                return CreateDateOnly(offsetValue.Year, offsetValue.Month, offsetValue.Day);

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return CreateDateOnly(parsed.Year, parsed.Month, parsed.Day);

            throw new STJ.JsonException("Ungültiger date-Wert.");
        }

        public static string Format(DateTime value)
            => CreateDateOnly(value.Year, value.Month, value.Day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static DateTime CreateDateOnly(int year, int month, int day)
            => new(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
    }

    public sealed class PostgresDateOnlyJsonConverter : STJSerialization.JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new STJ.JsonException("Ungültiger date-Wert.");

            return PostgresDateOnlyConverterHelper.ParseRequired(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(PostgresDateOnlyConverterHelper.Format(value));
        }
    }

    public sealed class NullablePostgresDateOnlyJsonConverter : STJSerialization.JsonConverter<DateTime?>
    {
        private readonly PostgresDateOnlyJsonConverter _inner = new();

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

    public sealed class NewtonsoftPostgresDateOnlyJsonConverter : NSJ.JsonConverter<DateTime>
    {
        public override DateTime ReadJson(NSJ.JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, NSJ.JsonSerializer serializer)
        {
            if (reader.TokenType == NSJ.JsonToken.Null)
                return PostgresDateOnlyConverterHelper.CreateDateOnly(DateTime.MinValue.Year, DateTime.MinValue.Month, DateTime.MinValue.Day);

            if (reader.TokenType != NSJ.JsonToken.String && reader.Value == null)
                throw new NSJ.JsonSerializationException("Ungültiger date-Wert.");

            return PostgresDateOnlyConverterHelper.ParseRequired(reader.Value?.ToString());
        }

        public override void WriteJson(NSJ.JsonWriter writer, DateTime value, NSJ.JsonSerializer serializer)
        {
            writer.WriteValue(PostgresDateOnlyConverterHelper.Format(value));
        }
    }

    public sealed class NewtonsoftNullablePostgresDateOnlyJsonConverter : NSJ.JsonConverter<DateTime?>
    {
        private readonly NewtonsoftPostgresDateOnlyJsonConverter _inner = new();

        public override DateTime? ReadJson(NSJ.JsonReader reader, Type objectType, DateTime? existingValue, bool hasExistingValue, NSJ.JsonSerializer serializer)
        {
            if (reader.TokenType == NSJ.JsonToken.Null)
                return null;

            return _inner.ReadJson(reader, typeof(DateTime), existingValue ?? default, hasExistingValue, serializer);
        }

        public override void WriteJson(NSJ.JsonWriter writer, DateTime? value, NSJ.JsonSerializer serializer)
        {
            if (!value.HasValue)
            {
                writer.WriteNull();
                return;
            }

            _inner.WriteJson(writer, value.Value, serializer);
        }
    }

    public sealed class PostgresTimestampWithoutTimeZoneJsonConverter : STJSerialization.JsonConverter<DateTime>
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

            throw new STJ.JsonException("Ungültiger timestamp-Wert.");
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

    public sealed class NullablePostgresTimestampWithoutTimeZoneJsonConverter : STJSerialization.JsonConverter<DateTime?>
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
