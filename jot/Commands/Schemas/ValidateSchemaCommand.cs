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

namespace jot.Commands.Schemas;

/// <summary>
/// Validates the schema is consistent.
/// </summary>
public class ValidateSchemaCommand : SchemaCancellableAsyncCommand<SchemaCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SchemaCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        if (settings.Verbose ?? false)
        {
            AnsiConsole.WriteLine($"Validating schema {schema!.Name} ({schema.Guid}) ...");
        }
        else
        {
            AnsiConsole.WriteLine($"Validating schema {schema!.Name} ...");
        }

        if (!Schema.IsSchemaNameValid(schema.Name))
        {
            AmbientErrorContext.Provider.LogWarning($"'{schema.Name}' is an invalid name for a schema.");
        }

        if (string.IsNullOrWhiteSpace(schema.Description))
        {
            AmbientErrorContext.Provider.LogWarning("Description is not set, leading to an invalid JSON schema on disk.  Resolve with: describe \"Sample description\"");
        }

        if (string.IsNullOrWhiteSpace(schema.Plural))
        {
            AmbientErrorContext.Provider.LogWarning($"Plural is not set, rendering listing of all things with this schema on the REPL inaccessible.  Resolve with: set plural {schema.Name.ToLowerInvariant()}s");
        }

        if (!string.IsNullOrWhiteSpace(schema.VersionGuid))
        {
            var provider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
            if (provider == null)
            {
                AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            }
            else
            {
                var version = await provider.LoadAsync(schema.VersionGuid, cancellationToken);
                if (version == null)
                {
                    AmbientErrorContext.Provider.LogWarning($"Version is set to {schema.VersionGuid}, but unable to load it.");
                }
            }
        }

        foreach (var property in schema.Properties)
        {
            if (!ThingProperty.IsPropertyNameValid(property.Key, out string? message))
            {
                AmbientErrorContext.Provider.LogWarning($"Property name '{property.Key}' is invalid: {message}");
            }
        }

        AmbientErrorContext.Provider.LogDone($"Validation has finished.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}