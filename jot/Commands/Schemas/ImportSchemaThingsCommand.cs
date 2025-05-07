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
using Figment.Common.Calculations.Parsing;
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
    private static readonly string TOMBSTONE = "$$$|🪦|TOMBSTONE";

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

        var importedThings = new List<(Thing thing, int rowNumber)>();
        await foreach (var (thing, rowNumber) in things)
        {
            importedThings.Add((thing, rowNumber));
        }

        // Are there duplicates in the import batch?  We can test this without touching the current data store.
        var dupes = importedThings
            .GroupBy(i => i.thing.Name, StringComparer.InvariantCultureIgnoreCase)
            .Where(i => i.Count() > 1)
            .Select(i => new { name = i.Key, count = i.Count(), rows = i.Select(r => $"{r.rowNumber}").Aggregate((c, n) => $"{c},{n}") });

        if (dupes.Any())
        {
            foreach (var dupe in dupes)
            {
                AmbientErrorContext.Provider.LogError($"{dupe.count} duplicates for '{dupe.name}' on rows {dupe.rows}.");
            }

            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR; // TODO: Return a more specific error code.
        }

        // Now check dupes against the data store.
        var savedCount = 0;
        await foreach (var (thing, rowNumber) in things)
        {
            // Before we save it, is there already an object with the same name?
            var existing = await tsp.FindByNameAsync(thing.Name, cancellationToken);
            if (existing != Reference.EMPTY)
            {
                AmbientErrorContext.Provider.LogError($"Failed to save thing from row {rowNumber}: Another object with the same name already exists. ('{thing.Name}')");
                continue;
            }

            var (saved, saveMessage) = await tsp.SaveAsync(thing, cancellationToken);
            if (!saved)
            {
                AmbientErrorContext.Provider.LogError($"Failed to save thing from row {rowNumber}: {saveMessage}");
                continue;
            }
            else
            {
                savedCount++;
            }
        }

        AmbientErrorContext.Provider.LogDone($"Imported {savedCount} rows from '{settings.FilePath}'.");

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("purple"))
            .StartAsync("Rebuilding thing indexes...", async ctx =>
            {
                var success = await tsp.RebuildIndexes(cancellationToken);
                if (success)
                {
                    ctx.Status("Success!");
                }
                else
                {
                    ctx.Status("Failed!");
                }
            });

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
        var propertyNamesHandler = new Dictionary<string, Func<CsvReader, object?>>();
        foreach (var fc in importMap.FieldConfiguration)
        {
            if (string.IsNullOrWhiteSpace(fc.ImportFieldName)
                || string.IsNullOrWhiteSpace(fc.SchemaPropertyName))
            {
                // Field is unmapped, so skip.
                continue;
            }

            // If the import field name is a formula, parse it now as it might have multiple field references.
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
            string[]? formulaFieldReferences = null;
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
            if (fc.ImportFieldName.StartsWith('='))
            {
                // This property matches to a formula, which may reference zero to many CSV header columns.
                if (!ExpressionParser.TryParse(fc.ImportFieldName, out NodeBase? nb))
                {
                    AmbientErrorContext.Provider.LogError($"Import map '{importMap.Name}' contains mapping '{fc.ImportFieldName}' which starts with an equals sign, but it could not be parsed as a formula.  This file will not be imported.");
                    yield break;
                }

                formulaFieldReferences = [.. nb.WalkFieldNames()];

                // Build argument parsers
                var argHandlers = new Dictionary<string, Func<CsvReader, string?>>();
                foreach (var formFieldRef in formulaFieldReferences)
                {
                    var matchingCsvColumns = csv.HeaderRecord
                        .Select((hdr, idx) => new { hdr, idx })
                        .Where(x => x.hdr.Equals(formFieldRef, StringComparison.InvariantCultureIgnoreCase))
                        .ToArray();

                    if (matchingCsvColumns.Length == 0)
                    {
                        if (fc.SkipRecordIfMissing)
                        {
                            AmbientErrorContext.Provider.LogError($"File column has formula '{fc.ImportFieldName}' and references field '{formFieldRef}' which was not found.  This property {fc.SchemaPropertyName} is marked as required (SkipRecordIfMissing=true).  This file will not be imported.");
                            yield break;
                        }

                        argHandlers.Add(formFieldRef, cr => null);
                    }
                    else if (matchingCsvColumns.Length > 1)
                    {
                        AmbientErrorContext.Provider.LogError($"File column has formula '{fc.ImportFieldName}' and references field '{formFieldRef}' which appears multiple times in the CSV file.  This file will not be imported.");
                        yield break;
                    }
                    else
                    {
                        argHandlers.Add(formFieldRef, cr => cr.GetField(matchingCsvColumns[0].idx));
                    }
                }

                var bespokeHandler = new Func<CsvReader, object?>(csv =>
                {
                    var args = argHandlers.ToDictionary(k => k.Key, v => v.Value.Invoke(csv));
                    var bespokeContext = new EvaluationContext(args);
                    var expressionResult = nb.Evaluate(bespokeContext);
                    if (expressionResult.IsSuccess)
                    {
                        return expressionResult.Result;
                    }

                    return TOMBSTONE;
                });
                propertyNamesHandler.Add(fc.SchemaPropertyName, bespokeHandler);
            }
            else
            {
                // This property should match a single CSV header column.
                var matchingCsvColumns = csv.HeaderRecord
                    .Select((hdr, idx) => new { hdr, idx })
                    .Where(x => x.hdr.Equals(fc.ImportFieldName, StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();

                if (fc.SkipRecordIfMissing && matchingCsvColumns.Length == 0)
                {
                    AmbientErrorContext.Provider.LogError($"File column '{fc.ImportFieldName}' is marked as required (SkipRecordIfMissing=true) but is missing in the CSV header.  This file will not be imported.");
                    yield break;
                }

                if (matchingCsvColumns.Length > 1)
                {
                    AmbientErrorContext.Provider.LogError($"File column '{fc.ImportFieldName}' appears multiple times in the CSV file.  This file will not be imported.");
                    yield break;
                }

                propertyNamesHandler.Add(fc.SchemaPropertyName, new Func<CsvReader, object?>(csv => csv.GetField(matchingCsvColumns[0].idx)));
            }
        }

        var rowCount = 0;
        while (await csv.ReadAsync())
        {
            rowCount++;
            var thingGuid = Guid.NewGuid().ToString();
            var thing = new Thing(thingGuid, $"Imported {rowCount}");

            foreach (var (propertyName, handler) in propertyNamesHandler)
            {
                var fieldConfig = importMap.FieldConfiguration.First(fc => fc.SchemaPropertyName != null && fc.SchemaPropertyName.Equals(propertyName));
                var value = handler.Invoke(csv);
                if (TOMBSTONE.Equals(value))
                {
                    if (fieldConfig.SkipRecordIfInvalid)
                    {
                        AmbientErrorContext.Provider.LogWarning($"Skipping row {rowCount}: Invalid value for field '{fieldConfig.ImportFieldName}'->{fieldConfig.SchemaPropertyName}.");
                        continue;
                    }
                    else
                    {
                        AmbientErrorContext.Provider.LogError($"Error processing row {rowCount}: Invalid value for field mapping '{fieldConfig.ImportFieldName}'->{fieldConfig.SchemaPropertyName}.");
                        yield break;
                    }
                }

                // Try to set the property value
                try
                {
                    string? inputValue;
                    if (value == null)
                    {
                        inputValue = null;
                    }
                    else if (value is string sv)
                    {
                        inputValue = sv;
                    }
                    else
                    {
                        inputValue = value.ToString();
                    }

                    // Handle reserved values.
                    if (propertyName.Equals("$Name"))
                    {
                        if (!string.IsNullOrWhiteSpace(inputValue))
                        {
                            thing.Name = inputValue;
                        }
                        else
                        {
                            AmbientErrorContext.Provider.LogError($"Error processing row {rowCount}: Invalid value '{value}' for the name of the {schema.Name} instance.");
                            yield break;
                        }
                    }
                    else
                    {
                        var tsr = await thing.Set(propertyName, inputValue, cancellationToken);
                        if (!tsr.Success)
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
            if (thing.IsDirty)
            {
                yield return (thing, rowCount);
            }
        }
    }
}