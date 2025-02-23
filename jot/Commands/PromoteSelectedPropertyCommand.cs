using Figment;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class PromoteSelectedPropertyCommand : CancellableAsyncCommand<PromoteSelectedPropertyCommandSettings>, ICommand
{
    private enum ERROR_CODES : int
    {
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, PromoteSelectedPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        if (Program.SelectedEntity.Equals(Reference.EMPTY))
        {
            AnsiConsole.MarkupLine("[yellow]ERROR[/]: To set properties on an entity, you must first 'select' it.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (Program.SelectedEntity.Type)
        {
            case Reference.ReferenceType.Thing:
                {
                    var cmd = new PromoteThingPropertyCommand();
                    return await cmd.ExecuteAsync(context, new PromoteThingPropertyCommandSettings { Name = Program.SelectedEntity.Guid, PropertyName = settings.PropertyName }, cancellationToken);
                }
            default:
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Markup.Escape(Enum.GetName(Program.SelectedEntity.Type) ?? string.Empty)}'.");
                    return (int)ERROR_CODES.UNKNOWN_TYPE;
                }
        }
    }
}