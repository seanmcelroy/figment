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

using System.Globalization;
using CsvHelper;
using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// Command that imports entities as <see cref="Thing"/>s of a <see cref="Schema"/> type.
/// </summary>
public class ImportSchemaThingsCommand : CancellableAsyncCommand<ImportSchemaThingsCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ImportSchemaThingsCommandSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.FilePath)
            || !File.Exists(settings.FilePath))
        {
            AmbientErrorContext.Provider.LogError($"File path '{settings.FilePath}' was not found.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        if (string.IsNullOrWhiteSpace(settings.SchemaName))
        {
            AmbientErrorContext.Provider.LogError("Schema name must be specified.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var schemaPossibilities = Schema.ResolveAsync(settings.SchemaName, cancellationToken)
            .ToBlockingEnumerable(cancellationToken)
            .ToArray();

        Schema? schema;
        switch (schemaPossibilities.Length)
        {
            case 0:
                AmbientErrorContext.Provider.LogError($"No schema found named '{settings.SchemaName}'");
                return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
            case 1:
                {
                    var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
                    if (provider == null)
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to load schema storage provider.");
                        return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
                    }

                    schema = await provider.LoadAsync(schemaPossibilities[0].Guid, cancellationToken);
                    if (schema == null)
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to load schema '{settings.SchemaName}'.");
                        return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR;
                    }

                    break;
                }

            default:
                AmbientErrorContext.Provider.LogError("Ambiguous match; more than one schema matches this name.");
                return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
        }

        IAsyncEnumerable<Thing> things;

        // Try to determine file format
        var fileFormat = settings.Format;
        if (string.IsNullOrWhiteSpace(settings.Format))
        {
            // Extension
            if (string.Compare(Path.GetExtension(settings.FilePath), "csv", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                fileFormat = "csv";
            }
        }

        var possibleImportMaps = schema.ImportMaps
            .Where(s => string.Compare(s.Format, settings.Format, StringComparison.InvariantCultureIgnoreCase) == 0)
            .ToArray();

        if (possibleImportMaps.Length == 0)
        {
            AmbientErrorContext.Provider.LogError($"No import configuration are defined on {schema.Name} for {fileFormat ?? "this"} file");
            return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }

        if (string.Compare(fileFormat, "csv", StringComparison.InvariantCultureIgnoreCase) == 0)
        {
            things = ImportCsv(settings.FilePath, schema);
        }
        else
        {
            AmbientErrorContext.Provider.LogError($"Unsupported format '{settings.Format}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        await foreach (var thing in things)
        {

        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }

    private static async IAsyncEnumerable<Thing> ImportCsv(
        string filePath,
        Schema schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(schema);

        if (!File.Exists(filePath))
        {
            yield break;
        }

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        if (!await csv.ReadAsync())
        {
            AmbientErrorContext.Provider.LogError($"Unable to read CSV file {filePath}");
            yield break;
        }

        if (!csv.ReadHeader())
        {
            AmbientErrorContext.Provider.LogError($"Unable to read CSV file headers from {filePath}");
            yield break;
        }

        // Dump headers
        AmbientErrorContext.Provider.LogDebug($"Headers: {csv.HeaderRecord.Aggregate((c, n) => $"{c},{n}")}");

        // Is this a CSV?

        yield break;
    }
}