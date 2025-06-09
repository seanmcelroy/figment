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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Figment.Common.Calculations.Parsing;
using Figment.Common.Data;
using Figment.Common.Errors;

namespace Figment.Common;

/// <summary>
/// A thing is the core object of Figment, which represents an entity which implements
/// one or more schemas.
/// </summary>
public class Thing
{
    private string name;

    /// <summary>
    /// Initializes a new instance of the <see cref="Thing"/> class.
    /// </summary>
    /// <param name="guid">Globally unique identifier for the thing.</param>
    /// <param name="newName">Name of the thing.</param>
    public Thing(string guid, string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guid, nameof(guid));
        ArgumentException.ThrowIfNullOrWhiteSpace(newName, nameof(newName));

        if (!IsThingNameValid(newName))
        {
            throw new ArgumentException($"Name '{newName}' is not valid for things.", nameof(newName));
        }

        Guid = guid;
        name = newName;
    }

    /// <summary>
    /// Gets the globally unique identifier for the thing.
    /// </summary>
    public string Guid { get; init; }

    /// <summary>
    /// Gets or sets the name of the thing.
    /// </summary>
    public string Name
    {
        get => name;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Name));

            if (!IsThingNameValid(value))
            {
                throw new ArgumentException($"Name '{value}' is not valid for things.", nameof(Name));
            }

            MarkDirty();
            name = value;
        }
    }

    /// <summary>
    /// Gets or sets the unique identifiers of the <see cref="Schema"/> associated with this thing.
    /// </summary>
    public List<string> SchemaGuids { get; set; } = [];

    /// <summary>
    /// Gets the list of properties, keyed by name, defined for this thing.
    /// </summary>
    /// <remarks>
    /// Do not use this outside of this class.  Left public for serialization only.
    /// </remarks>
    public Dictionary<string, object> Properties { get; init; } = []; // DO NOT MAKE PRIVATE.

    /// <summary>
    /// Gets or sets the date this thing was created.
    /// </summary>
    [JsonIgnore]
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the date things thing was last modified.
    /// </summary>
    [JsonIgnore]
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the date things thing was last accessed.
    /// </summary>
    [JsonIgnore]
    public DateTime LastAccessed { get; set; }

    /// <summary>
    /// Gets a value indicating whether this object has been changed since it was loaded.
    /// </summary>
    [JsonIgnore]
    public bool IsDirty { get; private set; }

    /// <summary>
    /// Retrieves an enumeration of references for <see cref="Thing"/> instances that have a
    /// <see cref="Guid"/> or <see cref="Name"/> matching <paramref name="guidOrNamePart"/>.
    /// </summary>
    /// <param name="guidOrNamePart">The <see cref="Guid"/> or <see cref="Name"/> to search for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumeration of <see cref="Thing"/> instances with a matching
    /// <see cref="Guid"/> or <see cref="Name"/>.</returns>
    public static async IAsyncEnumerable<Reference> ResolveAsync(
        string guidOrNamePart,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guidOrNamePart);

        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            yield break;
        }

        if (await tsp.GuidExists(guidOrNamePart, cancellationToken))
        {
            yield return new Reference
            {
                Guid = guidOrNamePart,
                Type = Reference.ReferenceType.Thing,
            };
            yield break;
        }

        // Nope, so GLOBAL name searching...
        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (ssp == null)
        {
            yield break;
        }

        List<Reference> alreadyReturned = [];

        await foreach (var schemaRef in ssp.GetAll(cancellationToken))
        {
            await foreach (var (reference, _) in tsp.FindByPartialNameAsync(schemaRef.Reference.Guid, guidOrNamePart, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                if (alreadyReturned.Contains(reference))
                {
                    continue;
                }

                alreadyReturned.Add(reference);
                yield return reference;
            }
        }
    }

    /// <summary>
    /// An enumeration of <see cref="Schema"/> instances that are associated with this <see cref="Thing"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asychronous enumeration of <see cref="Schema"/> instances associated with this thing.</returns>
    public async IAsyncEnumerable<Schema> GetAssociatedSchemas([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Does this thing adhere to any schemas?
        if (SchemaGuids?.Count > 0)
        {
            var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
            if (ssp == null)
            {
                yield break;
            }

            foreach (var schemaGuid in SchemaGuids.Distinct())
            {
                if (!string.IsNullOrWhiteSpace(schemaGuid))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        yield break;
                    }

                    var schema = await ssp.LoadAsync(schemaGuid, cancellationToken);
                    if (schema != null)
                    {
                        yield return schema;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Parses the true property name stored for a thing into various versions.
    /// </summary>
    /// <param name="truePropertyName">The true property name.</param>
    /// <param name="schema">The schema to which the property belongs, if any.</param>
    /// <returns>
    /// <para>
    /// The escaped property name is the version without a schema prefix,
    /// but which is encased in brackets if it contains spaces.
    /// </para>
    /// <para>
    /// The full display name is the schema name (encased in brackets if it contains spaces),
    /// a period separator, and then the escaped property name.  The schema could have
    /// been subsequently deleted, in which case the full display name is the same as
    /// the escaped property name.
    /// </para>
    /// <para>
    /// The simple property name is always just the property name without any schema
    /// prefix or any bracketing.
    /// </para>
    /// </returns>
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
            if (!schema.Properties.TryGetValue(choppedPropName, out SchemaFieldBase? _))
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

    /// <summary>
    /// Attempts to add a property to a thing by name and value.
    /// </summary>
    /// <param name="propertyName">The name of the property to add.</param>
    /// <param name="value">The value of the property.</param>
    /// <returns>A value indicating whether or not the property was added.</returns>
    public bool TryAddProperty(string propertyName, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        MarkDirty();
        return Properties.TryAdd(propertyName, value);
    }

    /// <summary>
    /// Attempts to remove a property from a thing by the property's name.
    /// </summary>
    /// <param name="propertyName">The name of the property to remove.</param>
    /// <returns>A value indicating whether or not the property was removed.</returns>
    public bool TryRemoveProperty(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        MarkDirty();
        return Properties.Remove(propertyName);
    }

    /// <summary>
    /// Gets an asyncronous enumerable that iterats over the properties of the thing.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerator for each <see cref="ThingProperty"/>.</returns>
    public async IAsyncEnumerable<ThingProperty> GetProperties(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        MarkAccessed();

        if (Properties == null || Properties.Count == 0)
        {
            yield break;
        }

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
                    required = schemaField?.Required == true;
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
                SchemaName = schema?.Name,
            };
        }
    }

    /// <summary>
    /// Recalculates all <see cref="SchemaCalculatedField"/> field values on any
    /// associated schemas.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A take for asynchronous processing.</returns>
    public async Task ComputeCalculatedProperties(CancellationToken cancellationToken)
    {
        // Does this thing adhere to any schemas?
        List<Schema> thingSchemas = [];
        await foreach (var schema in GetAssociatedSchemas(cancellationToken))
        {
            thingSchemas.Add(schema);
        }

        var unsetProperties = (await GetUnsetProperties(cancellationToken))
            .ToDictionary(k => $"{k.SchemaGuid}.{k.SimpleDisplayName}", v => (object?)null);

        var allProperties = new Dictionary<string, object?>();
        foreach (var setProperty in Properties)
        {
            allProperties.Add(setProperty.Key, setProperty.Value);
        }

        foreach (var unsetProperty in unsetProperties)
        {
            allProperties.Add(unsetProperty.Key, null);
        }

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
            {
                continue; // If the schema was deleted, ignore this field.
            }

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
            {
                continue; // Not a calculate field.
            }

            if (string.IsNullOrWhiteSpace(calcField.Formula))
            {
                AmbientErrorContext.Provider.LogWarning($"Calculated field {calcField.Name} on {schema.Name} is missing its formula");
                continue;
            }

            var xp = new ExpressionParser();
            var ast = xp.Parse(calcField.Formula);
            var context = new EvaluationContext(this);
            var result = ast.Evaluate(context);

            if (!result.IsSuccess)
            {
                var carved = CarvePropertyName(thingProp.Key, schema);
                AmbientErrorContext.Provider.LogWarning($"Unable to calculate field {carved.fullDisplayName}: {result.Message}");
            }
            else if ((thingProp.Value == null && result.Result != null)
                || (thingProp.Value?.Equals(result.Result) == false))
            {
                changedProperties.Add(thingProp.Key, result.Result);
                MarkDirty();
            }
        }

        if (changedProperties.Count > 0)
        {
            foreach (var changed in changedProperties)
            {
                if (changed.Value == null)
                {
                    Properties.Remove(changed.Key);
                }
                else
                {
                    Properties[changed.Key] = changed.Value;
                }
            }

            await SaveAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Gets all properties defined for this thing which are not set.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of unset properties.</returns>
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
            .SelectMany(s => s.Properties

                // .Where(p => p.Value.Type.CompareTo(SchemaCalculatedField.SCHEMA_FIELD_TYPE) != 0) // Calculated properties are never 'unset'.
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
                        Field = v.s.Properties[v.Key],
                    };
                });

        await foreach (var thingProperty in GetProperties(cancellationToken))
        {
            if (thingProperty.SchemaGuid != null)
            {
                // Remove from list of schema properties once we note it's set on the thing.
                _ = unsetSchemaFields.Remove((thingProperty.SchemaGuid, thingProperty.SimpleDisplayName));
            }
        }

        return [.. unsetSchemaFields.Values];
    }

    /// <summary>
    /// Retrieves a property by its <see cref="ThingProperty.TruePropertyName"/> name.
    /// </summary>
    /// <param name="truePropertyName">The true name of the properties to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The property, if found by its <see cref="ThingProperty.TruePropertyName"/>.  The value is null if the requested true property name was not found.</returns>
    public async Task<ThingProperty?> GetPropertyByTrueNameAsync(string truePropertyName, CancellationToken cancellationToken)
    {
        var dict = await GetPropertiesByTrueNameAsync([truePropertyName], cancellationToken);
        if (dict.Count != 1)
        {
            return null;
        }

        return dict.Single().Value;
    }

    /// <summary>
    /// Retrieves each property by the property's <see cref="ThingProperty.TruePropertyName"/> name.
    /// </summary>
    /// <param name="truePropertyNames">The true names of the properties to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The a dictionary of property values, keyed by the <paramref name="truePropertyNames"/> true names, if found by its <see cref="ThingProperty.TruePropertyName"/>.  The value is null if the requested true property name was not found.</returns>
    [return: NotNull]
    public async Task<Dictionary<string, ThingProperty?>> GetPropertiesByTrueNameAsync(string[] truePropertyNames, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(truePropertyNames);

        MarkAccessed();

        // Validate property names upfront
        foreach (var truePropName in truePropertyNames)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(truePropName, nameof(truePropertyNames));
            if (!truePropName.Contains('.'))
            {
                throw new ArgumentException($"True property name '{truePropName}' does not contain a period.", nameof(truePropertyNames));
            }
        }

        var result = new Dictionary<string, ThingProperty?>();
        var searchSet = new HashSet<string>(truePropertyNames, StringComparer.InvariantCultureIgnoreCase);

        // Stream through properties without materializing the entire collection
        await foreach (var prop in GetProperties(cancellationToken))
        {
            if (searchSet.Contains(prop.TruePropertyName))
            {
                result[prop.TruePropertyName] = prop;
                searchSet.Remove(prop.TruePropertyName);

                // Early exit if we've found all requested properties
                if (searchSet.Count == 0)
                {
                    break;
                }
            }
        }

        // Add null entries for properties that weren't found
        foreach (var missingPropName in searchSet)
        {
            result[missingPropName] = null;
        }

        return result;
    }

    /// <summary>
    /// Retrieves each property by the property name.
    /// Because each <see cref="Thing"/> may have multiple properties that have the
    /// same name, but are defined by varying schema, this can return zero to many
    /// results for any given <paramref name="propName"/>.
    /// </summary>
    /// <param name="propName">The name of the property to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerator for each <see cref="ThingProperty"/>.</returns>
    public async IAsyncEnumerable<ThingProperty> GetPropertyByName(string propName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        MarkAccessed();

        await foreach (var prop in GetProperties(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            if (string.Equals(propName, prop.TruePropertyName, StringComparison.CurrentCultureIgnoreCase))
            {
                // For instance: c9882fca-62ed-4456-8dbb-231ae518a410.[Work Phone]
                yield return prop;
            }
            else if (string.Equals(propName, prop.FullDisplayName, StringComparison.CurrentCultureIgnoreCase)
                && !string.Equals(propName, nameof(Schema.Plural), StringComparison.OrdinalIgnoreCase) // Ignore schema built-in
            )
            {
                // For instance: vendor.[Work Phone]
                yield return prop;
            }
            else if (string.Equals(propName, prop.SimpleDisplayName, StringComparison.CurrentCultureIgnoreCase))
            {
                // For instance: [Work Phone]
                yield return prop;
            }
        }
    }

    /// <summary>
    /// Sets the values of properties.
    /// </summary>
    /// <param name="properties">The names and values of the properties to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="chooserHandler">The function that selects which entity to use when setting a property which is a reference, when multiple entities are located by the given property name.</param>
    /// <returns>The result of this operation.</returns>
    public async Task<ThingSetResult> Set(
        Dictionary<string, object?> properties,
        CancellationToken cancellationToken,
        Func<string, IEnumerable<PossibleNameMatch>, PossibleNameMatch>? chooserHandler = null)
    {
        ArgumentNullException.ThrowIfNull(properties);

        foreach (var (propName, _) in properties)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propName, nameof(propName));

            // Check if the property name is valid.
            if (!ThingProperty.IsPropertyNameValid(propName, out string? message))
            {
                return new ThingSetResult(false, [message]);
            }
        }

        MarkAccessed();

        // Reusable values across each property set.
        var associatedSchemas = new List<Schema>();
        await foreach (var schema in GetAssociatedSchemas(cancellationToken))
        {
            associatedSchemas.Add(schema);
        }

        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (ssp == null)
        {
            return new ThingSetResult(false, [AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER]);
        }

        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            return new ThingSetResult(false, [AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER]);
        }

        // Is this property already set?

        // Step 1, Check EXISTING properties on this thing.
        var messages = new List<string>();
        foreach (var (propName, propValue) in properties)
        {
            var existingProperties = new List<ThingProperty>();
            await foreach (var prop in GetPropertyByName(propName, cancellationToken))
            {
                existingProperties.Add(prop);
            }

            // Step 2, Check properties on associated schemas NOT already set on this object
            Dictionary<string, object?> massagedPropValues = [];

            // Massage values using schema field methods.
            var schemaPropertyMatches = associatedSchemas.SelectMany(s =>
                s.Properties.Where(p => string.Equals(p.Key, propName, StringComparison.Ordinal))
                .Select(p => new { SchemaGuid = s.Guid, PropertyName = p.Key, Field = p.Value }))
                .ToArray();
            foreach (var match in schemaPropertyMatches)
            {
                if (match.Field.TryMassageInput(propValue, out object? prePossibleMassaged))
                {
                    massagedPropValues.Add($"{match.SchemaGuid}.{match.PropertyName}", prePossibleMassaged);
                }
            }

            // Use dictionary for O(1) lookups and maintain index mapping
            var candidatePropertiesDict = new Dictionary<string, int>(StringComparer.Ordinal);
            var candidateProperties = new List<ThingProperty>(existingProperties);

            // Build index mapping for existing properties
            for (int i = 0; i < candidateProperties.Count; i++)
            {
                candidatePropertiesDict[candidateProperties[i].TruePropertyName] = i;
            }

            // Process only matching schema properties to reduce iterations
            foreach (var schema in associatedSchemas)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    messages.Add("Operation canceled.");
                    return new ThingSetResult(false, [.. messages]);
                }

                // Filter schema properties that match the property name upfront
                var matchingSchemaProperties = schema.Properties.Where(p =>
                    string.Equals(p.Key, propName, StringComparison.CurrentCultureIgnoreCase) ||
                    string.Equals($"[{p.Key}]", propName, StringComparison.CurrentCultureIgnoreCase) ||
                    string.Equals($"{schema.EscapedName}.{p.Key}", propName, StringComparison.CurrentCultureIgnoreCase) ||
                    string.Equals($"{schema.EscapedName}.[{p.Key}]", propName, StringComparison.CurrentCultureIgnoreCase))
                .ToArray();

                foreach (var schemaProperty in matchingSchemaProperties)
                {
                    var truePropertyName = $"{schema.Guid}.{schemaProperty.Key}";

                    // Does this schema field massage the string input?
                    var canMassage = schemaProperty.Value.TryMassageInput(propValue, out object? possibleMassagedPropValue);

                    // If this value was for this property, would it be valid?
                    var wouldBeValid = canMassage && await schemaProperty.Value.IsValidAsync(possibleMassagedPropValue, cancellationToken);

                    // Use dictionary lookup for O(1) access
                    if (candidatePropertiesDict.TryGetValue(truePropertyName, out var existingIndex))
                    {
                        // Update existing property in-place using index
                        var existingProperty = candidateProperties[existingIndex];
                        candidateProperties[existingIndex] = new ThingProperty
                        {
                            TruePropertyName = existingProperty.TruePropertyName,
                            FullDisplayName = existingProperty.FullDisplayName,
                            SimpleDisplayName = existingProperty.SimpleDisplayName,
                            SchemaGuid = existingProperty.SchemaGuid,
                            Value = existingProperty.Value,
                            Valid = wouldBeValid,
                            Required = schemaProperty.Value.Required,
                            SchemaFieldType = string.Equals(schemaProperty.Value.Type, SchemaRefField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal)
                                ? $"{SchemaRefField.SCHEMA_FIELD_TYPE}.{((SchemaRefField)schemaProperty.Value).SchemaGuid}"
                                : schemaProperty.Value.Type,
                            SchemaName = existingProperty.SchemaName,
                        };
                        continue;
                    }

                    // Create phantom property only if name matches
                    var fullDisplayName = $"{schema.EscapedName}.{schemaProperty.Key}";
                    var simpleDisplayName = schemaProperty.Key.Contains(' ') && !schemaProperty.Key.StartsWith('[') && !schemaProperty.Key.EndsWith(']') ? $"[{schemaProperty.Key}]" : schemaProperty.Key;

                    if ((string.Equals(propName, fullDisplayName, StringComparison.CurrentCultureIgnoreCase) ||
                         string.Equals(propName, simpleDisplayName, StringComparison.CurrentCultureIgnoreCase)) &&
                        !string.Equals(propName, "plural", StringComparison.OrdinalIgnoreCase))
                    {
                        var phantomProp = new ThingProperty
                        {
                            TruePropertyName = truePropertyName,
                            FullDisplayName = fullDisplayName,
                            SimpleDisplayName = simpleDisplayName,
                            SchemaGuid = schema.Guid,
                            Value = null,
                            Valid = wouldBeValid,
                            Required = schemaProperty.Value.Required,
                            SchemaFieldType = string.Equals(schemaProperty.Value.Type, SchemaRefField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal)
                                ? $"{SchemaRefField.SCHEMA_FIELD_TYPE}.{((SchemaRefField)schemaProperty.Value).SchemaGuid}"
                                : schemaProperty.Value.Type,
                            SchemaName = schema.Name,
                        };
                        candidateProperties.Add(phantomProp);
                        candidatePropertiesDict[truePropertyName] = candidateProperties.Count - 1;
                    }
                }
            }

            MarkDirty();

            switch (candidateProperties.Count)
            {
                case 0:
                    {
                        // No existing property by this name on the thing (nor in any associated schema), so we're going to add it.
                        if (propValue == null || (propValue is string pvs && string.IsNullOrWhiteSpace(pvs)))
                        {
                            Properties.Remove(propName);
                        }
                        else
                        {
                            Properties[propName] = propValue;
                        }

                        // Special case for Name.
                        if (string.Equals(propName, nameof(Name), StringComparison.OrdinalIgnoreCase))
                        {
                            if (propValue == null || propValue is not string pvs2 || string.IsNullOrWhiteSpace(pvs2))
                            {
                                AmbientErrorContext.Provider.LogError($"Value of {nameof(Name)} cannot be empty.");
                                messages.Add($"Value of {nameof(Name)} cannot be empty.");
                                return new ThingSetResult(false, [.. messages]);
                            }

                            Name = pvs2;
                            continue;

                            // return new ThingSetResult(true, $"Property {nameof(Name)} set to '{propValue}'");
                        }
                        else
                        {
                            continue;

                            // return new ThingSetResult(true, $"Property {propName} set to '{propValue}'");
                        }
                    }

                case 1:
                    // Exactly one, we need to update:
                    var massagedPropValue = massagedPropValues
                        .Where(m => string.Equals(m.Key, candidateProperties[0].TruePropertyName, StringComparison.Ordinal))
                        .Select(m => m.Value)
                        .FirstOrDefault(propValue);

                    // Unset (null it out) case
                    if (massagedPropValue == null || string.IsNullOrWhiteSpace(massagedPropValue.ToString()))
                    {
                        if (Properties.Remove(candidateProperties[0].TruePropertyName)
                            && candidateProperties[0].Required)
                        {
                            AmbientErrorContext.Provider.LogWarning($"Required {propName} was removed.");
                        }

                        continue;

                        // return new ThingSetResult(true, $"Property {candidateProperties[0].TruePropertyName} value was removed.");
                    }

                    if (!candidateProperties[0].Valid)
                    {
                        // This is for name resolution of "schema" fields.
                        if (chooserHandler != null
                            && candidateProperties[0].SchemaGuid != null
                            && (candidateProperties[0].SchemaFieldType?.StartsWith(SchemaSchemaField.SCHEMA_FIELD_TYPE) ?? false))
                        {
                            var disambig = new List<PossibleNameMatch>();
                            if (massagedPropValue?.ToString() != null)
                            {
                                await foreach (var match in ssp.FindByPartialNameAsync(massagedPropValue.ToString()!, cancellationToken))
                                {
                                    disambig.Add(match);
                                }
                            }

                            if (disambig.Count == 1)
                            {
                                Properties[candidateProperties[0].TruePropertyName] = disambig[0].Reference.Guid;
                                AmbientErrorContext.Provider.LogInfo($"Set {propName} to {disambig[0].Reference.Guid}.");
                                continue;

                                // return new ThingSetResult(true, $"Property {propName} set to '{disambig[0].Reference.Guid}'");
                            }
                            else if (disambig.Count > 1)
                            {
                                var which = chooserHandler(
                                    $"There was more than one schema matching '{propValue}'.  Which do you want to select?",
                                    disambig);

                                Properties[candidateProperties[0].TruePropertyName] = which.Reference.Guid;
                                continue;

                                // return new ThingSetResult(true, $"Property {candidateProperties[0].TruePropertyName} set to '{which.Reference.Guid}'");
                            }
                            else
                            {
                                if (massagedPropValue == null)
                                {
                                    Properties.Remove(candidateProperties[0].TruePropertyName);
                                    continue;

                                    // return new ThingSetResult(true, $"Property {candidateProperties[0].TruePropertyName} was removed.");
                                }
                                else
                                {
                                    Properties[candidateProperties[0].TruePropertyName] = massagedPropValue;
                                    continue;

                                    // return new ThingSetResult(true, $"Property {candidateProperties[0].TruePropertyName} set to '{massagedPropValue}'");
                                }
                            }
                        }
                        else if (chooserHandler != null
                            && candidateProperties[0].SchemaGuid != null
                            && (candidateProperties[0].SchemaFieldType?.StartsWith(SchemaRefField.SCHEMA_FIELD_TYPE) ?? false))
                        {
                            // This is for name resolution of "ref" fields.
                            var remoteSchemaGuid = candidateProperties[0].SchemaFieldType![(SchemaRefField.SCHEMA_FIELD_TYPE.Length + 1)..];

                            var disambig = new List<PossibleNameMatch>();
                            if (massagedPropValue?.ToString() != null)
                            {
                                await foreach (var match in tsp.FindByPartialNameAsync(remoteSchemaGuid, massagedPropValue.ToString()!, cancellationToken))
                                {
                                    disambig.Add(match);
                                }
                            }

                            if (disambig.Count == 1)
                            {
                                Properties[candidateProperties[0].TruePropertyName] = disambig[0].Reference.Guid;
                                AmbientErrorContext.Provider.LogInfo($"Set {propName} to {disambig[0].Name}.");
                                continue;

                                // return new ThingSetResult(true, $"Property {candidateProperties[0].TruePropertyName} set to '{disambig[0].Reference.Guid}'");
                            }
                            else if (disambig.Count > 1)
                            {
                                var which = chooserHandler(
                                    $"There was more than one {candidateProperties[0].SchemaName} matching '{propValue}'.  Which do you want to select?",
                                    disambig);

                                Properties[candidateProperties[0].TruePropertyName] = which.Reference.Guid;
                                continue;

                                // return new ThingSetResult(true, $"Property {candidateProperties[0].TruePropertyName} set to '{which.Reference.Guid}'");
                            }
                            else
                            {
                                if (massagedPropValue == null)
                                {
                                    Properties.Remove(candidateProperties[0].TruePropertyName);
                                    continue;

                                    // return new ThingSetResult(true, $"Property {candidateProperties[0].TruePropertyName} was removed.");
                                }
                                else
                                {
                                    Properties[candidateProperties[0].TruePropertyName] = massagedPropValue;
                                    continue;

                                    // return new ThingSetResult(true, $"Property {candidateProperties[0].TruePropertyName} set to '{massagedPropValue}'");
                                }
                            }
                        }
                        else
                        {
                            Properties[candidateProperties[0].TruePropertyName] = massagedPropValue;
                            AmbientErrorContext.Provider.LogWarning($"Value of {propName} is invalid.");
                            continue;

                            // return new ThingSetResult(true, $"Property {candidateProperties[0].TruePropertyName} set to '{massagedPropValue}'");
                        }
                    }

                    // Special case for Name.
                    {
                        if (string.Equals(propName, nameof(Name), StringComparison.OrdinalIgnoreCase))
                        {
                            if (massagedPropValue == null || string.IsNullOrWhiteSpace(massagedPropValue.ToString()))
                            {
                                AmbientErrorContext.Provider.LogError($"Value of {nameof(Name)} cannot be empty.");
                                messages.Add($"Value of {nameof(Name)} cannot be empty.");
                                return new ThingSetResult(false, [.. messages]);
                            }

                            Name = massagedPropValue.ToString()!;
                            continue;

                            // return new ThingSetResult(true, $"Property {nameof(Name)} set to '{massagedPropValue}'");
                        }
                        else
                        {
                            Properties[candidateProperties[0].TruePropertyName] = massagedPropValue;
                            continue;

                            // return new ThingSetResult(true, $"Property {candidateProperties[0].TruePropertyName} set to '{massagedPropValue}'");
                        }
                    }

                default:
                    // Ambiguous
                    var errorMessage = $"Unable to determine which property between {string.Join(", ", candidateProperties.Select(x => x.TruePropertyName))} to update.";
                    AmbientErrorContext.Provider.LogError(errorMessage);
                    messages.Add(errorMessage);
                    return new ThingSetResult(false, [.. messages]);
            }
        }

        return new ThingSetResult(true, [.. messages]);
    }

    /// <summary>
    /// Sets the value of a property.
    /// </summary>
    /// <param name="propName">The name of the property to update.</param>
    /// <param name="propValue">The value for the property.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="chooserHandler">The function that selects which entity to use when setting a property which is a reference, when multiple entities are located by the given property name.</param>
    /// <returns>The result of this operation.</returns>
    public async Task<ThingSetResult> Set(
        string propName,
        object? propValue,
        CancellationToken cancellationToken,
        Func<string, IEnumerable<PossibleNameMatch>, PossibleNameMatch>? chooserHandler = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propName, nameof(propName));

        return await Set(new Dictionary<string, object?> { { propName, propValue } }, cancellationToken, chooserHandler);
    }

    /// <summary>
    /// Attempts to save the thing to its underlying data store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the save attempt was successful.</returns>
    public async Task<(bool success, string? message)> SaveAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (provider == null)
        {
            return (false, AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
        }

        var saved = await provider.SaveAsync(this, cancellationToken);
        MarkModified();

        if (saved.success)
        {
            IsDirty = false;
        }

        return saved;
    }

    /// <summary>
    /// Associates this <see cref="Thing"/> with a <see cref="Schema"/>.
    /// </summary>
    /// <param name="schema">the <see cref="Schema"/> to which this <see cref="Thing"/> shall be associated.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task returning a <see cref="bool"/> indicating whether the operation was successful and an updated <see cref="Thing"/> loaded from the data store after the modification was made, if successful.</returns>
    public async Task<(bool, Thing?)> AssociateWithSchemaAsync(Schema schema, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var provider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (provider == null)
        {
            return (false, null);
        }

        MarkDirty();

        var success = await provider.AssociateWithSchemaAsync(Guid, schema, cancellationToken);
        MarkModified();
        return success;
    }

    /// <summary>
    /// Attempts to dissociate a <see cref="Schema"/> from this thing.
    /// </summary>
    /// <param name="schemaGuid">Unique identiifer of the schema that will be dissociated. </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether the operation was successful, and an updated <see cref="Thing"/> if it was successful.</returns>
    public async Task<(bool, Thing?)> DissociateFromSchemaAsync(string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        var provider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (provider == null)
        {
            return (false, null);
        }

        MarkDirty();

        var success = await provider.DissociateFromSchemaAsync(Guid, schemaGuid, cancellationToken);
        MarkModified();
        return success;
    }

    /// <summary>
    /// Attempts to delete this <see cref="Thing"/> from its underlying data store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the delete attempt was successful.</returns>
    public async Task<bool> DeleteAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (provider == null)
        {
            return false;
        }

        MarkDirty();

        var success = await provider.DeleteAsync(Guid, cancellationToken);
        MarkModified();
        return success;
    }

    /// <summary>
    /// Marks the thing as as dirty, that is, any changes to it since it was loaded.
    /// Once this thing is saved the dirty flag is cleared.  This is different than modified,
    /// which indicates an object is changed at all since it was last loaded.
    /// </summary>
    public void MarkDirty()
    {
        IsDirty = true;
    }

    /// <summary>
    /// Marks the thing as changed, updating both the <see cref="LastModified"/> and <see cref="LastAccessed"/> dates.
    /// </summary>
    public void MarkModified()
    {
        LastModified = DateTime.UtcNow;
        LastAccessed = LastModified;
    }

    /// <summary>
    /// Marks the thing as accessed, updating the <see cref="LastAccessed"/> date.
    /// </summary>
    public void MarkAccessed() => LastAccessed = DateTime.UtcNow;

    /// <summary>
    /// Returns the <see cref="Name"/> of this thing.
    /// </summary>
    /// <returns>The <see cref="Name"/> of this thing.</returns>
    public override string ToString() => Name;

    /// <summary>
    /// Determines whether a <see cref="Name"/> is considered valid when specified by a user.
    /// </summary>
    /// <param name="thingName">The proposed thing name to analyze.</param>
    /// <returns>A value indicating whether the thing name is valid when specified by a user.</returns>
    public static bool IsThingNameValid(string thingName)
    {
        // Cannot be null or empty.
        if (string.IsNullOrWhiteSpace(thingName))
        {
            return false;
        }

        // Cannot start with digit.
        if (char.IsDigit(thingName, 0))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Merges another <see cref="Thing"/> into this one.  Only values missing on this thing
    /// are replaced with values on the <paramref name="incoming"/> version.
    /// </summary>
    /// <param name="incoming">The <see cref="Thing"/> to merge into this one.</param>
    /// <returns>This object, which has been modified by the merge.</returns>
    public Thing Merge(Thing incoming)
    {
        ArgumentNullException.ThrowIfNull(incoming);

        // We retain our Guid.
        CreatedOn = incoming.CreatedOn < CreatedOn ? incoming.CreatedOn : CreatedOn;
        IsDirty = true;
        LastAccessed = DateTime.UtcNow;
        LastModified = LastAccessed;
        Name = string.IsNullOrWhiteSpace(Name) ? incoming.Name : Name;

        if (incoming.Properties != null)
        {
            foreach (var ip in incoming.Properties)
            {
                if (!Properties.ContainsKey(ip.Key))
                {
                    Properties.TryAdd(ip.Key, ip.Value);
                }
            }
        }

        if (incoming.SchemaGuids != null)
        {
            foreach (var ig in incoming.SchemaGuids)
            {
                if (!SchemaGuids.Contains(ig))
                {
                    SchemaGuids.Add(ig);
                }
            }
        }

        return this;
    }
}
