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

using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// Lists all the things in the database.
/// </summary>
public class ListThingsCommand : CancellableAsyncCommand<ListThingsCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ListThingsCommandSettings settings, CancellationToken cancellationToken)
    {
        var thingProvider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        if (settings.AsTable ?? false)
        {
            Table t = new();
            t.AddColumn("Name");
            t.AddColumn("GUID");

            await foreach (var (reference, name) in thingProvider.GetAll(cancellationToken))
                if (string.IsNullOrWhiteSpace(settings.PartialNameMatch)
                    || (
                        name != null
                        && name.Contains(settings.PartialNameMatch, StringComparison.CurrentCultureIgnoreCase)
                    )
                )
                {
                    t.AddRow(name ?? string.Empty, reference.Guid);
                }

            AnsiConsole.Write(t);
        }
        else
        {
            await foreach (var (_, name) in thingProvider.GetAll(cancellationToken))
                if (string.IsNullOrWhiteSpace(settings.PartialNameMatch)
                    || (
                        name != null
                        && name.Contains(settings.PartialNameMatch, StringComparison.CurrentCultureIgnoreCase)
                    )
                )
                {
                    Console.WriteLine(name);
                }
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}