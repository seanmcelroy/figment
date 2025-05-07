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
using System.Runtime.CompilerServices;
using CsvHelper;
using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
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
                    var provider = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
                    if (provider == null)
                    {
                        AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
                        return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
                    }

                    schema = await provider.LoadAsync(schemaPossibilities[0].Reference.Guid, cancellationToken);
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

        IAsyncEnumerable<(Thing thing, int rowNumber)> things;

        // Try to determine file format
        var fileFormat = settings.Format;
        if (string.IsNullOrWhiteSpace(settings.Format))
        {
            // Extension
            if (string.Equals(Path.GetExtension(settings.FilePath), "csv", StringComparison.InvariantCultureIgnoreCase))
            {
                fileFormat = "csv";
            }
        }

        var possibleImportMaps = schema.ImportMaps
            .Where(s => string.Equals(s.Format, settings.Format, StringComparison.InvariantCultureIgnoreCase))
            .ToArray();

        if (possibleImportMaps.Length == 0)
        {
            AmbientErrorContext.Provider.LogError($"No import configuration are defined on {schema.Name} for {fileFormat ?? "this"} file");
            return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }

        if (string.Equals(fileFormat, "csv", StringComparison.InvariantCultureIgnoreCase))
        {
            things = ImportCsv(settings.FilePath, schema, possibleImportMaps, cancellationToken);
        }
        else
        {
            AmbientErrorContext.Provider.LogError($"Unsupported format '{settings.Format}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        await foreach (var (thing, rowNumber) in things)
        {
            var (saved, saveMessage) = await tsp.SaveAsync(thing, cancellationToken);
            if (!saved)
            {
                AmbientErrorContext.Provider.LogError($"Failed to save thing from row {rowNumber}: {saveMessage}");
                continue;
            }
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }

    private static async IAsyncEnumerable<(Thing thing, int rowNumber)> ImportCsv(
        string filePath,
        Schema schema,
        SchemaImportMap[] possibleImportMaps,
        [EnumeratorCancellation] CancellationToken cancellationToken)
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

        if (!csv.ReadHeader() || csv.HeaderRecord == null || csv.HeaderRecord.Length == 0)
        {
            AmbientErrorContext.Provider.LogError($"Unable to read CSV file headers from {filePath}");
            yield break;
        }

        // Dump headers
        AmbientErrorContext.Provider.LogDebug($"Headers: {csv.HeaderRecord.Aggregate((c, n) => $"{c},{n}")}");

        // Select import map
        SchemaImportMap importMap;

        if (possibleImportMaps.Length == 0)
        {
            AmbientErrorContext.Provider.LogError("No import maps found for schema.");
            yield break;
        }

        if (possibleImportMaps.Length == 1)
        {
            importMap = possibleImportMaps[0];
        }
        else
        {
            var which = AnsiConsole.Prompt(
                new SelectionPrompt<PossibleGenericMatch<SchemaImportMap>>()
                    .Title($"There was more than one import map.  Which do you want to use for this import?")
                    .PageSize(5)
                    .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                    .EnableSearch()
                    .AddChoices(possibleImportMaps.Select(m => new PossibleGenericMatch<SchemaImportMap>(x => x.Name, m))));
            importMap = which.Entity;
        }

        if (importMap.FieldConfiguration == null
            || importMap.FieldConfiguration.Count == 0
            || !importMap.FieldConfiguration.Any(fc => !string.IsNullOrWhiteSpace(fc.SchemaPropertyName)))
        {
            AmbientErrorContext.Provider.LogError($"Import map '{importMap.Name}' does not map to any fields on {schema.Name}.  This file will not be imported.");
            yield break;
        }

        // Build index map
        Dictionary<int, string> columnIndexToPropertyName = [];
        foreach (var fc in importMap.FieldConfiguration)
        {
            if (string.IsNullOrWhiteSpace(fc.SchemaPropertyName))
            {
                // Field is unmapped, so skip.
                continue;
            }

            var matchingCsvColumns = csv.HeaderRecord
                .Select((hdr, idx) => new { hdr, idx })
                .Where(x =>
                {
                    // It could be a field name.
                    if (!x.hdr.StartsWith('=') && x.hdr.Equals(fc.ImportFieldName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }

                    // Or it could be a formula.
                    if (x.hdr.StartsWith('='))
                    {
                        // Are all fields part of CSV columns?
                        return true; // for now.
                    }

                    return false;
                })
                .ToArray();

            if (fc.SkipRecordIfMissing && matchingCsvColumns.Length == 0)
            {
                AmbientErrorContext.Provider.LogError($"File column '{fc.ImportFieldName}' is marked as required (SkipRecordIfMissing) but is missing in the CSV header.  This file will not be imported.");
                yield break;
            }

            if (matchingCsvColumns.Length > 1)
            {
                AmbientErrorContext.Provider.LogError($"File column '{fc.ImportFieldName}' appears multiple times in the CSV file.  This file will not be imported.");
                yield break;
            }

            columnIndexToPropertyName.Add(matchingCsvColumns[0].idx, fc.SchemaPropertyName);
        }

        // Dump headers
        AmbientErrorContext.Provider.LogDebug($"CsvToProps: {columnIndexToPropertyName.Select(x => $"({x.Key}:{x.Value})").Aggregate((c, n) => $"{c},{n}")}");

        var rowCount = 0;
        while (await csv.ReadAsync())
        {
            rowCount++;
            var thingGuid = Guid.NewGuid().ToString();
            var thing = new Thing(thingGuid, $"Imported {rowCount}");

            // Process each mapped field
            foreach (var mapping in columnIndexToPropertyName)
            {
                var fieldConfig = importMap.FieldConfiguration.First(fc => fc.SchemaPropertyName == mapping.Value);
                var value = csv.GetField(mapping.Key);

                // Skip if required field is missing and configured to skip
                if (string.IsNullOrWhiteSpace(value) && fieldConfig.SkipRecordIfMissing)
                {
                    AmbientErrorContext.Provider.LogWarning($"Skipping row {rowCount}: Required field '{fieldConfig.ImportFieldName}' is missing.");
                    continue;
                }

                // Try to set the property value
                try
                {
                    var result = await thing.Set(mapping.Value, value, cancellationToken);
                    if (!result.Success)
                    {
                        if (fieldConfig.SkipRecordIfInvalid)
                        {
                            AmbientErrorContext.Provider.LogWarning($"Skipping row {rowCount}: Invalid value '{value}' for field '{fieldConfig.ImportFieldName}'.");
                            continue;
                        }
                        else
                        {
                            AmbientErrorContext.Provider.LogError($"Error processing row {rowCount}: Invalid value '{value}' for field '{fieldConfig.ImportFieldName}'.");
                            yield break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (fieldConfig.SkipRecordIfInvalid)
                    {
                        AmbientErrorContext.Provider.LogWarning($"Skipping row {rowCount}: Error setting field '{fieldConfig.ImportFieldName}': {ex.Message}");
                        continue;
                    }
                    else
                    {
                        AmbientErrorContext.Provider.LogError($"Error processing row {rowCount}: {ex.Message}");
                        yield break;
                    }
                }
            }

            // Save the thing if we have at least one property set
            if (thing.Properties.Count > 0)
            {
                yield return (thing, rowCount);
            }
        }
    }
}