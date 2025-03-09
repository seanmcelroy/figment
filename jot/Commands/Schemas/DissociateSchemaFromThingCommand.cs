using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class DissociateSchemaFromThingCommand : CancellableAsyncCommand<DissociateSchemaFromThingCommandSettings>
{
    private enum ERROR_CODES : int
    {
        SUCCESS = Globals.GLOBAL_ERROR_CODES.SUCCESS,
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
        SCHEMA_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR,
        SCHEMA_SAVE_ERROR = Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR,
        THING_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR,
        THING_SAVE_ERROR = Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, DissociateSchemaFromThingCommandSettings settings, CancellationToken cancellationToken)
    {
        // Schema first
        if (string.IsNullOrWhiteSpace(settings.SchemaName))
        {
            AmbientErrorContext.Provider.LogWarning("Schema name must be specified.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        var schemaPossibilities = Schema.ResolveAsync(settings.SchemaName, cancellationToken)
            .ToBlockingEnumerable(cancellationToken)
            .ToArray();

        Schema? schema;
        switch (schemaPossibilities.Length)
        {
            case 0:
                AmbientErrorContext.Provider.LogError($"No schema found named '{settings.SchemaName}'");
                return (int)ERROR_CODES.NOT_FOUND;
            case 1:
                {
                    var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
                    if (provider == null)
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to load schema storage provider.");
                        return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
                    }

                    schema = await provider.LoadAsync(schemaPossibilities[0].Guid, cancellationToken);
                    if (schema == null)
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to load schema '{settings.SchemaName}'.");
                        return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
                    }
                    break;
                }
            default:
                AmbientErrorContext.Provider.LogError("Ambiguous match; more than one schema matches this name.");
                return (int)ERROR_CODES.AMBIGUOUS_MATCH;
        }

        // Now on to the thing.
        if (string.IsNullOrWhiteSpace(settings.ThingName))
        {
            AmbientErrorContext.Provider.LogError("Thing name must be specified.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        var thingPossibilities = Thing.ResolveAsync(settings.ThingName, cancellationToken)
            .ToBlockingEnumerable(cancellationToken)
            .ToArray();

        Thing? thing;
        switch (thingPossibilities.Length)
        {
            case 0:
                AmbientErrorContext.Provider.LogError($"No thing found named '{settings.ThingName}'");
                return (int)ERROR_CODES.NOT_FOUND;
            case 1:
                var thingProvider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
                if (thingProvider == null)
                {
                    AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
                    return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
                }

                thing = await thingProvider.LoadAsync(thingPossibilities[0].Guid, cancellationToken);
                if (thing == null)
                {
                    AmbientErrorContext.Provider.LogError($"Unable to load schema '{settings.ThingName}'.");
                    return (int)ERROR_CODES.THING_LOAD_ERROR;
                }
                break;
            default:
                AmbientErrorContext.Provider.LogError("Ambiguous match; more than one thing matches this name.");
                return (int)ERROR_CODES.AMBIGUOUS_MATCH;
        }

        var (success, modifiedThing) = await thing.DissociateFromSchemaAsync(schema.Guid, cancellationToken);
        if (!success || modifiedThing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to edit thing with Guid '{thing.Guid}'.");
            return (int)ERROR_CODES.THING_SAVE_ERROR;
        }

        if (modifiedThing.SchemaGuids.Count == 0)
            AmbientErrorContext.Provider.LogDone($"{modifiedThing.Name} is no longer associated to any schemas.");
        else
            AmbientErrorContext.Provider.LogDone($"{modifiedThing.Name} is no longer a '{schema.Name}'.");

        return (int)ERROR_CODES.SUCCESS;
    }
}