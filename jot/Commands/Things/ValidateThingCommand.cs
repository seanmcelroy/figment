using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

public class ValidateThingCommand : CancellableAsyncCommand<ThingCommandSettings>
{
    private enum ERROR_CODES : int
    {
        SUCCESS = Globals.GLOBAL_ERROR_CODES.SUCCESS,
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
        SCHEMA_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR,
        THING_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ThingCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.ThingName))
            {
                AmbientErrorContext.Provider.LogError("To validate a thing, you must first 'select' a thing.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Thing.ResolveAsync(settings.ThingName, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibilities.Length)
            {
                case 0:
                    AmbientErrorContext.Provider.LogError("Nothing found with that name");
                    return (int)ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibilities[0];
                    break;
                default:
                    AmbientErrorContext.Provider.LogError("Ambiguous match; more than one thing matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (selected.Type != Reference.ReferenceType.Thing)
        {
            AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        var thingProvider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thing = await thingProvider.LoadAsync(selected.Guid, cancellationToken);
        if (thing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.THING_LOAD_ERROR;
        }

        await Console.Out.WriteLineAsync($"Validating {thing.Name} ({thing.Guid}) ...");

        List<ThingProperty> thingProperties = [];
        await foreach (var property in thing.GetProperties(cancellationToken))
        {
            thingProperties.Add(property);
            if (!property.Valid)
                AmbientErrorContext.Provider.LogWarning($"Property {property.SimpleDisplayName} ({property.TruePropertyName}) has an invalid value of '{property.Value}'.");
        }

        if (thing.SchemaGuids == null
            || thing.SchemaGuids.Count == 0)
        {
            AmbientErrorContext.Provider.LogDone($"Validation has finished.");
            return (int)ERROR_CODES.SUCCESS;
        }

        if (thing.SchemaGuids.Count > 0)
        {
            var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
            if (provider == null)
            {
                AmbientErrorContext.Provider.LogError($"Unable to load schema storage provider.");
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
                    return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
                }

                foreach (var sp in schemaLoaded.Properties
                    .Where(sp => sp.Value.Required
                        && !thingProperties.Any(
                            tp => tp.SchemaGuid == schemaLoaded.Guid
                            && string.CompareOrdinal(tp.SimpleDisplayName, sp.Key) == 0)))
                {
                    AmbientErrorContext.Provider.LogWarning($"Schema property {sp.Key} is required but is not set!");
                }
            }
        }

        AmbientErrorContext.Provider.LogDone($"Validation has finished.");
        return (int)ERROR_CODES.SUCCESS;
    }
}