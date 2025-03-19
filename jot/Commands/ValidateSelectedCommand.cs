using Figment.Common;
using Figment.Common.Errors;
using jot.Commands.Schemas;
using jot.Commands.Things;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ValidateSelectedCommand : CancellableAsyncCommand<ValidateSelectedCommandSettings>, ICommand
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ValidateSelectedCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To validate an entity, you must first 'select' one.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (selected.Type)
        {
            case Reference.ReferenceType.Schema:
                return await new ValidateSchemaCommand().ExecuteAsync(context, new SchemaCommandSettings { SchemaName = selected.Guid, Verbose = settings.Verbose }, cancellationToken);
            case Reference.ReferenceType.Thing:
                return await new ValidateThingCommand().ExecuteAsync(context, new ThingCommandSettings { ThingName = selected.Guid, Verbose = settings.Verbose }, cancellationToken);
            default:
                AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }
    }
}