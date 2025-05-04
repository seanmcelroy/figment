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

namespace jot.Commands.Schemas;

/// <summary>
/// Lists all the things associated with a schema.
/// </summary>
public class ListSchemaMembersCommand : SchemaCancellableAsyncCommand<ListSchemaMembersCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ListSchemaMembersCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        if (settings.AsTable ?? false)
        {
            Table t = new();
            t.AddColumn("Name");
            t.AddColumn("GUID");

            await foreach (var reference in tsp.GetBySchemaAsync(schema!.Guid, cancellationToken))
            {
                var thing = await tsp.LoadAsync(reference.Guid, cancellationToken);
                if (thing != null)
                {
                    t.AddRow(thing.Name ?? string.Empty, reference.Guid);
                }
            }

            AnsiConsole.Write(t);
        }
        else
        {
            await foreach (var reference in tsp.GetBySchemaAsync(schema!.Guid, cancellationToken))
            {
                var thing = await tsp.LoadAsync(reference.Guid, cancellationToken);
                if (thing != null)
                {
                    Console.WriteLine(thing.Name);
                }
            }
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}