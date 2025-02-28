using Figment.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ListThingsCommand : CancellableAsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var thingProvider = StorageUtility.StorageProvider.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        await foreach (var thing in thingProvider.GetAll(cancellationToken))
        {
            Console.WriteLine(thing.name);
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}