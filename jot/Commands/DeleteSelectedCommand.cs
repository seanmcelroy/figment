using Figment;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class DeleteSelectedCommand : CancellableAsyncCommand<DeleteSelectedCommandSettings>, ICommand
{
    private enum ERROR_CODES : int
    {
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, DeleteSelectedCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.EntityName))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To delete a thing, you must first 'select' a thing.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = 
                Schema.ResolveAsync(settings.EntityName, cancellationToken)
                    .ToBlockingEnumerable(cancellationToken)
                    .Concat([.. Thing.ResolveAsync(settings.EntityName, cancellationToken).ToBlockingEnumerable(cancellationToken)]
                    ).ToArray();

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
            case Reference.ReferenceType.Thing:
                {
                    var cmd = new DeleteThingCommand();
                    return await cmd.ExecuteAsync(context, new ThingCommandSettings { Name = selected.Guid }, cancellationToken);
                }
            default:
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Markup.Escape(Enum.GetName(selected.Type) ?? string.Empty)}'.");
                return (int)ERROR_CODES.UNKNOWN_TYPE;
        }
    }
}