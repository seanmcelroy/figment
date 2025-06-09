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

using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

/// <summary>
/// Validates a <see cref="Thing"/> is consistent with its <see cref="Schema"/>.
/// </summary>
public class ValidateThingCommand : CancellableAsyncCommand<ThingCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ThingCommandSettings settings, CancellationToken cancellationToken)
    {
        Reference thingReference;
        var thingResolution = settings.ResolveThingName(cancellationToken);
        switch (thingResolution.Item1)
        {
            case Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR:
                AmbientErrorContext.Provider.LogError("To validate a thing, you must first 'select' one.");
                return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
            case Globals.GLOBAL_ERROR_CODES.NOT_FOUND:
                AmbientErrorContext.Provider.LogError($"No thing found named '{settings.ThingName}'");
                return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
            case Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH:
                AmbientErrorContext.Provider.LogError("Ambiguous match; more than one thing matches this name.");
                return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
            case Globals.GLOBAL_ERROR_CODES.SUCCESS:
                thingReference = thingResolution.thing;
                break;
            default:
                throw new NotImplementedException($"Unexpected return code {Enum.GetName(thingResolution.Item1)}");
        }

        var thingProvider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thing = await thingProvider.LoadAsync(thingReference.Guid, cancellationToken);
        if (thing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing with Guid '{thingReference.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR;
        }

        await Console.Out.WriteLineAsync($"Validating {thing.Name} ({thing.Guid}) ...");

        if (!Thing.IsThingNameValid(thing.Name))
        {
            AmbientErrorContext.Provider.LogWarning($"'{thing.Name}' is an invalid name for a thing.");
        }

        List<ThingProperty> thingProperties = [];
        await foreach (var property in thing.GetProperties(cancellationToken))
        {
            thingProperties.Add(property);
            if (!ThingProperty.IsPropertyNameValid(property.SimpleDisplayName, out string? message))
            {
                AmbientErrorContext.Provider.LogWarning($"Property name '{property.SimpleDisplayName}' is invalid: {message}");
            }
        }

        if (thing.SchemaGuids == null
            || thing.SchemaGuids.Count == 0)
        {
            AmbientErrorContext.Provider.LogDone($"Validation has finished.");
            return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
        }

        if (thing.SchemaGuids.Count > 0)
        {
            var provider = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
            if (provider == null)
            {
                AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
            }

            // Load all schemas first
            var schemas = new Dictionary<string, Schema>();
            foreach (var schemaGuid in thing.SchemaGuids.Where(g => !string.IsNullOrWhiteSpace(g)))
            {
                var schemaLoaded = await provider.LoadAsync(schemaGuid, cancellationToken);
                if (schemaLoaded == null)
                {
                    AmbientErrorContext.Provider.LogError($"Unable to load schema '{schemaGuid}' from {thing.Name}.  Must be able to load schema to promote a property to it.");
                    return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR;
                }

                schemas[schemaGuid] = schemaLoaded;
            }

            // Validate each property against its schema field type
            await ValidatePropertyAgainstSchemaFields(thingProperties, schemas, settings.Verbose ?? false, cancellationToken);

            // Validate reference integrity
            await ValidateReferenceIntegrity(thingProperties, schemas, cancellationToken);

            // Check for required properties
            foreach (var schema in schemas.Values)
            {
                foreach (var sp in schema.Properties
                    .Where(sp => sp.Value.Required
                        && !thingProperties.Any(
                            tp => string.Equals(tp.SchemaGuid, schema.Guid, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(tp.SimpleDisplayName, sp.Key, StringComparison.OrdinalIgnoreCase))))
                {
                    AmbientErrorContext.Provider.LogWarning($"Schema property {sp.Key} is required but is not set!");
                }
            }
        }

        AmbientErrorContext.Provider.LogDone($"Validation has finished.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }

    private static async Task ValidatePropertyAgainstSchemaFields(
        List<ThingProperty> thingProperties,
        Dictionary<string, Schema> schemas,
        bool verbose,
        CancellationToken cancellationToken)
    {
        foreach (var property in thingProperties.Where(p => !string.IsNullOrEmpty(p.SchemaGuid)))
        {
            if (!schemas.TryGetValue(property.SchemaGuid!, out var schema))
            {
                continue;
            }

            var fieldName = property.SimpleDisplayName;
            if (schema.Properties.TryGetValue(fieldName, out var schemaField))
            {
                if (schemaField is SchemaRefField)
                {
                    // We do not evaluate these here.  That will be checked in ValidateReferenceIntegrity.
                    continue;
                }

                // Use the schema field's built-in validation
                var isValid = await schemaField.IsValidAsync(property.Value, cancellationToken);

                if (!isValid)
                {
                    var fieldType = await schemaField.GetReadableFieldTypeAsync(verbose, cancellationToken);
                    var suggestion = GetValidationSuggestion(schemaField);

                    AmbientErrorContext.Provider.LogWarning(
                        $"Property '{property.FullDisplayName}' has invalid value '{property.Value}' for {fieldType} field. {suggestion}");
                }
            }
        }
    }

    private static string GetValidationSuggestion(SchemaFieldBase field)
    {
        return field switch
        {
            SchemaEmailField => "Expected valid email address format (e.g., user@domain.com)",
            SchemaDateField => "Expected date format (e.g., 2023-12-25, today, tomorrow, next friday)",
            SchemaIntegerField => "Expected whole number",
            SchemaNumberField => "Expected numeric value",
            SchemaBooleanField => "Expected true/false or yes/no",
            SchemaUriField => "Expected valid URL format",
            SchemaPhoneField => "Expected valid phone number format",
            _ => "Value does not meet field requirements"
        };
    }

    private static async Task ValidateReferenceIntegrity(
        List<ThingProperty> thingProperties,
        Dictionary<string, Schema> schemas,
        CancellationToken cancellationToken)
    {
        var thingProvider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (thingProvider == null)
        {
            return;
        }

        foreach (var property in thingProperties.Where(p => !string.IsNullOrEmpty(p.SchemaGuid)))
        {
            if (!schemas.TryGetValue(property.SchemaGuid!, out var schema))
            {
                continue;
            }

            var fieldName = property.SimpleDisplayName;
            if (!schema.Properties.TryGetValue(fieldName, out var schemaField) || schemaField is not SchemaRefField refField)
            {
                continue;
            }

            if (property.Value is not string referencedGuid)
            {
                AmbientErrorContext.Provider.LogWarning($"Reference '{property.FullDisplayName}' has invalid value '{property.Value}'. Expected string GUID, but type is {property.Valid.GetType().FullName}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(referencedGuid))
            {
                continue;
            }

            // Check if referenced thing exists
            if (!await thingProvider.GuidExists(referencedGuid, cancellationToken))
            {
                AmbientErrorContext.Provider.LogWarning($"Reference '{property.FullDisplayName}' points to non-existent thing with GUID '{referencedGuid}'. Update the reference or remove the property.");
                continue;
            }

            // Check if referenced thing has the expected schema
            var referencedThing = await thingProvider.LoadAsync(referencedGuid, cancellationToken);
            if (referencedThing == null)
            {
                // We break this up between the previous and next if branches so we don't output warnings on ref field checks.
                AmbientErrorContext.Provider.LogWarning($"Reference '{property.FullDisplayName}' points to non-existent thing with GUID '{referencedGuid}'. Update the reference or remove the property.");
                continue;
            }

            if (referencedThing.SchemaGuids == null || !referencedThing.SchemaGuids.Contains(refField.SchemaGuid))
            {
                AmbientErrorContext.Provider.LogWarning($"Reference '{property.FullDisplayName}' points to thing '{referencedThing.Name}' which doesn't have the expected schema '{refField.SchemaGuid}'.");
            }
        }
    }
}
