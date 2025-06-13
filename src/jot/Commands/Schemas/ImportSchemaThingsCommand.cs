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

using System.Runtime.CompilerServices;
using Figment.Common;
using Figment.Common.Calculations.Parsing;
using Figment.Common.Data;
using Figment.Common.Errors;
using nietras.SeparatedValues;
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
        if (string.IsNullOrWhiteSpace(settings.FilePath))
        {
            AmbientErrorContext.Provider.LogError($"File path not specified.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var expandedPath = FileUtility.ExpandRelativePaths(settings.FilePath);
        if (!File.Exists(expandedPath))
        {
            AmbientErrorContext.Provider.LogError($"File path '{expandedPath}' was not found.");
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
            if (string.Equals(Path.GetExtension(expandedPath), ".csv", StringComparison.InvariantCultureIgnoreCase))
            {
                fileFormat = "csv";
            }
        }

        var possibleImportMaps = schema.ImportMaps
            .Where(s => string.Equals(s.Format, fileFormat, StringComparison.InvariantCultureIgnoreCase))
            .ToArray();

        if (possibleImportMaps.Length == 0)
        {
            AmbientErrorContext.Provider.LogError($"No import configuration are defined on {schema.Name} for {fileFormat ?? "this"} file");
            return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }

        // Select import map
        SchemaImportMap importMap;
        {
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
                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
            }
        }

        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        Queue<Action> postProgressActionQueue = new();

        var result = await AnsiConsole.Progress()
            .Columns(
            [
                new TaskDescriptionColumn(),    // Task description
                new ProgressBarColumn(),        // Progress bar
                new PercentageColumn(),         // Percentage
                new ElapsedTimeColumn(),        // Elapsed time
            ])
            .StartAsync(
            async ctx =>
            {
                var overviewTask = ctx.AddTask("Importing data")
                    .IsIndeterminate(true);
                {
                    var rebuildIndexTask = ctx.AddTask("Rebuilding thing indexes pre-import")
                        .IsIndeterminate(true);

                    var success = await tsp.RebuildIndexes(cancellationToken);
                    rebuildIndexTask.MaxValue(rebuildIndexTask.Value);
                    rebuildIndexTask.StopTask();
                    if (!success)
                    {
                        overviewTask.MaxValue(overviewTask.Value);
                        overviewTask.StopTask();
                        postProgressActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogError($"Unable to reindex things.  Import aborted."));
                        return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
                    }
                }

                if (string.Equals(fileFormat, "csv", StringComparison.InvariantCultureIgnoreCase))
                {
                    things = ImportCsv(expandedPath, schema, importMap, settings.CsvFromRow, settings.CsvToRow, ctx, cancellationToken);
                }
                else
                {
                    overviewTask.MaxValue(overviewTask.Value);
                    overviewTask.StopTask();
                    postProgressActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogError($"Unsupported format '{fileFormat}'."));
                    return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
                }

                // This implements partial imports.
                // This will skip settings.RecordsToSkip records, and only take settings.RecordsToImport records, if specified.
                var thingsToDelete = new HashSet<Reference>();
                var thingsToImport = new List<(Thing thing, int rowNumber)>();
                {
                    var count = 0;
                    await foreach (var (thing, rowNumber) in things)
                    {
                        count++;
                        if (count <= (settings.RecordsToSkip ?? 0))
                        {
                            continue;
                        }

                        thingsToImport.Add((thing, rowNumber));

                        if (settings.RecordsToImport != null && thingsToImport.Count >= settings.RecordsToImport)
                        {
                            break;
                        }
                    }
                }

                // Are there duplicates in the import batch?  We can test this without touching the current data store.
                {
                    var dupeTask = ctx.AddTask("Analyzing imported records for dupes")
                        .IsIndeterminate(true);

                    var dupes = thingsToImport
                        .GroupBy(i => i.thing.Name, StringComparer.InvariantCultureIgnoreCase)
                        .Where(i => i.Count() > 1)
                        .Select(i => new { name = i.Key, count = i.Count(), rows = string.Join(',', i.Select(r => $"{r.rowNumber}")) });

                    if (dupes.Any())
                    {
                        switch (settings.DupeStrategy)
                        {
                            case "skip":
                                // Print warnings but fall through.
                                foreach (var dupe in dupes)
                                {
                                    postProgressActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogWarning($"Ignoring {dupe.count} duplicates in file for '{dupe.name}' on rows {dupe.rows}."));
                                }

                                // Ignore dupes by culling them out.
                                thingsToImport = [.. thingsToImport
                                    .GroupBy(i => i.thing.Name, StringComparer.InvariantCultureIgnoreCase)
                                    .Where(i => i.Count() == 1) // We do not want to keep ANY dupes here.
                                    .Select(i => (i.First().thing, i.First().rowNumber))];
                                break;
                            case "merge":
                                // There might be multiple dupes, but we aggregate them all together.
                                thingsToImport = [.. thingsToImport
                                    .GroupBy(i => i.thing.Name, StringComparer.InvariantCultureIgnoreCase)
                                    .Select(i => (i.Select(j => j.thing).Aggregate((c, n) => c.Merge(n)), i.Last().rowNumber))];
                                break;
                            case "overwrite":
                                // Since we are at the pre-store check, overwrite will pick the last of the same name where dupes do exist.
                                thingsToImport = [.. thingsToImport
                                    .GroupBy(i => i.thing.Name, StringComparer.InvariantCultureIgnoreCase)
                                    .Select(i => (i.Last().thing, i.Last().rowNumber))];
                                break;
                            default:
                                // STOP. Print errors and exit.
                                foreach (var dupe in dupes)
                                {
                                    postProgressActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogError($"{dupe.count} duplicates in file for '{dupe.name}' on rows {dupe.rows}."));
                                }

                                dupeTask.MaxValue(dupeTask.Value);
                                dupeTask.StopTask();
                                overviewTask.MaxValue(overviewTask.Value);
                                overviewTask.StopTask();
                                postProgressActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogError($"Aborted file import: No things were created."));
                                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR; // TODO: Return a more specific error code.
                        }
                    }

                    dupeTask.MaxValue(dupeTask.Value);
                    dupeTask.StopTask();
                }

                // Now check dupes against the data store.
                var thingsToImportReal = new List<(Thing thing, int rowNumber)>();
                {
                    var dupeTask = ctx.AddTask("Analyzing for dupes in data store")
                        .MaxValue(thingsToImport.Count);

                    foreach (var (thing, rowNumber) in thingsToImport)
                    {
                        dupeTask.Increment(1);

                        // Before we save it, is there already an object with the same name?
                        var existing = await tsp.FindByNameAsync(thing.Name, cancellationToken);
                        if (existing != Reference.EMPTY)
                        {
                            switch (settings.DupeStrategy)
                            {
                                case "skip":
                                    // Print warnings but fall through.
                                    postProgressActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogWarning($"Skipping row {rowNumber}: Another object with the same name already exists. ('{thing.Name}')"));
                                    break;
                                case "merge":
                                    var existingThing = await tsp.LoadAsync(existing.Guid, cancellationToken);
                                    if (existingThing != null)
                                    {
                                        existingThing.Merge(thing);
                                        thingsToImportReal.Add((existingThing, rowNumber));
                                        postProgressActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogWarning($"Merging row {rowNumber}: Another object with the same name already exists. ('{thing.Name}', GUID '{existing.Guid}')  This row will be merged into it."));
                                    }

                                    break;
                                case "overwrite":
                                    thingsToDelete.Add(existing);
                                    thingsToImportReal.Add((thing, rowNumber));
                                    postProgressActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogWarning($"Superceding row {rowNumber}: Another object with the same name already exists. ('{thing.Name}', GUID '{existing.Guid}')  It will be overwritten by this row."));
                                    break;
                                default: // Stop
                                    postProgressActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogError($"Conflict on row {rowNumber}: Another object with the same name already exists. ('{thing.Name}')"));
                                    dupeTask.MaxValue(dupeTask.Value);
                                    dupeTask.StopTask();
                                    overviewTask.MaxValue(overviewTask.Value);
                                    overviewTask.StopTask();
                                    postProgressActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogError($"Aborted file import: No things were created."));
                                    return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR; // TODO: Return a more specific error code.
                            }

                            continue;
                        }
                        else
                        {
                            // No dupe, no special handling required.
                            thingsToImportReal.Add((thing, rowNumber));
                        }
                    }

                    dupeTask.MaxValue(dupeTask.Value);
                    dupeTask.StopTask();
                }

                // Now save them.
                var savedCount = 0;
                var importJobId = Guid.NewGuid().ToString();
                {
                    var saveTask = ctx.AddTask("Saving items to data store")
                        .MaxValue(thingsToImportReal.Count);

                    foreach (var (thing, rowNumber) in thingsToImportReal)
                    {
                        saveTask.Increment(1);

                        await thing.Set("ImportJobID", importJobId, cancellationToken);
                        if (settings.DryRun ?? false)
                        {
                            savedCount++;
                        }
                        else
                        {
                            var (saved, saveMessage) = await tsp.SaveAsync(thing, cancellationToken);
                            if (!saved)
                            {
                                postProgressActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogError($"Failed to save thing from row {rowNumber}: {saveMessage}"));
                                continue;
                            }
                            else
                            {
                                savedCount++;
                            }
                        }
                    }

                    saveTask.MaxValue(saveTask.Value);
                    saveTask.StopTask();
                }

                if (settings.DryRun ?? false)
                {
                    postProgressActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogDone($"Dry Run: Would have imported {savedCount} rows from '{expandedPath}'."));
                }
                else
                {
                    postProgressActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogDone($"Imported {savedCount} rows from '{expandedPath}' as import job ID {importJobId}."));

                    var rebuildIndexTask = ctx.AddTask("Rebuilding thing indexes post-import")
                        .IsIndeterminate(true);

                    var success = await tsp.RebuildIndexes(cancellationToken);
                    rebuildIndexTask.MaxValue(rebuildIndexTask.Value);
                    rebuildIndexTask.StopTask();
                }

                overviewTask.MaxValue(overviewTask.Value);
                overviewTask.StopTask();
                return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
            });

        while (postProgressActionQueue.Count > 0)
        {
            var action = postProgressActionQueue.Dequeue();
            action.Invoke();
        }

        return result;
    }

    private static async IAsyncEnumerable<(Thing thing, int rowNumber)> ImportCsv(
        string filePath,
        Schema schema,
        SchemaImportMap importMap,
        int? csvFromRow,
        int? csvToRow,
        ProgressContext ctx,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(schema);

        if (!File.Exists(filePath))
        {
            yield break;
        }

        await using var fs = File.OpenRead(filePath);
        var fileRowCount = FileUtility.CountLines(fs);
        fs.Seek(0, SeekOrigin.Begin); // Rewind to beginning of file.

        using var reader = new StreamReader(fs);
        using var csvReader = await Sep.Reader().FromAsync(reader, cancellationToken);

        if (!csvReader.HasHeader || csvReader.Header == null || csvReader.Header.IsEmpty)
        {
            AmbientErrorContext.Provider.LogError($"Unable to read CSV file headers from {filePath}");
            yield break;
        }

        if (!csvReader.HasRows)
        {
            AmbientErrorContext.Provider.LogError($"Unable to read CSV file {filePath}: File has no rows.");
            yield break;
        }

        // Dump headers
        // AmbientErrorContext.Provider.LogDebug($"Headers: {string.Join(',', csv.HeaderRecord)}");

        // Building index map
        var propertyNamesHandler = new Dictionary<string, Func<SepReader, object?>>();
        {
            var buildIndexMapTask = ctx.AddTask("Building index map")
                .MaxValue(importMap.FieldConfiguration.Count);

            foreach (var fc in importMap.FieldConfiguration)
            {
                buildIndexMapTask.Increment(1);

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
                    var argHandlers = new Dictionary<string, Func<SepReader, string?>>();
                    foreach (var formFieldRef in formulaFieldReferences)
                    {
                        var matchingCsvColumns = csvReader.Header.ColNames
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
                            argHandlers.Add(formFieldRef, cr => cr.Current[matchingCsvColumns[0].idx].ToString());
                        }
                    }

                    var bespokeHandler = new Func<SepReader, object?>(csv =>
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
                    var matchingCsvColumns = csvReader.Header.ColNames
                        .Select((hdr, idx) => new { hdr, idx })
                        .Where(x => x.hdr.Equals(fc.ImportFieldName, StringComparison.InvariantCultureIgnoreCase))
                        .ToArray();

                    if (fc.SkipRecordIfMissing && matchingCsvColumns.Length == 0)
                    {
                        AmbientErrorContext.Provider.LogError($"File column '{fc.ImportFieldName}' is marked as required (SkipRecordIfMissing=true) but is missing in the CSV header.  This file will not be imported.");
                        yield break;
                    }

                    switch (matchingCsvColumns.Length)
                    {
                        case 0:
                            AmbientErrorContext.Provider.LogError($"File column '{fc.ImportFieldName}' missing in file.  This file will not be imported.");
                            yield break;
                        case 1:
                            propertyNamesHandler.Add(fc.SchemaPropertyName, new Func<SepReader, object?>(csv => csv.Current[matchingCsvColumns[0].idx].ToString()));
                            break;
                        default: // > 1
                            AmbientErrorContext.Provider.LogError($"File column '{fc.ImportFieldName}' appears multiple times in the CSV file.  This file will not be imported.");
                            yield break;
                    }
                }
            }

            buildIndexMapTask.MaxValue(buildIndexMapTask.Value);
            buildIndexMapTask.StopTask();
        }

        string readStatusMessage;
        if (csvFromRow.HasValue)
        {
            if (csvToRow.HasValue)
            {
                readStatusMessage = $"Reading CSV file rows {csvFromRow.Value} to {csvToRow.Value}";
            }
            else
            {
                readStatusMessage = $"Reading CSV file starting at row {csvFromRow.Value}";
            }
        }
        else
        {
            if (csvToRow.HasValue)
            {
                readStatusMessage = $"Reading CSV file rows 1 to {csvToRow.Value}";
            }
            else
            {
                readStatusMessage = $"Reading CSV file rows";
            }
        }

        {
            var readCsvRowTask = ctx.AddTask(readStatusMessage)
                .MaxValue(fileRowCount);

            var rowCount = 0;
            while (await csvReader.MoveNextAsync(cancellationToken))
            {
                rowCount++;

                // Apply row range filtering
                if (csvFromRow.HasValue && rowCount < csvFromRow.Value)
                {
                    continue;
                }
                else if (csvToRow.HasValue && rowCount > csvToRow.Value)
                {
                    break;
                }

                var thingGuid = Guid.NewGuid().ToString();
                var thing = new Thing(thingGuid, $"Imported {rowCount}");
                thing.SchemaGuids.Add(schema.Guid);

                var skipThisRow = false;
                foreach (var (propertyName, handler) in propertyNamesHandler)
                {
                    if (skipThisRow)
                    {
                        break;
                    }

                    var fieldConfig = importMap.FieldConfiguration.FirstOrDefault(fc => fc.SchemaPropertyName != null && fc.SchemaPropertyName.Equals(propertyName));
                    if (fieldConfig == null)
                    {
                        AmbientErrorContext.Provider.LogError($"No field configuration found for property '{propertyName}' in row {rowCount}.");
                        yield break;
                    }

                    var value = handler.Invoke(csvReader);
                    if (TOMBSTONE.Equals(value))
                    {
                        if (fieldConfig.SkipRecordIfInvalid)
                        {
                            skipThisRow = true;
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
                                    skipThisRow = true;
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
                            skipThisRow = true;
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
                if (!skipThisRow && thing.IsDirty)
                {
                    yield return (thing, rowCount);
                }
            }

            readCsvRowTask.MaxValue(readCsvRowTask.Value);
            readCsvRowTask.StopTask();
        }
    }
}