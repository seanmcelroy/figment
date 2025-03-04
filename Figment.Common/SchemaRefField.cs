using System.Text.Json.Serialization;
using Figment.Common.Data;

namespace Figment.Common;

/// <summary>
/// This field is a reference to a thing in a given <see cref="Schema"/>
/// </summary>
/// <param name="Name">The name of the field</param>
/// <param name="SchemaGuid">The schema to which the thing (whose guid is the value of this field) must adhere</param>
public class SchemaRefField(string Name, string SchemaGuid) : SchemaFieldBase(Name)
{
    public const string SCHEMA_FIELD_TYPE = "ref";

    [JsonIgnore] // Only for enums.
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    [JsonIgnore]
    public string SchemaGuid { get; set; } = SchemaGuid;

    [JsonPropertyName("ref")] // TODO: Someday, make this a $ meta property
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

        var ssp = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (ssp == null)
            return false;

        if (!await ssp.GuidExists(SchemaGuid, cancellationToken))
            return false;

        var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (tsp == null)
            return false;

        if (value is string s && !await tsp.GuidExists(s, cancellationToken))
            return false;

        return true;
    }

    public override async Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
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