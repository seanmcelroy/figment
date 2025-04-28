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
/// Promotes a property on one <see cref="Thing"/> to become a property defined on a <see cref="Schema"/>.
/// </summary>
public class PromoteThingPropertyCommand : CancellableAsyncCommand<PromoteThingPropertyCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, PromoteThingPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        // promote propertyname, like
        Reference thingReference;
        var thingResolution = settings.ResolveThingName(cancellationToken);
        switch (thingResolution.Item1)
        {
            case Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR:
                AmbientErrorContext.Provider.LogError("To promote a property on a thing, you must first 'select' a thing.");
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

        if (thingReference.Type != Reference.ReferenceType.Thing)
        {
            AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(thingReference.Type)}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }

        if (string.IsNullOrWhiteSpace(settings.PropertyName))
        {
            AmbientErrorContext.Provider.LogError("To promote a property on a thing, you must specify the property name.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var thingProvider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thingLoaded = await thingProvider.LoadAsync(thingReference.Guid, cancellationToken);
        if (thingLoaded == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing with Guid '{thingReference.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR;
        }

        if (thingLoaded.SchemaGuids == null
            || thingLoaded.SchemaGuids.Count == 0)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load any schema from {thingLoaded.Name}.  Must be able to load an associated schema to promote a property to it.");
            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        var property = thingLoaded.GetPropertyByName(settings.PropertyName, cancellationToken)
            .ToBlockingEnumerable(cancellationToken)
            .FirstOrDefault();
        if (property.Equals(default(KeyValuePair<string, object>)))
        {
            AmbientErrorContext.Provider.LogError($"No property named '{settings.PropertyName}' on thing.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        if (thingLoaded.SchemaGuids.Count > 0)
        {
            var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
            if (provider == null)
            {
                AmbientErrorContext.Provider.LogError($"Unable to load schema storage provider.");
                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
            }

            foreach (var schemaGuid in thingLoaded.SchemaGuids)
            {
                var schema = string.IsNullOrWhiteSpace(schemaGuid)
                    ? null
                    : await provider.LoadAsync(schemaGuid, cancellationToken);

                if (schema == null)
                {
                    AmbientErrorContext.Provider.LogError($"Unable to load schema '{schemaGuid}' from {thingLoaded.Name}.  Must be able to load schema to promote a property to it.");
                    return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR;
                }

                // TODO: Right now we promote the field to EVERY associated schema.
                // Should there be a schema chooser?

                // Put the field on the schema.
                var schemaProperty = schema.AddTextField(property.TruePropertyName);

                // Update my version of the file to point to the schema version
                thingLoaded.TryRemoveProperty(property.TruePropertyName);

                // var truePropertyName = $"{schemaLoaded.Guid}.{schemaProperty.Name}";
                if (property.Value != null)
                {
                    thingLoaded.TryAddProperty($"{schema.Guid}.{schemaProperty.Name}", property.Value);
                }

                var schemaSaved = await schema.SaveAsync(cancellationToken);
                if (!schemaSaved)
                {
                    if (settings.Verbose ?? false)
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}' ({schema.Guid}).");
                    }
                    else
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}'.");
                    }

                    return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR;
                }
            }
        }

        // But what field type?  Just assume text field for now.  Deal with anything different as a schema field type change.
        var thingSaved = await thingLoaded.SaveAsync(cancellationToken);
        if (!thingSaved)
        {
            return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
        }

        AmbientErrorContext.Provider.LogDone($"'{property.FullDisplayName}' is now promoted from a one-off property on {thingLoaded.Name} to a property on associated schema(s).");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;

        // Is there a conflicting name?
    }
}