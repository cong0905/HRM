using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HRM.Next.Api.Serialization;

public static class ContractJson
{
    public const string DecimalPattern = @"^-?(0|[1-9][0-9]*)(\.[0-9]+)?$";

    public static void Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.RespectRequiredConstructorParameters = true;
        options.RespectNullableAnnotations = true;
        options.NumberHandling = JsonNumberHandling.Strict;
        options.Converters.Add(new DecimalStringConverter());
        options.Converters.Add(new UtcTimestampConverter());
    }

    private sealed class DecimalStringConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Decimal values must be JSON strings.");
            var text = reader.GetString() ?? throw new JsonException("Decimal values must not be null.");
            if (!Regex.IsMatch(text, DecimalPattern, RegexOptions.CultureInvariant) ||
                !decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var value) ||
                Canonical(text) != Canonical(value.ToString(CultureInfo.InvariantCulture)))
                throw new JsonException("Invalid decimal string or loss of precision.");
            return value;
        }

        private static string Canonical(string text)
        {
            if (text.Contains('.')) text = text.TrimEnd('0').TrimEnd('.');
            return text == "-0" ? "0" : text;
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
    }

    private sealed class UtcTimestampConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String ||
                !Regex.IsMatch(reader.GetString() ?? "", @"(Z|[+-][0-9]{2}:[0-9]{2})$", RegexOptions.CultureInvariant) ||
                !reader.TryGetDateTimeOffset(out var value))
                throw new JsonException("Timestamp must be ISO8601 with an explicit offset.");
            return value.ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
    }
}
