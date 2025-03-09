using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class NewImportMapCommand : CancellableAsyncCommand<NewImportMapCommandSettings>
{
    private enum ERROR_CODES : int
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, NewImportMapCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY) || selected.Type != Reference.ReferenceType.Schema)
        {
            if (string.IsNullOrWhiteSpace(settings.SchemaName))
            {
                AmbientErrorContext.Provider.LogError("To modify a schema, you must first 'select' one.");
                return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibleSchemas = Schema.ResolveAsync(settings.SchemaName, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibleSchemas.Length)
            {
                case 0:
                    AmbientErrorContext.Provider.LogError("Nothing found with that name.");
                    return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibleSchemas[0];
                    break;
                default:
                    AmbientErrorContext.Provider.LogError("Ambiguous match; more than one entity matches this name.");
                    return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (selected.Type != Reference.ReferenceType.Schema)
        {
            AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}