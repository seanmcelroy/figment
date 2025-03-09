using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

public class DeleteThingCommand : CancellableAsyncCommand<ThingCommandSettings>
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
        THING_DELETE_ERROR = -2003
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ThingCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.ThingName))
            {
                AmbientErrorContext.Provider.LogError("To delete a thing, you must first 'select' a thing.");
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

        // TODO: Check links

        var deleted = await thing.DeleteAsync(cancellationToken);
        if (deleted)
        {
            AmbientErrorContext.Provider.LogDone($"{thing.Name} ({thing.Guid}) deleted.");
            Program.SelectedEntity = Reference.EMPTY;
            Program.SelectedEntityName = string.Empty;
            return (int)ERROR_CODES.SUCCESS;
        }
        else
            return (int)ERROR_CODES.THING_DELETE_ERROR;
    }
}