using System.Text.Json.Serialization;

namespace Figment.Common;

public class SchemaBooleanField(string Name) : SchemaFieldBase(Name)
{
    public const string SCHEMA_FIELD_TYPE = "bool";

    [JsonPropertyName("type")]
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult(SCHEMA_FIELD_TYPE);

    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
    {
        if (!Required && value == null)
            return Task.FromResult(true);
        if (Required && value == null)
            return Task.FromResult(false);

        return Task.FromResult(bool.TryParse(value!.ToString(), out bool _));
    }

    public override bool TryMassageInput(object? input, out object? output)
    {
        if (input == null || input.GetType() == typeof(bool))
        {
            output = input;
            return true;
        }

        if (input is int i) {
            output = i != 0;
            return true;
        }

        var prov = input.ToString();

        if (bool.TryParse(prov, out bool provBool)) {
            output = provBool;
            return true;
        }

        if (string.Compare("yes", prov, StringComparison.CurrentCultureIgnoreCase) == 0)
        {
            output = true;
            return true;
        }

        if (string.Compare("no", prov, StringComparison.CurrentCultureIgnoreCase) == 0)
        {
            output = false;
            return true;
        }

        return base.TryMassageInput(input, out output);
    }
}