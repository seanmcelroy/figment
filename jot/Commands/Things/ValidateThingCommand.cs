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
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
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
            if (!ThingProperty.IsPropertyNameValid(property.SimpleDisplayName))
            {
                AmbientErrorContext.Provider.LogWarning($"Property {property.SimpleDisplayName} has an invalid name.");
            }

            if (!property.Valid)
            {
                AmbientErrorContext.Provider.LogWarning($"Property {property.SimpleDisplayName} ({property.TruePropertyName}) has an invalid value of '{property.Value}'.");
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
                AmbientErrorContext.Provider.LogError("Unable to load schema storage provider.");
                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
            }

            foreach (var schemaGuid in thing.SchemaGuids)
            {
                var schemaLoaded = string.IsNullOrWhiteSpace(schemaGuid)
                    ? null
                    : await provider.LoadAsync(schemaGuid, cancellationToken);

                if (schemaLoaded == null)
                {
                    AmbientErrorContext.Provider.LogError($"Unable to load schema '{schemaGuid}' from {thing.Name}.  Must be able to load schema to promote a property to it.");
                    return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR;
                }

                foreach (var sp in schemaLoaded.Properties
                    .Where(sp => sp.Value.Required
                        && !thingProperties.Any(
                            tp => string.Equals(tp.SchemaGuid, schemaLoaded.Guid, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(tp.SimpleDisplayName, sp.Key, StringComparison.OrdinalIgnoreCase))))
                {
                    AmbientErrorContext.Provider.LogWarning($"Schema property {sp.Key} is required but is not set!");
                }
            }
        }

        AmbientErrorContext.Provider.LogDone($"Validation has finished.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}