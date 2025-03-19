using Figment.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Interactive;

/// <summary>
/// Interactive command that sets verbosity.
/// </summary>
public class VerboseCommand : CancellableAsyncCommand<VerboseCommandSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, VerboseCommandSettings settings, CancellationToken cancellationToken)
    {
        if (!SchemaBooleanField.TryParseBoolean(settings.Value, out bool value))
        {
            Program.Verbose = false;
            AnsiConsole.MarkupLine("Always verbose: [red]off[/].");
            return Task.FromResult((int)Globals.GLOBAL_ERROR_CODES.SUCCESS);
        }

        Program.Verbose = value;
        if (value)
        {
            AnsiConsole.MarkupLine("Always verbose: [green]on[/].");
        }
        else
        {
            AnsiConsole.MarkupLine("Always verbose: [red]off[/].");
        }

        return Task.FromResult((int)Globals.GLOBAL_ERROR_CODES.SUCCESS);
    }
}