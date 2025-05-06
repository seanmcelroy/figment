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

namespace jot.Commands.Schemas;

/// <summary>
/// Command that associates a <see cref="Thing"/> with a <see cref="Schema"/>.
/// </summary>
public class AssociateSchemaWithThingCommand : CancellableAsyncCommand<AssociateSchemaWithThingCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, AssociateSchemaWithThingCommandSettings settings, CancellationToken cancellationToken)
    {
        Reference thingReference;
        var thingResolution = settings.ResolveThingName(cancellationToken);
        switch (thingResolution.Item1)
        {
            case Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR:
                AmbientErrorContext.Provider.LogError("To associate a schema to a thing, you must first 'select' a thing.");
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

        // Schema first
        if (string.IsNullOrWhiteSpace(settings.SchemaName))
        {
            AmbientErrorContext.Provider.LogError("Schema name must be specified.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var schemaPossibilities = Schema.ResolveAsync(settings.SchemaName, cancellationToken)
            .ToBlockingEnumerable(cancellationToken)
            .ToArray();

        Schema? schema;
        switch (schemaPossibilities.Length)
        {
            case 0:
                AmbientErrorContext.Provider.LogError($"No schema found named '{settings.SchemaName}'");
                return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
            case 1:
                {
                    var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
                    if (provider == null)
                    {
                        AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
                        return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
                    }

                    schema = await provider.LoadAsync(schemaPossibilities[0].Reference.Guid, cancellationToken);
                    if (schema == null)
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to load schema '{settings.SchemaName}'.");
                        return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR;
                    }

                    break;
                }

            default:
                AmbientErrorContext.Provider.LogError("Ambiguous match; more than one schema matches this name.");
                return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
        }

        // Now on to the thing.
        var thingProvider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thing = await thingProvider.LoadAsync(thingReference.Guid, cancellationToken);
        if (thing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing '{settings.ThingName}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR;
        }

        if (thing.SchemaGuids.Any(s => string.Equals(s, schema.Guid, StringComparison.Ordinal)))
        {
            AmbientErrorContext.Provider.LogDone($"{thing.Name} is already associated with schema {schema.Name}. No change.");
            return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
        }

        var (success, modifiedThing) = await thing.AssociateWithSchemaAsync(schema.Guid, cancellationToken);
        if (!success || modifiedThing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to edit thing with Guid '{thing.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
        }

        if (modifiedThing.SchemaGuids.Count == 1)
        {
            AmbientErrorContext.Provider.LogDone($"{modifiedThing.Name} is now a '{schema.Name}'.");
        }
        else
        {
            AmbientErrorContext.Provider.LogDone($"{modifiedThing.Name} is now also a '{schema.Name}'.");
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}