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
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.ThingName))
            {
                AmbientErrorContext.Provider.LogError("To associate a schema to a thing, you must first 'select' a thing.");
                return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Thing.ResolveAsync(settings.ThingName, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibilities.Length)
            {
                case 0:
                    AmbientErrorContext.Provider.LogError("Nothing found with that name");
                    return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibilities[0];
                    break;
                default:
                    AmbientErrorContext.Provider.LogError("Ambiguous match; more than one thing matches this name.");
                    return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (selected.Type != Reference.ReferenceType.Thing)
        {
            AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
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
                        AmbientErrorContext.Provider.LogError($"Unable to load schema storage provider.");
                        return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
                    }

                    schema = await provider.LoadAsync(schemaPossibilities[0].Guid, cancellationToken);
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
        if (string.IsNullOrWhiteSpace(settings.ThingName))
        {
            AmbientErrorContext.Provider.LogError("Thing name must be specified.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var thingPossibilities = Thing.ResolveAsync(settings.ThingName, cancellationToken)
            .ToBlockingEnumerable(cancellationToken)
            .ToArray();

        Thing? thing;
        switch (thingPossibilities.Length)
        {
            case 0:
                AmbientErrorContext.Provider.LogError($"No thing found named '{settings.ThingName}'");
                return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
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
                    AmbientErrorContext.Provider.LogError($"Unable to load thing '{settings.ThingName}'.");
                    return (int)Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR;
                }

                break;
            default:
                AmbientErrorContext.Provider.LogError("Ambiguous match; more than one thing matches this name.");
                return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
        }

        if (thing.SchemaGuids.Any(s => string.CompareOrdinal(s, schema.Guid) == 0))
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