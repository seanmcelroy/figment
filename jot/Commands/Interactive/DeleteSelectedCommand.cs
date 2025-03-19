using Figment.Common;
using Figment.Common.Errors;
using jot.Commands.Schemas;
using jot.Commands.Things;
using Spectre.Console.Cli;

namespace jot.Commands.Interactive;

/// <summary>
/// Deletes the selected object in interactive mode.
/// </summary>
public class DeleteSelectedCommand : CancellableAsyncCommand<DeleteSelectedCommandSettings>, ICommand
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, DeleteSelectedCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To delete an entity, you must first 'select' one.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (selected.Type)
        {
            case Reference.ReferenceType.Schema:
                {
                    var cmd = new DeleteSchemaCommand();
                    return await cmd.ExecuteAsync(context, new SchemaCommandSettings { SchemaName = selected.Guid, Verbose = settings.Verbose }, cancellationToken);
                }

            case Reference.ReferenceType.Thing:
                {
                    var cmd = new DeleteThingCommand();
                    return await cmd.ExecuteAsync(context, new ThingCommandSettings { ThingName = selected.Guid, Verbose = settings.Verbose }, cancellationToken);
                }

            default:
                AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }
    }
}