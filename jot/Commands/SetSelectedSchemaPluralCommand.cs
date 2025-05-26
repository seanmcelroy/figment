using Figment.Common;
using Figment.Common.Errors;
using jot.Commands.Schemas;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// Interactive mode command.  Sets the plural name for the <see cref="Schema"/>.
/// </summary>
public class SetSelectedSchemaPluralCommand : CancellableAsyncCommand<SetSelectedSchemaPluralCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SetSelectedSchemaPluralCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To edit a schema, you must first 'select' one.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (selected.Type)
        {
            case Reference.ReferenceType.Schema:
                {
                    var cmd = new SetSchemaPluralCommand();
                    return await cmd.ExecuteAsync(context, new SetSchemaPluralCommandSettings { SchemaName = selected.Guid, Plural = settings.Plural, Verbose = settings.Verbose }, cancellationToken);
                }

            default:
                AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }
    }
}