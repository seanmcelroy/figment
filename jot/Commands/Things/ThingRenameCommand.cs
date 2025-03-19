using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

/// <summary>
/// Changes the name of a <see cref="Thing"/>.
/// </summary>
public class ThingRenameCommand : CancellableAsyncCommand<ThingRenameCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ThingRenameCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY) || selected.Type != Reference.ReferenceType.Schema)
        {
            if (string.IsNullOrWhiteSpace(settings.ThingName))
            {
                AmbientErrorContext.Provider.LogError("To rename a thing, you must first 'select' one.");
                return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibleThings = Thing.ResolveAsync(settings.ThingName, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibleThings.Length)
            {
                case 0:
                    AmbientErrorContext.Provider.LogError("Nothing found with that name.");
                    return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibleThings[0];
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

        var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError("Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thing = await tsp.LoadAsync(selected.Guid, cancellationToken);
        if (thing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing with Guid '{selected.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        if (string.IsNullOrWhiteSpace(settings.NewName))
        {
            AmbientErrorContext.Provider.LogError("Name of a thing cannot be empty.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var oldName = thing.Name;
        thing.Name = settings.NewName.Trim();
        var saved = await thing.SaveAsync(cancellationToken);
        if (!saved)
        {
            AmbientErrorContext.Provider.LogError($"Unable to save thing with Guid '{selected.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        // For 'name', we know we should rebuild indexes.
        await tsp.RebuildIndexes(cancellationToken);
        AmbientErrorContext.Provider.LogDone($"'{oldName}' renamed to '{thing.Name}'.");

        if (string.CompareOrdinal(Program.SelectedEntity.Guid, thing.Guid) == 0)
        {
            Program.SelectedEntityName = thing.Name;
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}