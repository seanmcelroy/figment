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