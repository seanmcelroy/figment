using Figment.Common;
using Figment.Common.Errors;
using jot.Commands.Schemas;
using Spectre.Console.Cli;

namespace jot.Commands.Interactive;

/// <summary>
/// Lists all the things associated with a selected <see cref="Schema"/>.
/// </summary>
public class ListSelectedSchemaMembersCommand : CancellableAsyncCommand<ListSelectedSchemaMembersCommandSettings>, ICommand
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ListSelectedSchemaMembersCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To list the members of a schema, you must first 'select' a schema.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (selected.Type)
        {
            case Reference.ReferenceType.Schema:
                {
                    var cmd = new ListSchemaMembersCommand();
                    return await cmd.ExecuteAsync(context, new ListSchemaMembersCommandSettings { SchemaName = selected.Guid, Verbose = settings.Verbose }, cancellationToken);
                }

            default:
                {
                    AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
                    return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
                }
        }
    }
}