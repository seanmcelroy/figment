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

using System.Reflection;
using Figment.Common.Data;
using Figment.Common.Errors;
using Microsoft.Extensions.FileProviders;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// Lists all schemas.
/// </summary>
public class InitSchemasCommand : CancellableAsyncCommand
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load schema storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
        var defaultSchemas = embeddedProvider.GetDirectoryContents("/")
            .Where(x => x.Name.EndsWith(".schema.json", StringComparison.InvariantCulture))
            .ToArray();

        int errorCount = 0, replaceCount = 0, createCount = 0;

        foreach (var defaultSchemaFileInfo in defaultSchemas)
        {
            var defaultSchemaName = defaultSchemaFileInfo.Name[..defaultSchemaFileInfo.Name.IndexOf(".schema.json")];
            using var stream = defaultSchemaFileInfo.CreateReadStream();
            using var sr = new StreamReader(stream);
            var schemaContent = await sr.ReadToEndAsync(cancellationToken);
            var defaultSchema = await provider.LoadJsonContentAsync(schemaContent, cancellationToken);
            if (defaultSchema == null)
            {
                AmbientErrorContext.Provider.LogError($"Unable to deserialize built-in schema '{defaultSchemaFileInfo.Name}'");
                errorCount++;
                continue;
            }

            if (await provider.GuidExists(defaultSchema.Guid, cancellationToken))
            {
                if (await defaultSchema.SaveAsync(cancellationToken))
                {
                    AmbientErrorContext.Provider.LogProgress($"Overwriting schema: {defaultSchema.Name}");
                    replaceCount++;
                }
                else
                {
                    AmbientErrorContext.Provider.LogError($"Unable to save created built-in schema '{defaultSchemaFileInfo.Name}'");
                    errorCount++;
                }
            }
            else
            {
                if (await defaultSchema.SaveAsync(cancellationToken))
                {
                    AmbientErrorContext.Provider.LogProgress($"Initialized schema: {defaultSchema.Name}");
                    createCount++;
                }
                else
                {
                    AmbientErrorContext.Provider.LogError($"Unable to save created built-in schema '{defaultSchemaFileInfo.Name}'");
                    errorCount++;
                }
            }
        }

        AmbientErrorContext.Provider.LogDone($"Schemas initialized ({createCount} created, {replaceCount} replaced, {errorCount} errors).");

        var reindexSchemasCommand = new ReindexSchemasCommand();
        return await reindexSchemasCommand.ExecuteAsync(context, cancellationToken);
    }
}