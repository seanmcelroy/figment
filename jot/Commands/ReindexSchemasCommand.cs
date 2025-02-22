using Figment;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ReindexSchemasCommand : CancellableAsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("purple"))
            .StartAsync("Rebuilding schema indexes...", async ctx =>
            {
                if (AnsiConsole.Profile.Capabilities.Interactive)
                    Thread.Sleep(1000);

                var success = await Schema.RebuildIndexes(cancellationToken);
                if (success)
                    ctx.Status("Success!");
                else
                    ctx.Status("Failed!");
            });

        AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: All schemas reindexed.\r\n");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}