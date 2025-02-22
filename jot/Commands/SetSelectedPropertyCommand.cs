using Figment;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSelectedPropertyCommand : CancellableAsyncCommand<SetSelectedPropertyCommandSettings>, ICommand
{
    private enum ERROR_CODES : int
    {
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SetSelectedPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        if (Program.SelectedEntity.Equals(Reference.EMPTY))
        {
            AnsiConsole.MarkupLine("[yellow]ERROR[/]: To set properties on an entity, you must first 'select' it.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (Program.SelectedEntity.Type)
        {
            case Reference.ReferenceType.Schema:
                {
                    var cmd = new SetSchemaPropertyCommand();
                    return await cmd.ExecuteAsync(context, new SetSchemaPropertyCommandSettings { Name = Program.SelectedEntity.Guid, PropertyName = settings.PropertyName, Value = settings.Value }, cancellationToken);
                }
            case Reference.ReferenceType.Thing:
                {
                    var cmd = new SetThingPropertyCommand();
                    return await cmd.ExecuteAsync(context, new SetThingPropertyCommandSettings { Name = Program.SelectedEntity.Guid, PropertyName = settings.PropertyName, Value = settings.Value }, cancellationToken);
                }
            default:
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(Program.SelectedEntity.Type)}'.");
                    return (int)ERROR_CODES.UNKNOWN_TYPE;
                }
        }
    }
}