using System.Text.Json.Serialization;
using Figment.Common.Data;

namespace Figment.Common;

public class SchemaRefField(string Name, string SchemaGuid) : SchemaFieldBase(Name)
{
    public const string TYPE = "ref";

    [JsonIgnore] // Only for enums.
    public override string Type { get; } = TYPE;

    [JsonIgnore]
    public string SchemaGuid { get; set; } = SchemaGuid;

    [JsonPropertyName("ref")]
    public string Id
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SchemaGuid))
                return "???";
            return $"https://figment.seanmcelroy.com/{SchemaGuid}.schema.json";
        }
        set
        {
            if (value.StartsWith("https://figment.seanmcelroy.com/", StringComparison.OrdinalIgnoreCase)
            && value.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            {
                SchemaGuid = value[32..^12];
            }
        }
    }

    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (!Required && value == null)
            return true;
        if (Required && value == null)
            return false;

        var ssp = StorageUtility.StorageProvider.GetSchemaStorageProvider();
        if (ssp == null)
            return false;

        if (!await ssp.GuidExists(SchemaGuid, cancellationToken))
            return false;

        var tsp = StorageUtility.StorageProvider.GetThingStorageProvider();
        if (tsp == null)
            return false;

        if (value is string s && !await tsp.GuidExists(s, cancellationToken))
            return false;

        return true;
    }

    public override async Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken)
    {
        var provider = StorageUtility.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
            return "???";

        if (!await provider.GuidExists(SchemaGuid, cancellationToken))
            return "???";

        var schemaLoaded = await provider.LoadAsync(SchemaGuid, cancellationToken);
        if (schemaLoaded == null)
            return "???";

        return $"{schemaLoaded.Name} ({schemaLoaded.Guid})";
    }
}