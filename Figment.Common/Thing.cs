/*
Figment
Copyright (C) 2025  Sean McElroy

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Figment.Common.Calculations;
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

    //[Obsolete("Do not use outside of Things")]
    public Dictionary<string, object> Properties { get; init; } = [];

    [JsonIgnore]
    public DateTime CreatedOn { get; init; }
    [JsonIgnore]
    public DateTime LastModified { get; set; }
    [JsonIgnore]
    public DateTime LastAccessed { get; set; }

    public static async IAsyncEnumerable<Reference> ResolveAsync(
        string guidOrNamePart,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guidOrNamePart);

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

        List<Reference> alreadyReturned = [];

        await foreach (var schemaRef in ssp.GetAll(cancellationToken))
            await foreach (var (reference, _) in tsp.FindByPartialNameAsync(schemaRef.reference.Guid, guidOrNamePart, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                if (alreadyReturned.Contains(reference))
                    continue;

                alreadyReturned.Add(reference);
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
                AmbientErrorContext.Provider.LogWarning($"Found property {truePropertyName} ({escapedPropKey}) on thing, but it doesn't appear on schema {schema.Name} ({schema.Guid}).");
                escapedPropKey = truePropertyName.Contains(' ') && !truePropertyName.StartsWith('[') && !truePropertyName.EndsWith(']') ? $"[{truePropertyName}]" : truePropertyName;
                fullDisplayName = escapedPropKey; // b0c1592e-5d79-4fe4-8814-aa6e534d2b7f.phone
                simpleDisplayName = truePropertyName; // b0c1592e-5d79-4fe4-8814-aa6e534d2b7f.phone
            }

            return (escapedPropKey, fullDisplayName, simpleDisplayName);
        }
        else
        {
            var escapedPropKey = truePropertyName.Contains(' ') && !truePropertyName.StartsWith('[') && !truePropertyName.EndsWith(']') ? $"[{truePropertyName}]" : truePropertyName;
            var fullDisplayName = escapedPropKey.Contains(' ') && escapedPropKey.StartsWith('[') && escapedPropKey.EndsWith(']') ? escapedPropKey[1..^1] : escapedPropKey;
            var simpleDisplayName = fullDisplayName;
            return (escapedPropKey, fullDisplayName, simpleDisplayName);
        }
    }

    public bool TryAddProperty(string propertyyName, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyyName);
        return Properties.TryAdd(propertyyName, value);
    }

    public bool TryRemoveProperty(string propertyyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyyName);
        return Properties.Remove(propertyyName);
    }

    public async IAsyncEnumerable<ThingProperty> GetProperties(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        MarkAccessed();

        if (Properties == null || Properties.Count == 0)
            yield break;

        // Does this thing adhere to any schemas?
        List<Schema> thingSchemas = [];
        await foreach (var schema in GetAssociatedSchemas(cancellationToken))
        {
            thingSchemas.Add(schema);
        }

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

    public async Task ComputeCalculatedProperties(CancellationToken cancellationToken)
    {
        // Does this thing adhere to any schemas?
        List<Schema> thingSchemas = [];
        await foreach (var schema in GetAssociatedSchemas(cancellationToken))
            thingSchemas.Add(schema);

        var unsetProperties = (await GetUnsetProperties(cancellationToken))
            .ToDictionary(k => $"{k.SchemaGuid}.{k.SimpleDisplayName}", v => (object?)null);

        var allProperties = new Dictionary<string, object?>();
        foreach (var setProperty in Properties)
            allProperties.Add(setProperty.Key, setProperty.Value);
        foreach (var unsetProperty in unsetProperties)
            allProperties.Add(unsetProperty.Key, null);

        var changedProperties = new Dictionary<string, object?>();
        foreach (var thingProp in allProperties)
        {
            // Does this property belong to a schema?
            bool valid = true;
            var schema = thingSchemas.FirstOrDefault(s => thingProp.Key.StartsWith($"{s.Guid}."));
            var (escapedPropKey, fullDisplayName, simpleDisplayName) = CarvePropertyName(thingProp.Key, schema);
            var required = false;
            var schemaFieldType = default(string?);
            if (schema == default)
                continue; // If the schema was deleted, ignore this field.

            // Watch out, the schema field could have been deleted but it's still there on the instance.
            if (schema.Properties.TryGetValue(simpleDisplayName, out SchemaFieldBase? schemaField))
            {
                valid = schemaField == null || await schemaField.IsValidAsync(thingProp.Value, cancellationToken); // Valid if no schema.
                required = schemaField != null && schemaField.Required;
                schemaFieldType = schemaField?.Type;
            }

            if (schemaFieldType == null
                || schemaFieldType.CompareTo(SchemaCalculatedField.SCHEMA_FIELD_TYPE) != 0
                || schemaField is not SchemaCalculatedField calcField)
                continue; // Not a calculate field.

            if (string.IsNullOrWhiteSpace(calcField.Formula))
            {
                AmbientErrorContext.Provider.LogWarning($"Calculated field {calcField.Name} on {schema.Name} is missing its formula");
                continue;
            }

            var result = Parser.Calculate(calcField.Formula, this);
            if (result.IsError)
            {
                var carved = CarvePropertyName(thingProp.Key, schema);
                AmbientErrorContext.Provider.LogWarning($"Unable to calculate field {carved.fullDisplayName}: {result.Message}");

            }
            else if ((thingProp.Value == null && result.Result != null)
                || (thingProp.Value != null && !thingProp.Value.Equals(result.Result)))
                changedProperties.Add(thingProp.Key, result.Result);
        }

        if (changedProperties.Count > 0)
        {
            foreach (var changed in changedProperties)
                if (changed.Value == null)
                    Properties.Remove(changed.Key);
                else
                    Properties[changed.Key] = changed.Value;

            await SaveAsync(cancellationToken);
        }
    }

    public async Task<List<ThingUnsetProperty>> GetUnsetProperties(CancellationToken cancellationToken)
    {
        // Does this thing adhere to any schemas?
        List<Schema> thingSchemas = [];
        await foreach (var schema in GetAssociatedSchemas(cancellationToken))
            thingSchemas.Add(schema);

        var unsetSchemaFields = thingSchemas
            .Select(s => new { Schema = s, s.Properties })
            .SelectMany(s => s.Properties
                //.Where(p => p.Value.Type.CompareTo(SchemaCalculatedField.SCHEMA_FIELD_TYPE) != 0) // Calculated properties are never 'unset'.
                .Select(p => (s, s.Schema.Guid, s.Schema.Name, p.Key)))
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

        return [.. unsetSchemaFields.Values];
    }

    public async IAsyncEnumerable<ThingProperty> GetPropertyByName(string propName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        MarkAccessed();

        await foreach (var prop in GetProperties(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (string.Compare(propName, prop.TruePropertyName, StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                // For instance: c9882fca-62ed-4456-8dbb-231ae518a410.[Work Phone]
                yield return prop;
            }
            else if (string.Compare(propName, prop.FullDisplayName, StringComparison.CurrentCultureIgnoreCase) == 0
                && string.Compare(propName, nameof(Schema.Plural), StringComparison.OrdinalIgnoreCase) != 0 // Ignore schema built-in
            )
            {
                // For instance: vendor.[Work Phone]
                yield return prop;
            }
            else if (string.Compare(propName, prop.SimpleDisplayName, StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                // For instance: [Work Phone]
                yield return prop;
            }
        }
    }

    public async Task<ThingSetResult> Set(
        string propName,
        string? propValue,
        CancellationToken cancellationToken,
        Func<string, IEnumerable<PossibleNameMatch>, PossibleNameMatch>? chooserHandler = null
        )
    {
        MarkAccessed();

        // If prop name came in unescaped, and it should be escaped, then escape it here for comparisons.
        if (propName.Contains(' ') && !propName.StartsWith('[') && !propName.EndsWith(']'))
            propName = $"[{propName}]";

        // Is this property alerady set?

        // Step 1, Check EXISTING properties on this thing.
        var existingProperties = GetPropertyByName(propName, cancellationToken).ToBlockingEnumerable(cancellationToken).ToFrozenSet();

        // Step 2, Check properties on associated schemas NOT already set on this object
        Dictionary<string, object?> massagedPropValues = [];
        //object? massagedPropValue = propValue; // comment

        var associatedSchemas = GetAssociatedSchemas(cancellationToken).ToBlockingEnumerable().ToArray();

        // Massage values using schema field methods.
        var x = associatedSchemas.SelectMany(s =>
            s.Properties.Where(p => string.CompareOrdinal(p.Key, propName) == 0)
            .Select(p => new { SchemaGuid = s.Guid, PropertyName = p.Key, Field = p.Value }))
            .ToArray();
        foreach (var y in x)
            if (y.Field.TryMassageInput(propValue, out object? prePossibleMassaged))
                massagedPropValues.Add($"{y.SchemaGuid}.{y.PropertyName}", prePossibleMassaged);

        List<ThingProperty> candidateProperties = [.. existingProperties];

        foreach (var schema in associatedSchemas)
        {
            if (cancellationToken.IsCancellationRequested)
                return new ThingSetResult(false);

            foreach (var schemaProperty in schema.Properties)
            {
                var truePropertyName = $"{schema.Guid}.{schemaProperty.Key}";

                // Does this schema field massage the string input?
                var canMassage = schemaProperty.Value.TryMassageInput(propValue, out object? possibleMassagedPropValue);

                // If this value was for this property, would it be valid?
                var wouldBeValid = canMassage && await schemaProperty.Value.IsValidAsync(possibleMassagedPropValue, cancellationToken);

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
                                string.CompareOrdinal(schemaProperty.Value.Type, SchemaRefField.SCHEMA_FIELD_TYPE) == 0
                                    ? $"{SchemaRefField.SCHEMA_FIELD_TYPE}.{((SchemaRefField)schemaProperty.Value).SchemaGuid}"
                                    : schemaProperty.Value.Type,
                            SchemaName = candidateProperties[i].SchemaName
                        };
                        candidatesMatch = true;
                        //massagedPropValue = possibleMassagedPropValue;
                    }
                }
                if (candidatesMatch)
                    continue;// Already set, no need to add a phantom.

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
                        string.CompareOrdinal(schemaProperty.Value.Type, SchemaRefField.SCHEMA_FIELD_TYPE) == 0
                            ? $"{SchemaRefField.SCHEMA_FIELD_TYPE}.{((SchemaRefField)schemaProperty.Value).SchemaGuid}"
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
                    if (propValue == null || string.IsNullOrWhiteSpace(propValue.ToString()))
                        Properties.Remove(propName);
                    else
                        Properties[propName] = propValue;

                    // Special case for Name.
                    if (string.Compare(propName, nameof(Name), StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (propValue == null || string.IsNullOrWhiteSpace(propValue.ToString()))
                        {
                            AmbientErrorContext.Provider.LogError($"Value of {nameof(Name)} cannot be empty.");
                            return new ThingSetResult(false);
                        }
                        Name = propValue.ToString()!;
                    }

                    var saved = await SaveAsync(cancellationToken);
                    return new ThingSetResult(saved);
                }
            case 1:
                // Exactly one, we need to update:

                var massagedPropValue = massagedPropValues
                    .Where(m => string.CompareOrdinal(m.Key, candidateProperties[0].TruePropertyName) == 0)
                    .Select(m => m.Value)
                    .FirstOrDefault(propValue);

                // Unset (null it out) case
                if (massagedPropValue == null || string.IsNullOrWhiteSpace(massagedPropValue.ToString()))
                {
                    if (Properties.Remove(candidateProperties[0].TruePropertyName)
                        && candidateProperties[0].Required)
                        AmbientErrorContext.Provider.LogWarning($"Required {propName} was removed.");

                    var saved = await SaveAsync(cancellationToken);
                    return new ThingSetResult(saved);
                }

                if (!candidateProperties[0].Valid)
                {
                    if (chooserHandler != null
                        && candidateProperties[0].SchemaGuid != null
                        && (candidateProperties[0].SchemaFieldType?.StartsWith(SchemaRefField.SCHEMA_FIELD_TYPE) ?? false))
                    {
                        var remoteSchemaGuid = candidateProperties[0].SchemaFieldType![(SchemaRefField.SCHEMA_FIELD_TYPE.Length + 1)..];

                        var disambig = (massagedPropValue == null || massagedPropValue.ToString() == null)
                            ? []
                            : tsp.FindByPartialNameAsync(remoteSchemaGuid, massagedPropValue.ToString()!, cancellationToken)
                                .ToBlockingEnumerable(cancellationToken)
                                .Select(p => new PossibleNameMatch(p.reference, p.name))
                                .ToArray();

                        if (disambig.Length == 1)
                        {
                            Properties[candidateProperties[0].TruePropertyName] = disambig[0].Reference.Guid;
                            AmbientErrorContext.Provider.LogInfo($"Set {propName} to {disambig[0].Name}.");
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
                            AmbientErrorContext.Provider.LogWarning($"Value of {propName} is invalid.");
                            var saved = await SaveAsync(cancellationToken);
                            return new ThingSetResult(saved);
                        }
                    }
                    else
                    {
                        Properties[candidateProperties[0].TruePropertyName] = massagedPropValue;
                        AmbientErrorContext.Provider.LogWarning($"Value of {propName} is invalid.");
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
                            AmbientErrorContext.Provider.LogError($"Value of {nameof(Name)} cannot be empty.");
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
                AmbientErrorContext.Provider.LogError($"Unable to determine which property between {candidateProperties.Select(x => x.TruePropertyName).Aggregate((c, n) => $"{c}, {n}")} to update.");
                return new ThingSetResult(false);
        }

    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (provider == null)
            return false;

        var success = await provider.SaveAsync(this, cancellationToken);
        MarkModified();
        return success;
    }

    public async Task<(bool, Thing?)> AssociateWithSchemaAsync(string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        var provider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (provider == null)
            return (false, null);

        var success = await provider.AssociateWithSchemaAsync(Guid, schemaGuid, cancellationToken);
        return success;
    }

    public async Task<(bool, Thing?)> DissociateFromSchemaAsync(string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        var provider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (provider == null)
            return (false, null);

        var success = await provider.DissociateFromSchemaAsync(Guid, schemaGuid, cancellationToken);
        return success;
    }

    public async Task<bool> DeleteAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (provider == null)
            return false;

        var success = await provider.DeleteAsync(Guid, cancellationToken);
        MarkModified();
        return success;
    }

    public void MarkModified()
    {
        LastModified = DateTime.UtcNow;
        LastAccessed = LastModified;
    }
    public void MarkAccessed() => LastAccessed = DateTime.UtcNow;

    public override string ToString() => Name;
}
