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

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}