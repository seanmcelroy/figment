using Figment.Common;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class PrintSelectedCommand : CancellableAsyncCommand<PrintSelectedCommandSettings>, ICommand
{
    private enum ERROR_CODES : int
    {
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, PrintSelectedCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To view properties on an entity, you must first 'select' one.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (selected.Type)
        {
            case Reference.ReferenceType.Schema:
                {
                    var cmd = new PrintSchemaCommand();
                    return await cmd.ExecuteAsync(context, new SchemaCommandSettings
                    {
                        SchemaName = selected.Guid,
                        Verbose = settings.Verbose ?? Program.Verbose
                    }, cancellationToken);
                }
            case Reference.ReferenceType.Thing:
                {
                    var cmd = new PrintThingCommand();
                    return await cmd.ExecuteAsync(context, new PrintThingCommandSettings
                    {
                        ThingName = selected.Guid,
                        NoPrettyDisplayNames = settings.NoPrettyDisplayNames,
                        Verbose = settings.Verbose ?? Program.Verbose
                    }, cancellationToken);
                }
            default:
                {
                    AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
                    return (int)ERROR_CODES.UNKNOWN_TYPE;
                }
        }
    }
}