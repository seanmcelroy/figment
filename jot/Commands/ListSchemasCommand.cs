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
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// Lists all schemas.
/// </summary>
public class ListSchemasCommand : CancellableAsyncCommand<ListSchemasCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ListSchemasCommandSettings settings, CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        List<Schema> schemas = [];

        await foreach (var (reference, name) in provider.GetAll(cancellationToken))
        {
            var schema = await provider.LoadAsync(reference.Guid, cancellationToken);
            if (schema == null)
            {
                AmbientErrorContext.Provider.LogError($"Unable to load schema '{name}' ({reference.Guid}).");
                continue;
            }

            schemas.Add(schema);
        }

        schemas.Sort((x, y) => x.Name.CompareTo(y.Name));

        if (settings.AsTable ?? false)
        {
            Table t = new();
            t.AddColumn("Name");
            t.AddColumn("Description");
            t.AddColumn("Plural");
            t.AddColumn("GUID");

            foreach (var s in schemas)
            {
                if (string.IsNullOrWhiteSpace(settings.PartialNameMatch)
                    || (
                        s.Name != null
                        && s.Name.Contains(settings.PartialNameMatch, StringComparison.CurrentCultureIgnoreCase)
                    )
                )
                {
                    t.AddRow(s.Name ?? string.Empty, s.Description ?? string.Empty, s.Plural ?? string.Empty, s.Guid);
                }
            }

            AnsiConsole.Write(t);
        }
        else
        {
            foreach (var schema in schemas)
            {
                if (string.IsNullOrWhiteSpace(settings.PartialNameMatch)
                    || (
                        schema.Name != null
                        && schema.Name.Contains(settings.PartialNameMatch, StringComparison.CurrentCultureIgnoreCase)
                    )
                )
                {
                    Console.Out.WriteLine(schema.Name);
                }
            }
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}