using Figment.Common.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ListThingsCommand : CancellableAsyncCommand<ListThingsCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListThingsCommandSettings settings, CancellationToken cancellationToken)
    {
        var thingProvider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        if (settings.AsTable ?? false)
        {
            Table t = new();
            t.AddColumn("Name");
            t.AddColumn("GUID");

            await foreach (var (reference, name) in thingProvider.GetAll(cancellationToken))
                t.AddRow(name ?? string.Empty, reference.Guid);
            AnsiConsole.Write(t);
        }
        else
        {
            await foreach (var (_, name) in thingProvider.GetAll(cancellationToken))
                Console.WriteLine(name);
        }


        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}