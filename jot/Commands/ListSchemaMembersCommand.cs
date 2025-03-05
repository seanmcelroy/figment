using Figment.Common;
using Figment.Common.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ListSchemaMembersCommand : CancellableAsyncCommand<ListSchemaMembersCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListSchemaMembersCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.SchemaName))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To enumerabe the members of a schema, you must first 'select' a schema.");
                return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Schema.ResolveAsync(settings.SchemaName, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibilities.Length)
            {
                case 0:
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Nothing found with that name");
                    return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibilities[0];
                    break;
                default:
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one schema matches this name.");
                    return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (selected.Type != Reference.ReferenceType.Schema)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }

        var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (tsp == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        if (settings.AsTable ?? false)
        {
            Table t = new();
            t.AddColumn("Name");
            t.AddColumn("GUID");

            await foreach (var reference in tsp.GetBySchemaAsync(selected.Guid, cancellationToken))
            {
                var thing = await tsp.LoadAsync(reference.Guid, cancellationToken);
                if (thing != null)
                    t.AddRow(thing.Name ?? string.Empty, reference.Guid);
            }
            AnsiConsole.Write(t);
        }
        else
        {
            await foreach (var reference in tsp.GetBySchemaAsync(selected.Guid, cancellationToken))
            {
                var thing = await tsp.LoadAsync(reference.Guid, cancellationToken);
                if (thing != null)
                    Console.WriteLine(thing.Name);
            }
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}