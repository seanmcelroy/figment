using System.Globalization;
using CsvHelper;
using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ImportSchemaThingsCommand : CancellableAsyncCommand<ImportSchemaThingsCommandSettings>
{
    private enum ERROR_CODES : int
    {
        SUCCESS = Globals.GLOBAL_ERROR_CODES.SUCCESS,
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
        SCHEMA_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR,
        SCHEMA_SAVE_ERROR = Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR,
        THING_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR,
        THING_SAVE_ERROR = Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ImportSchemaThingsCommandSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.FilePath)
            || !File.Exists(settings.FilePath))
        {
            AmbientErrorContext.Provider.LogError($"File path '{settings.FilePath}' was not found.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        if (string.IsNullOrWhiteSpace(settings.SchemaName))
        {
            AmbientErrorContext.Provider.LogError("Schema name must be specified.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        var schemaPossibilities = Schema.ResolveAsync(settings.SchemaName, cancellationToken)
            .ToBlockingEnumerable(cancellationToken)
            .ToArray();

        Schema? schema;
        switch (schemaPossibilities.Length)
        {
            case 0:
                AmbientErrorContext.Provider.LogError($"No schema found named '{settings.SchemaName}'");
                return (int)ERROR_CODES.NOT_FOUND;
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
                        return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
                    }
                    break;
                }
            default:
                AmbientErrorContext.Provider.LogError("Ambiguous match; more than one schema matches this name.");
                return (int)ERROR_CODES.AMBIGUOUS_MATCH;
        }

        IAsyncEnumerable<Thing> things;

        // Try to determine file format
        var fileFormat = settings.Format;
        if (string.IsNullOrWhiteSpace(settings.Format))
        {
            // Extension
            if (string.Compare(Path.GetExtension(settings.FilePath), "csv", StringComparison.InvariantCultureIgnoreCase) == 0)
                fileFormat = "csv";
        }

        var possibleImportMaps = schema.ImportMaps
            .Where(s => string.Compare(s.Format, settings.Format, StringComparison.InvariantCultureIgnoreCase) == 0)
            .ToArray();

        if (possibleImportMaps.Length == 0)
        {
            AmbientErrorContext.Provider.LogError($"No import configuration are defined on {schema.Name} for {fileFormat ?? "this"} file");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }


        if (string.Compare(fileFormat, "csv", StringComparison.InvariantCultureIgnoreCase) == 0)
            things = ImportCsv(settings.FilePath, schema);
        else
        {
            AmbientErrorContext.Provider.LogError($"Unsupported format '{settings.Format}'.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }


        await foreach (var thing in things)
        {

        }



        return (int)ERROR_CODES.SUCCESS;
    }

    private static async IAsyncEnumerable<Thing> ImportCsv(
        string filePath,
        Schema schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(schema);

        if (!File.Exists(filePath))
            yield break;

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