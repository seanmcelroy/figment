using System.Globalization;
using System.Runtime.CompilerServices;
using Figment.Common;
using Figment.Common.Errors;
using nietras.SeparatedValues;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas.ImportMaps;

/// <summary>
/// Creates a new import map to link file fields to <see cref="Schema"/> properties.
/// </summary>
public class NewImportMapCommand : SchemaCancellableAsyncCommand<NewImportMapCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, NewImportMapCommandSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ImportMapName))
        {
            AmbientErrorContext.Provider.LogError("Import map name must be specified.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        if (string.IsNullOrWhiteSpace(settings.FileType))
        {
            AmbientErrorContext.Provider.LogError("Import map name must be specified.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        if (string.IsNullOrWhiteSpace(settings.SampleFilePath))
        {
            AmbientErrorContext.Provider.LogError($"File path not specified.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var expandedPath = FileUtility.ExpandRelativePaths(settings.SampleFilePath);
        if (!File.Exists(expandedPath))
        {
            AmbientErrorContext.Provider.LogError($"File path '{expandedPath}' was not found.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        if (schema!.ImportMaps.Any(i => string.Equals(i.Name, settings.ImportMapName, StringComparison.InvariantCultureIgnoreCase)))
        {
            AmbientErrorContext.Provider.LogError($"Schema '{schema.Name}' already has an import map named '{settings.ImportMapName}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var importMap = new SchemaImportMap(settings.ImportMapName, settings.FileType);

        switch (settings.FileType.ToLowerInvariant())
        {
            case "csv":
                var csvFields = InferImportMapFieldsFromCsv(expandedPath, cancellationToken)
                    .ToBlockingEnumerable(cancellationToken);
                importMap.FieldConfiguration.AddRange(csvFields);
                break;
        }

        importMap.EnsureMetadataFields();
        schema!.ImportMaps.Add(importMap);
        var (saved, saveMessage) = await schema.SaveAsync(cancellationToken);
        if (!saved)
        {
            if (settings.Verbose ?? false)
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}' ({schema.Guid}): {saveMessage}");
            }
            else
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}': {saveMessage}");
            }

            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        AmbientErrorContext.Provider.LogDone($"Added new import map for '{importMap.Name}' to '{schema.Name}'.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }

    /// <summary>
    /// Infers the <see cref="SchemaImportField"/> objects from reading the header of a CSV file.
    /// </summary>
    /// <param name="filePath">Comma-separated value file with headers to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerator that returns each read file field with no property mapped.</returns>
    private static async IAsyncEnumerable<SchemaImportField> InferImportMapFieldsFromCsv(string filePath, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            yield break;
        }

        using var sr = new StreamReader(filePath);
        using var csvReader = await Sep.Reader(o => o with { CultureInfo = CultureInfo.InvariantCulture }).FromAsync(sr, cancellationToken);

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

        foreach (var header in csvReader.Header.ColNames)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return new SchemaImportField(null, header)
            {
                // By default, the property name is null, and thefore 'skip' settings are false.
                SkipRecordIfInvalid = false,
                SkipRecordIfMissing = false,
            };
        }
    }
}