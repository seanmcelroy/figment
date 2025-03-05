using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class HelpCommand : CancellableAsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("""
        Welcome to [bold fuchsia]jot[/]'s interactive mode.

        You can type [purple]--help[/] in interactive mode to see help on the main commands.
        Additionally, there are more commands available in this mode, documented below:

        [gold3_1]ADDITIONAL INTERACTIVE COMMANDS:[/]
            select <NAME>         [white]Selects an entity as the target for other interactive commands[/]
            delete                [white]Deletes the selected entity[/]
            members               [white]Enumerates things associated with the selected schema.[/]
            print                 [white]Prints the details about the entity. '?' is also an alias for 'print'.[/]
            promote <PROP>        [white]Promotes a property on a thing to become a schema property.[/]
            set <PROP> [[VALUE]]    [white]Same as the 'set' CLI command, just against the selected entity.[/]
            validate              [white]Validates the selected entity's correctness against its schema.[/]
        """);

        return await Task.FromResult(0);
    }
}