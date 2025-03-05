using Figment.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ListSelectedSchemaMembersCommand : CancellableAsyncCommand<ListSelectedSchemaMembersCommandSettings>, ICommand
{
    private enum ERROR_CODES : int
    {
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ListSelectedSchemaMembersCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.SchemaName))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To list the members of a schema, you must first 'select' a schema.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = 
                Schema.ResolveAsync(settings.SchemaName, cancellationToken)
                    .ToBlockingEnumerable(cancellationToken)
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
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one schema matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        switch (selected.Type)
        {
            case Reference.ReferenceType.Schema:
                {
                    var cmd = new ListSchemaMembersCommand();
                    return await cmd.ExecuteAsync(context, new ListSchemaMembersCommandSettings { SchemaName = selected.Guid }, cancellationToken);
                }
            default:
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(selected.Type)}'.");
                    return (int)ERROR_CODES.UNKNOWN_TYPE;
                }
        }
    }
}