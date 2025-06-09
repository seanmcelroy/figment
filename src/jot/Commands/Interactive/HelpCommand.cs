/*
Figment
Copyright (C) 2025  Sean McElroy

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Text;
using Figment.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Interactive;

/// <summary>
/// Help command in interactive mode.
/// </summary>
public class HelpCommand : CancellableAsyncCommand
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        if (string.Equals(context.Name, "help", StringComparison.CurrentCultureIgnoreCase))
        {
            AnsiConsole.MarkupLine(
            """
            Welcome to [bold fuchsia]jot[/]'s interactive mode.

            You can type [purple]--help[/] in interactive mode to see help on the main commands.

            Or you can type [purple]ihelp[/] to see commands only available in this special mode.
            """);
            return await Task.FromResult(0);
        }

        var sb = new StringBuilder();

        sb.AppendLine(
        """
        Welcome to [bold fuchsia]jot[/]'s interactive mode.

        You can type [purple]--help[/] in interactive mode to see help on the main commands.
        Additionally, there are more commands available in this mode, documented below:

        [gold3_1]ADDITIONAL INTERACTIVE COMMANDS:[/]
        """);

        // Contextual coloring
        sb.AppendLine("    select <NAME>         [white]Selects an entity as the target for other interactive commands[/]");

        if (Program.SelectedEntity == Reference.EMPTY
            || Program.SelectedEntity.Type != Reference.ReferenceType.Thing)
        {
            sb.AppendLine("[grey62]    associate <SCHEMA>    Associates the currently selected thing with the specified schema[/]");
        }
        else
        {
            sb.AppendLine("    associate <SCHEMA>    [white]Associates the currently selected thing with the specified schema[/]");
        }

        sb.AppendLine("    clear                 [white]Clears the current console[/]");

        if (Program.SelectedEntity == Reference.EMPTY
            || (Program.SelectedEntity.Type != Reference.ReferenceType.Schema
                && Program.SelectedEntity.Type != Reference.ReferenceType.Thing))
        {
            sb.AppendLine("    delete <NAME>         [white]Deletes the entity with the given name[/]");
        }
        else
        {
            sb.AppendLine("    delete                [white]Deletes the selected entity[/]");
        }

        if (Program.SelectedEntity == Reference.EMPTY
            || Program.SelectedEntity.Type != Reference.ReferenceType.Thing)
        {
            sb.AppendLine("[grey62]    dissociate <SCHEMA>   Dissociates the currently selected thing from the specified schema[/]");
        }
        else
        {
            sb.AppendLine("    dissociate <SCHEMA>   [white]Dissociates the currently selected thing from the specified schema[/]");
        }

        if (Program.SelectedEntity == Reference.EMPTY
            || Program.SelectedEntity.Type != Reference.ReferenceType.Schema)
        {
            sb.AppendLine("[grey62]    members               Enumerates things associated with the selected schema[/]");
        }
        else
        {
            sb.AppendLine("    members               [white]Enumerates things associated with the selected schema[/]");
        }

        if (Program.SelectedEntity == Reference.EMPTY
            || Program.SelectedEntity.Type != Reference.ReferenceType.Schema)
        {
            sb.AppendLine("[grey62]    plural                Sets the plural name for the schema[/]");
        }
        else
        {
            sb.AppendLine("    plural                [white]Sets the plural name for the schema[/]");
        }

        if (Program.SelectedEntity == Reference.EMPTY)
        {
            sb.AppendLine("[grey62]    print                 Prints the details about the entity. '?' is also an alias for 'print'[/]");
        }
        else
        {
            sb.AppendLine("    print                 [white]Prints the details about the entity. '?' is also an alias for 'print'[/]");
        }

        if (Program.SelectedEntity == Reference.EMPTY
            || Program.SelectedEntity.Type != Reference.ReferenceType.Thing)
        {
            sb.AppendLine("[grey62]    promote <PROP>        Promotes a property on a thing to become a schema property[/]");
        }
        else
        {
            sb.AppendLine("    promote <PROP>        [white]Promotes a property on a thing to become a schema property[/]");
        }

        if (Program.SelectedEntity == Reference.EMPTY
            || (Program.SelectedEntity.Type != Reference.ReferenceType.Schema
                && Program.SelectedEntity.Type != Reference.ReferenceType.Thing))
        {
            sb.AppendLine("[grey62]    rename <NEW_NAME>     Renames the selected entity to the new name[/]");
        }
        else
        {
            sb.AppendLine("    rename <NEW_NAME>     [white]Renames the selected entity to the new name[/]");
        }

        if (Program.SelectedEntity == Reference.EMPTY
            || (Program.SelectedEntity.Type != Reference.ReferenceType.Schema
                && Program.SelectedEntity.Type != Reference.ReferenceType.Thing))
        {
            sb.AppendLine("[grey62]    set <PROP> [[VALUE]]    Same as the 'set' CLI command, just against the selected entity[/]");
        }
        else
        {
            sb.AppendLine("    set <PROP> [[VALUE]]    [white]Same as the 'set' CLI command, just against the selected entity[/]");
        }

        if (Program.SelectedEntity == Reference.EMPTY
            || (Program.SelectedEntity.Type != Reference.ReferenceType.Schema
                && Program.SelectedEntity.Type != Reference.ReferenceType.Thing))
        {
            sb.AppendLine("[grey62]    validate              Validates the selected entity's correctness against its schema[/]");
        }
        else
        {
            sb.AppendLine("    validate              [white]Validates the selected entity's correctness against its schema[/]");
        }

        AnsiConsole.MarkupLine(sb.ToString());

        /*AnsiConsole.MarkupLine("""
        Welcome to [bold fuchsia]jot[/]'s interactive mode.

        You can type [purple]--help[/] in interactive mode to see help on the main commands.
        Additionally, there are more commands available in this mode, documented below:

        [gold3_1]ADDITIONAL INTERACTIVE COMMANDS:[/]
            select <NAME>         [white]Selects an entity as the target for other interactive commands[/]
            associate <SCHEMA>    [white]Associates the currently selected thing with the specified schema[/]
            delete                [white]Deletes the selected entity[/]
            members               [white]Enumerates things associated with the selected schema.[/]
            print                 [white]Prints the details about the entity. '?' is also an alias for 'print'.[/]
            promote <PROP>        [white]Promotes a property on a thing to become a schema property.[/]
            set <PROP> [[VALUE]]    [white]Same as the 'set' CLI command, just against the selected entity.[/]
            validate              [white]Validates the selected entity's correctness against its schema.[/]
        """);*/

        return await Task.FromResult(0);
    }
}