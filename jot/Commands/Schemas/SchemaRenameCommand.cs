using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class SchemaRenameCommand : SchemaCancellableAsyncCommand<SchemaRenameCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SchemaRenameCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, ssp) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        if (string.IsNullOrWhiteSpace(settings.NewName))
        {
            AmbientErrorContext.Provider.LogError("Name of a schema cannot be empty.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var oldName = schema!.Name;
        schema.Name = settings.NewName.Trim();
        var saved = await schema.SaveAsync(cancellationToken);
        if (!saved)
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

        // For 'name', we know we should rebuild indexes.
        await ssp!.RebuildIndexes(cancellationToken);
        AmbientErrorContext.Provider.LogDone($"Schema '{oldName}' renamed to '{schema.Name}'.  Please ensure your 'plural' value for this schema is accurate.");

        if (string.CompareOrdinal(Program.SelectedEntity.Guid, schema.Guid) == 0)
        {
            Program.SelectedEntityName = schema.Name;
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}