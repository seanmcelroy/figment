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
/// Rebuilds the index files for <see cref="Figment.Common.Schema"/> for consistency.
/// </summary>
public class ReindexSchemasCommand : CancellableAsyncCommand
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (provider == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("purple"))
            .StartAsync("Rebuilding schema indexes...", async ctx =>
            {
                if (AnsiConsole.Profile.Capabilities.Interactive)
                {
                    Thread.Sleep(1000);
                }

                var success = await provider.RebuildIndexes(cancellationToken);
                if (success)
                {
                    ctx.Status("Success!");
                }
                else
                {
                    ctx.Status("Failed!");
                }
            });

        AmbientErrorContext.Provider.LogDone($"All schemas reindexed.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}