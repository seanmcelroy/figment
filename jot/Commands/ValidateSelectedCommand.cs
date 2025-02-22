using Figment;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ValidateSelectedCommand : CancellableAsyncCommand<ValidateSelectedCommandSettings>, ICommand
{
    private enum ERROR_CODES : int
    {
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ValidateSelectedCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.EntityName))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To validate a thing, you must first 'select' a thing.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Reference.ResolveAsync(settings.EntityName, cancellationToken)
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
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one entity matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        switch (selected.Type)
        {
            case Reference.ReferenceType.Schema:
                {
                    var cmd = new ValidateSchemaCommand();
                    return await cmd.ExecuteAsync(context, new SchemaCommandSettings { Name = selected.Guid }, cancellationToken);
                }
            case Reference.ReferenceType.Thing:
                {
                    var cmd = new ValidateThingCommand();
                    return await cmd.ExecuteAsync(context, new ThingCommandSettings { Name = selected.Guid }, cancellationToken);
                }
            default:
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(selected.Type)}'.");
                    return (int)ERROR_CODES.UNKNOWN_TYPE;
                }
        }
    }
}