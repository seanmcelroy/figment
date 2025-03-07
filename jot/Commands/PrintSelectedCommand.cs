using Figment.Common;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class PrintSelectedCommand : CancellableAsyncCommand<PrintSelectedCommandSettings>, ICommand
{
    private enum ERROR_CODES : int
    {
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, PrintSelectedCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.EntityName))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To view properties on an entity, you must first 'select' one.");
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
                    AmbientErrorContext.Provider.LogError("Nothing found with that name"); return (int)ERROR_CODES.NOT_FOUND;
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
                    var cmd = new PrintSchemaCommand();
                    return await cmd.ExecuteAsync(context, new SchemaCommandSettings { SchemaName = selected.Guid }, cancellationToken);
                }
            case Reference.ReferenceType.Thing:
                {
                    var cmd = new PrintThingCommand();
                    return await cmd.ExecuteAsync(context, new ThingCommandSettings { ThingName = selected.Guid }, cancellationToken);
                }
            default:
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(selected.Type)}'.");
                    return (int)ERROR_CODES.UNKNOWN_TYPE;
                }
        }
    }
}