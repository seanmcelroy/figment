using Figment.Common;
using Figment.Common.Errors;
using jot.Commands.Schemas;
using Spectre.Console.Cli;

namespace jot.Commands.Interactive;

public class DescribeSelectedSchemaCommand : CancellableAsyncCommand<DescribeSelectedSchemaCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, DescribeSelectedSchemaCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To describe an entity, you must first 'select' one.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (selected.Type)
        {
            case Reference.ReferenceType.Schema:
                {
                    var cmd = new SetSchemaDescriptionCommand();
                    return await cmd.ExecuteAsync(context, new SetSchemaDescriptionCommandSettings { SchemaName = selected.Guid, Description = settings.Description, Verbose = settings.Verbose }, cancellationToken);
                }

            default:
                AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }
    }
}