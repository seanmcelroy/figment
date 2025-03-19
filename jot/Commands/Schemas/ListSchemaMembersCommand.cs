using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class ListSchemaMembersCommand : SchemaCancellableAsyncCommand<ListSchemaMembersCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ListSchemaMembersCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        if (settings.AsTable ?? false)
        {
            Table t = new();
            t.AddColumn("Name");
            t.AddColumn("GUID");

            await foreach (var reference in tsp.GetBySchemaAsync(schema!.Guid, cancellationToken))
            {
                var thing = await tsp.LoadAsync(reference.Guid, cancellationToken);
                if (thing != null)
                {
                    t.AddRow(thing.Name ?? string.Empty, reference.Guid);
                }
            }

            AnsiConsole.Write(t);
        }
        else
        {
            await foreach (var reference in tsp.GetBySchemaAsync(schema!.Guid, cancellationToken))
            {
                var thing = await tsp.LoadAsync(reference.Guid, cancellationToken);
                if (thing != null)
                {
                    Console.WriteLine(thing.Name);
                }
            }
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}