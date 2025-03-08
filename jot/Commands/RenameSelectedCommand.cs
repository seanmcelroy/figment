using Figment.Common;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands;

public class RenameSelectedCommand : CancellableAsyncCommand<RenameSelectedCommandSettings>, ICommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, RenameSelectedCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To rename an entity, you must first 'select' one.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (selected.Type)
        {
            case Reference.ReferenceType.Schema:
                    return await new SchemaRenameCommand().ExecuteAsync(context, new SchemaRenameCommandSettings { SchemaName = selected.Guid, NewName = settings.NewName, Verbose = settings.Verbose    }, cancellationToken);
            case Reference.ReferenceType.Thing:
                    return await new ThingRenameCommand().ExecuteAsync(context, new ThingRenameCommandSettings { ThingName = selected.Guid, NewName = settings.NewName, Verbose = settings.Verbose }, cancellationToken);
            default:
                AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }
    }
}