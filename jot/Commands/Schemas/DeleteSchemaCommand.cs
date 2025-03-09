using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class DeleteSchemaCommand : SchemaCancellableAsyncCommand<SchemaCommandSettings>
{
    private enum ERROR_CODES : int
    {
        THING_DELETE_ERROR = -2003
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SchemaCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
            return (int)tgs;

        var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        Thing? anyRandomThing = null;
        await foreach (var any in tsp.GetBySchemaAsync(schema!.Guid, cancellationToken))
        {
            anyRandomThing = await tsp.LoadAsync(any.Guid, cancellationToken);
            if (anyRandomThing != null)
                break;
        }

        if (anyRandomThing != null)
        {
            AmbientErrorContext.Provider.LogWarning($"Unable to delete a schema because things exist that use it, such as '{anyRandomThing.Name}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var deleted = await schema.DeleteAsync(cancellationToken);
        if (deleted)
        {
            if (settings.Verbose ?? Program.Verbose)
                AmbientErrorContext.Provider.LogDone($"{schema.Name} ({schema.Guid}) deleted.");
            else
                AmbientErrorContext.Provider.LogDone($"{schema.Name} deleted.");
            Program.SelectedEntity = Reference.EMPTY;
            Program.SelectedEntityName = string.Empty;
            return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
        }
        else
            return (int)ERROR_CODES.THING_DELETE_ERROR;
    }
}