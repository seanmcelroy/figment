using Figment.Common;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class VerboseCommand : CancellableAsyncCommand<VerboseCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, VerboseCommandSettings settings, CancellationToken cancellationToken)
    {
        // verbose on
        // verbose false

        if (!SchemaBooleanField.TryParseBoolean(settings.Value, out bool value))
        {
            Program.Verbose = false;
            AnsiConsole.MarkupLine("Always verbose: [red]off[/].");
            return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
        }

        Program.Verbose = value;
        if (value)
            AnsiConsole.MarkupLine("Always verbose: [green]on[/].");
        else
            AnsiConsole.MarkupLine("Always verbose: [red]off[/].");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}