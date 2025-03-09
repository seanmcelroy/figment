using Figment.Common;
using Figment.Common.Errors;
using jot.Commands.Things;
using Spectre.Console.Cli;

namespace jot.Commands;

public class PromoteSelectedPropertyCommand : CancellableAsyncCommand<PromoteSelectedPropertyCommandSettings>, ICommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, PromoteSelectedPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        if (Program.SelectedEntity.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To set properties on an entity, you must first 'select' it.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (Program.SelectedEntity.Type)
        {
            case Reference.ReferenceType.Thing:
                {
                    var cmd = new PromoteThingPropertyCommand();
                    return await cmd.ExecuteAsync(context, new PromoteThingPropertyCommandSettings { ThingName = Program.SelectedEntity.Guid, PropertyName = settings.PropertyName, Verbose = settings.Verbose }, cancellationToken);
                }
            default:
                AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(Program.SelectedEntity.Type)}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }
    }
}