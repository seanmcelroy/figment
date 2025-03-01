using Figment.Common.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ListSchemasCommand : CancellableAsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var provider = StorageUtility.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        await foreach (var (_, name) in provider.GetAll(cancellationToken))
        {
            Console.WriteLine(name);
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}