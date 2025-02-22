using Figment;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ValidateSchemaCommand : CancellableAsyncCommand<SchemaCommandSettings>
{
    private enum ERROR_CODES : int
    {
        SUCCESS = Globals.GLOBAL_ERROR_CODES.SUCCESS,
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
        SCHEMA_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SchemaCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To validate a schema, you must first 'select' a schema.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Reference.ResolveAsync(settings.Name, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .Where(x => x.Type == Reference.ReferenceType.Schema)
                .ToArray();
            switch (possibilities.Length)
            {
                case 0:
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Nothing found with that name");
                    return (int)ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibilities[0];
                    break;
                default:
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one entity matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (selected.Type != Reference.ReferenceType.Schema)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        var schema = await Schema.LoadAsync(selected.Guid, cancellationToken);
        if (schema == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        await Console.Out.WriteLineAsync($"Validating schema {schema.Name} ({schema.Guid}) ...");

        if (string.IsNullOrWhiteSpace(schema.Description))
        {
            AnsiConsole.MarkupLine("[yellow]WARN[/]: Description is not set, leading to an invalid JSON schema on disk.  Resolve with: [bold]set Description \"Sample description\"[/]");
        }
        if (string.IsNullOrWhiteSpace(schema.Plural))
        {
            AnsiConsole.MarkupLineInterpolated($"[yellow]WARN[/]: Plural is not set, rendering listing of all things with this schema on the REPL inaccessible.  Resolve with: [bold]set Plural {schema.Name.ToLowerInvariant()}s[/]");
        }

        AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: Validation has finished.\r\n");
        return (int)ERROR_CODES.SUCCESS;
    }
}