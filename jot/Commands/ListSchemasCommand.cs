using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ListSchemasCommand : CancellableAsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        List<Schema> schemas = [];

        await foreach (var (reference, name) in provider.GetAll(cancellationToken))
        {
            var schema = await provider.LoadAsync(reference.Guid, cancellationToken);
            if (schema == null)
            {
                AmbientErrorContext.ErrorProvider.LogError($"Unable to load schema '{name}' ({reference.Guid}).");
                continue;
            }

            schemas.Add(schema);
            Console.WriteLine(name);
        }

        schemas.Sort((x, y) => x.Name.CompareTo(y.Name));

        Table t = new();
        t.AddColumn("Name");
        t.AddColumn("Description");
        t.AddColumn("Plural");

        foreach (var s in schemas)
            t.AddRow(s.Name, s.Description ?? string.Empty, s.Plural ?? string.Empty);
        AnsiConsole.Write(t);

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}