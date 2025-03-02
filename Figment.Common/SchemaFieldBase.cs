using System.Text.Json.Serialization;

namespace Figment.Common;

[JsonPolymorphic]
[JsonDerivedType(typeof(SchemaBooleanField), typeDiscriminator: "bool")]
[JsonDerivedType(typeof(SchemaDateField), typeDiscriminator: "date")]
[JsonDerivedType(typeof(SchemaEmailField), typeDiscriminator: "email")]
[JsonDerivedType(typeof(SchemaEnumField), typeDiscriminator: "enum")]
[JsonDerivedType(typeof(SchemaIntegerField), typeDiscriminator: "integer")]
[JsonDerivedType(typeof(SchemaNumberField), typeDiscriminator: "number")]
[JsonDerivedType(typeof(SchemaPhoneField), typeDiscriminator: "phone")]
[JsonDerivedType(typeof(SchemaRefField), typeDiscriminator: "ref")]
[JsonDerivedType(typeof(SchemaSchemaField), typeDiscriminator: "schema")]
[JsonDerivedType(typeof(SchemaTextField), typeDiscriminator: "text")]
[JsonDerivedType(typeof(SchemaUriField), typeDiscriminator: "url")]
public abstract class SchemaFieldBase(string Name)
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    [JsonIgnore]
    public string Name { get; init; } = Name;

    [JsonIgnore]
    public bool Required { get; set; }

    /// <summary>
    /// Validates a parsed field meets all applicable optionally-defined constraints
    /// </summary>
    /// <returns></returns>
    public abstract Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken);

    public abstract Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken);
}