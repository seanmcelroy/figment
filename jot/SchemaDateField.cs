using System.Globalization;
using System.Text.Json.Serialization;

namespace Figment;

public class SchemaDateField(string Name) : SchemaTextField(Name)
{
    // RFC 3339 Formats
    private static readonly string[] _formats = [
        "yyyy-MM-ddTHH:mm:ssK",
        "yyyy-MM-ddTHH:mm:ss.ffK",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.ffZ",
        // Fallbacks
        "yyyy-MM-dd",
        DateTimeFormatInfo.InvariantInfo.UniversalSortableDateTimePattern,
        DateTimeFormatInfo.InvariantInfo.SortableDateTimePattern,
        // Weird fallbacks
        "MMM d, yyyy",
        "MMMM d, yyyy",
        "MMM d yyyy",
        "MMMM  yyyy",
    ];

    [JsonPropertyName("type")]
    public override string Type { get; } = "string";

    [JsonPropertyName("format")]
    public string Format { get; } = "date";

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("date");

    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (!await base.IsValidAsync(value, cancellationToken))
            return false;

        var str = value as string;

        return DateTimeOffset.TryParseExact(str, _formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out _);
    }    
}