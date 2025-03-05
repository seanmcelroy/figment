using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ReindexThingsCommand : CancellableAsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (provider == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("purple"))
            .StartAsync("Rebuilding thing indexes...", async ctx =>
            {
                if (AnsiConsole.Profile.Capabilities.Interactive)
                    Thread.Sleep(1000);
                var success = await provider.RebuildIndexes(cancellationToken);
                if (success)
                    ctx.Status("Success!");
                else
                    ctx.Status("Failed!");
            });

        AmbientErrorContext.Provider.LogDone($"All things reindexed.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}