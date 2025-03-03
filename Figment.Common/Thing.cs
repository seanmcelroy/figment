using System.ComponentModel;
using System.Runtime.CompilerServices;
using Figment.Common.Data;
using Figment.Common.Errors;

namespace Figment.Common;

public class Thing(string Guid, string Name)
{
    private const string NameIndexFileName = $"_thing.names.csv";

    public string Guid { get; set; } = Guid;
    public string Name { get; set; } = Name;
    //    public string? SchemaGuid { get; set; }
    public List<string> SchemaGuids { get; set; } = [];
    public Dictionary<string, object> Properties { get; init; } = [];

    public static async IAsyncEnumerable<Reference> ResolveAsync(
        string guidOrNamePart,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (tsp == null)
            yield break;

        if (await tsp.GuidExists(guidOrNamePart, cancellationToken))
        {
            yield return new Reference
            {
                Guid = guidOrNamePart,
                Type = Reference.ReferenceType.Thing
            };
            yield break;
        }

        // Nope, so GLOBAL name searching...
        var ssp = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (ssp == null)
            yield break;

        await foreach (var schemaRef in ssp.GetAll(cancellationToken))
            await foreach (var (reference, _) in tsp.FindByPartialNameAsync(schemaRef.reference.Guid, guidOrNamePart, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                yield return reference;
            }
    }

    public async IAsyncEnumerable<Schema> GetAssociatedSchemas([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Does this thing adhere to any schemas?
        if (SchemaGuids != null && SchemaGuids.Count > 0)
        {
            var ssp = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
            if (ssp == null)
                yield break;

            foreach (var schemaGuid in SchemaGuids)
                if (!string.IsNullOrWhiteSpace(schemaGuid))
                {
                    if (cancellationToken.IsCancellationRequested)
                        yield break;
                    var schema = await ssp.LoadAsync(schemaGuid, cancellationToken);
                    if (schema != null)
                        yield return schema;
                }
        }
        yield break;
    }

    public static (string escapedPropKey, string fullDisplayName, string simpleDisplayName)
        CarvePropertyName(string truePropertyName, Schema? schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(truePropertyName);

        if (schema != default)
        {
            // Yes, this property belongs to a schema, so chop the schema guid off it for display purposes.
            var choppedPropName = truePropertyName[(schema.Guid.Length + 1)..];
            var escapedPropKey = choppedPropName.Contains(' ') && !choppedPropName.StartsWith('[') && !choppedPropName.EndsWith(']') ? $"[{choppedPropName}]" : choppedPropName;
            var fullDisplayName = $"{schema.EscapedName}.{escapedPropKey}";
            var simpleDisplayName = choppedPropName;

            // Watch out, the schema field could have been deleted but it's still there on the instance.
            if (!schema.Properties.TryGetValue(choppedPropName, out SchemaFieldBase? schemaField))
            {
                AmbientErrorContext.ErrorProvider.LogWarning($"Found property {truePropertyName} ({escapedPropKey}) on thing, but it doesn't appear on schema {schema.Name} ({schema.Guid}).");
                escapedPropKey = truePropertyName.Contains(' ') && !truePropertyName.StartsWith('[') && !truePropertyName.EndsWith(']') ? $"[{truePropertyName}]" : truePropertyName;
                fullDisplayName = escapedPropKey; // b0c1592e-5d79-4fe4-8814-aa6e534d2b7f.phone
                simpleDisplayName = truePropertyName; // b0c1592e-5d79-4fe4-8814-aa6e534d2b7f.phone
            }

            return (escapedPropKey, fullDisplayName, simpleDisplayName);
        }
        else
        {
            var escapedPropKey = truePropertyName.Contains(' ') && !truePropertyName.StartsWith('[') && !truePropertyName.EndsWith(']') ? $"[{truePropertyName}]" : truePropertyName;
            var fullDisplayName = escapedPropKey;
            var simpleDisplayName = truePropertyName;
            return (escapedPropKey, fullDisplayName, simpleDisplayName);
        }
    }

    public async IAsyncEnumerable<ThingProperty> GetProperties([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (Properties == null || Properties.Count == 0)
            yield break;

        // Does this thing adhere to any schemas?
        List<Schema> thingSchemas = [];
        await foreach (var schema in GetAssociatedSchemas(cancellationToken))
        {
            thingSchemas.Add(schema);
        }

        var unsetSchemaFields = thingSchemas
            .Select(s => new { Schema = s, s.Properties })
            .SelectMany(s => s.Properties.Select(p => (s.Schema, p.Key, p.Value)))
            .ToList();

        foreach (var thingProp in Properties)
        {
            // Does this property belong to a schema?
            bool valid = true;
            var schema = thingSchemas.FirstOrDefault(s => thingProp.Key.StartsWith($"{s.Guid}."));
            var (escapedPropKey, fullDisplayName, simpleDisplayName) = CarvePropertyName(thingProp.Key, schema);
            var required = false;
            var schemaFieldType = default(string?);
            if (schema != default)
            {
                // Watch out, the schema field could have been deleted but it's still there on the instance.
                if (schema.Properties.TryGetValue(simpleDisplayName, out SchemaFieldBase? schemaField))
                {
                    valid = schemaField == null || await schemaField.IsValidAsync(thingProp.Value, cancellationToken); // Valid if no schema.
                    required = schemaField != null && schemaField.Required;
                    schemaFieldType = schemaField?.Type;
                }
            }

            yield return new ThingProperty
            {
                TruePropertyName = thingProp.Key,
                FullDisplayName = fullDisplayName,
                SimpleDisplayName = simpleDisplayName,
                SchemaGuid = schema?.Guid,
                Value = thingProp.Value,
                Valid = valid,
                Required = required,
                SchemaFieldType = schemaFieldType,
                SchemaName = schema?.Name
            };
        }
    }

    public async Task<List<ThingUnsetProperty>> GetUnsetProperties(CancellationToken cancellationToken)
    {
        // Does this thing adhere to any schemas?
        List<Schema> thingSchemas = [];
        await foreach (var schema in GetAssociatedSchemas(cancellationToken))
        {
            thingSchemas.Add(schema);
        }

        var unsetSchemaFields = thingSchemas
            .Select(s => new { Schema = s, s.Properties })
            .SelectMany(s => s.Properties.Select(p => (s, s.Schema.Guid, s.Schema.Name, p.Key)))
            .ToDictionary(
                k => (k.Guid, k.Key),
                v =>
                {
                    var (escapedPropKey, fullDisplayName, simpleDisplayName) = CarvePropertyName(
                        $"{v.Guid}.{v.Key}",
                        v.s.Schema);
                    return new ThingUnsetProperty
                    {
                        FullDisplayName = fullDisplayName,
                        SimpleDisplayName = simpleDisplayName,
                        SchemaGuid = v.Guid,
                        SchemaName = v.Name,
                        Field = v.s.Properties[v.Key]
                    };
                });

        await foreach (var thingProperty in GetProperties(cancellationToken))
        {
            if (thingProperty.SchemaGuid != null) // Remove from list of schema properties once we note it's set on the thing.
                _ = unsetSchemaFields.Remove((thingProperty.SchemaGuid, thingProperty.SimpleDisplayName));
        }

        return unsetSchemaFields.Values.ToList();
    }

    public async Task<ThingSetResult> Set(
        string propName,
        string? propValue,
        CancellationToken cancellationToken,
        Func<string, IEnumerable<PossibleNameMatch>, PossibleNameMatch>? chooserHandler = null
        )
    {
        // If prop name came in unescaped, and it should be escaled, then escape it here for comparisons.
        if (propName.Contains(' ') && !propName.StartsWith('[') && !propName.EndsWith(']'))
            propName = $"[{propName}]";

        // Is this property alerady set?
        List<ThingProperty> candidateProperties = [];

        // Step 1, Check EXISTING properties on this thing.
        await foreach (var prop in GetProperties(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                return new ThingSetResult(false);

            if (string.Compare(propName, prop.FullDisplayName, StringComparison.CurrentCultureIgnoreCase) == 0
                && string.Compare(propName, nameof(Schema.Plural), StringComparison.OrdinalIgnoreCase) != 0 // Ignore schema built-in
            )
            {
                // For instance, user does set vendor.[Work Phone]=+12125551234
                candidateProperties.Add(prop);
            }
            else if (string.Compare(propName, prop.SimpleDisplayName, StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                // For instance, user does set [Work Phone]=+12125551234
                candidateProperties.Add(prop);
            }
        }

        // Step 2, Check properties on associated schemas NOT already set on this object
        object? massagedPropValue = propValue;

        await foreach (var schema in GetAssociatedSchemas(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                return new ThingSetResult(false);

            foreach (var schemaProperty in schema.Properties)
            {
                var truePropertyName = $"{schema.Guid}.{schemaProperty.Key}";

                // Does this schema field massage the string input?
                var canMassage = schemaProperty.Value.TryMassageInput(propValue, out object? possibleMassagedPropValue);

                // If this value was for this property, would it be valid?
                var wouldBeValid = canMassage && await schemaProperty.Value.IsValidAsync(massagedPropValue, cancellationToken);

                var candidatesMatch = false;
                for (int i = 0; i < candidateProperties.Count; i++)
                {
                    if (string.CompareOrdinal(candidateProperties[i].TruePropertyName, truePropertyName) == 0)
                    {
                        candidateProperties[i] = new ThingProperty
                        {
                            TruePropertyName = candidateProperties[i].TruePropertyName,
                            FullDisplayName = candidateProperties[i].FullDisplayName,
                            SimpleDisplayName = candidateProperties[i].SimpleDisplayName,
                            SchemaGuid = candidateProperties[i].SchemaGuid,
                            Value = candidateProperties[i].Value,
                            Valid = wouldBeValid,
                            Required = schemaProperty.Value.Required,
                            SchemaFieldType =
                                string.CompareOrdinal(schemaProperty.Value.Type, SchemaRefField.TYPE) == 0
                                    ? $"{SchemaRefField.TYPE}.{((SchemaRefField)schemaProperty.Value).SchemaGuid}"
                                    : schemaProperty.Value.Type,
                            SchemaName = candidateProperties[i].SchemaName
                        };
                        candidatesMatch = true;
                        massagedPropValue = possibleMassagedPropValue; 
                    }
                }
                if (candidatesMatch)
                    continue;// Already set, no need to add a phantom.

                //if (candidateProperties.Any(c => string.CompareOrdinal(c.TruePropertyName, truePropertyName) == 0))
                //    continue; // Already set, no need to add a phantom.

                var fullDisplayName = $"{schema.EscapedName}.{schemaProperty.Key}";
                var simpleDisplayName = schemaProperty.Key.Contains(' ') && !schemaProperty.Key.StartsWith('[') && !schemaProperty.Key.EndsWith(']') ? $"[{schemaProperty.Key}]" : schemaProperty.Key;
                var phantomProp = new ThingProperty
                {
                    TruePropertyName = truePropertyName,
                    FullDisplayName = fullDisplayName,
                    SimpleDisplayName = simpleDisplayName,
                    SchemaGuid = schema.Guid,
                    Value = null,
                    Valid = wouldBeValid,
                    Required = schemaProperty.Value.Required,
                    SchemaFieldType =
                        string.CompareOrdinal(schemaProperty.Value.Type, SchemaRefField.TYPE) == 0
                            ? $"{SchemaRefField.TYPE}.{((SchemaRefField)schemaProperty.Value).SchemaGuid}"
                            : schemaProperty.Value.Type,
                    SchemaName = schema.Name
                };

                if (string.Compare(propName, fullDisplayName, StringComparison.CurrentCultureIgnoreCase) == 0
                    && string.Compare(propName, "plural", StringComparison.OrdinalIgnoreCase) != 0 // Ignore schema built-in
                )
                {
                    // For instance, user does set vendor.[Work Phone]=+12125551234
                    candidateProperties.Add(phantomProp);
                }
                if (string.Compare(propName, simpleDisplayName, StringComparison.CurrentCultureIgnoreCase) == 0
                    && string.Compare(propName, "plural", StringComparison.OrdinalIgnoreCase) != 0 // Ignore schema built-in
                )
                {
                    // For instance, user does set [Work Phone]=+12125551234
                    candidateProperties.Add(phantomProp);
                }
            }
        }

        var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (tsp == null)
            return new ThingSetResult(false);

        switch (candidateProperties.Count)
        {
            case 0:
                {
                    // No existing property by this name on the thing (nor in any associated schema), so we're going to add it.
                    if (massagedPropValue == null || string.IsNullOrWhiteSpace(massagedPropValue.ToString()))
                        Properties.Remove(propName);
                    else
                        Properties[propName] = massagedPropValue;

                    // Special case for Name.
                    if (string.Compare(propName, nameof(Name), StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (massagedPropValue == null || string.IsNullOrWhiteSpace(massagedPropValue.ToString()))
                        {
                            AmbientErrorContext.ErrorProvider.LogError($"Value of {nameof(Name)} cannot be empty.");
                            return new ThingSetResult(false);
                        }
                        Name = massagedPropValue.ToString()!;
                    }

                    var saved = await SaveAsync(cancellationToken);
                    return new ThingSetResult(saved);
                }
            case 1:
                // Exactly one, we need to update:

                // Unset (null it out) case
                if (massagedPropValue == null || string.IsNullOrWhiteSpace(massagedPropValue.ToString()))
                {
                    if (Properties.Remove(candidateProperties[0].TruePropertyName)
                        && candidateProperties[0].Required)
                        AmbientErrorContext.ErrorProvider.LogWarning($"Required {propName} was removed.");

                    var saved = await SaveAsync(cancellationToken);
                    return new ThingSetResult(saved);
                }

                if (!candidateProperties[0].Valid)
                {
                    if (chooserHandler != null
                        && candidateProperties[0].SchemaGuid != null
                        && (candidateProperties[0].SchemaFieldType?.StartsWith(SchemaRefField.TYPE) ?? false))
                    {
                        var remoteSchemaGuid = candidateProperties[0].SchemaFieldType![(SchemaRefField.TYPE.Length + 1)..];

                        var disambig = tsp.FindByPartialNameAsync(remoteSchemaGuid, massagedPropValue.ToString(), cancellationToken)
                            .ToBlockingEnumerable(cancellationToken)
                            .Select(p => new PossibleNameMatch(p.reference, p.name))
                            .ToArray();

                        if (disambig.Length == 1)
                        {
                            Properties[candidateProperties[0].TruePropertyName] = disambig[0].Reference.Guid;
                            AmbientErrorContext.ErrorProvider.LogInfo($"Set {propName} to {disambig[0].Name}.");
                            var saved = await SaveAsync(cancellationToken);
                            return new ThingSetResult(saved);
                        }
                        else if (disambig.Length > 1)
                        {
                            var which = chooserHandler(
                                $"There was more than one {candidateProperties[0].SchemaName} matching '{propValue}'.  Which do you want to select?",
                                disambig);

                            Properties[candidateProperties[0].TruePropertyName] = which.Reference.Guid;
                            var saved = await SaveAsync(cancellationToken);
                            return new ThingSetResult(saved);
                        }
                        else
                        {
                            Properties[candidateProperties[0].TruePropertyName] = massagedPropValue;
                            AmbientErrorContext.ErrorProvider.LogWarning($"Value of {propName} is invalid.");
                            var saved = await SaveAsync(cancellationToken);
                            return new ThingSetResult(saved);
                        }
                    }
                    else
                    {
                        Properties[candidateProperties[0].TruePropertyName] = massagedPropValue;
                        AmbientErrorContext.ErrorProvider.LogWarning($"Value of {propName} is invalid.");
                        var saved = await SaveAsync(cancellationToken);
                        return new ThingSetResult(saved);
                    }
                }

                // Special case for Name.
                {
                    if (string.Compare(propName, nameof(Name), StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (massagedPropValue == null || string.IsNullOrWhiteSpace(massagedPropValue.ToString()))
                        {
                            AmbientErrorContext.ErrorProvider.LogError($"Value of {nameof(Name)} cannot be empty.");
                            return new ThingSetResult(false);
                        }
                        Name = massagedPropValue.ToString()!;
                    }
                    else
                        Properties[candidateProperties[0].TruePropertyName] = massagedPropValue;

                    var saved = await SaveAsync(cancellationToken);
                    return new ThingSetResult(saved);
                }
            default:
                // Ambiguous
                AmbientErrorContext.ErrorProvider.LogError($"Unable to determine which property between {candidateProperties.Select(x => x.TruePropertyName).Aggregate((c, n) => $"{c}, {n}")} to update.");
                return new ThingSetResult(false);
        }

    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (provider == null)
            return false;

        var success = await provider.SaveAsync(this, cancellationToken);
        return success;
    }

    public async Task<bool> DeleteAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (provider == null)
            return false;

        var success = await provider.DeleteAsync(Guid, cancellationToken);
        return success;
    }

    public override string ToString() => Name;
}
