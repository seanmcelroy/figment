using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class SchemaRenameCommand : CancellableAsyncCommand<SchemaRenameCommandSettings>
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
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SchemaRenameCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY) || selected.Type != Reference.ReferenceType.Schema)
        {
            if (string.IsNullOrWhiteSpace(settings.SchemaName))
            {
                AmbientErrorContext.Provider.LogError("To rename a schema, you must first 'select' one.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibleSchemas = Schema.ResolveAsync(settings.SchemaName, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibleSchemas.Length)
            {
                case 0:
                    AmbientErrorContext.Provider.LogError("Nothing found with that name.");
                    return (int)ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibleSchemas[0];
                    break;
                default:
                    AmbientErrorContext.Provider.LogError("Ambiguous match; more than one schema matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (selected.Type != Reference.ReferenceType.Schema)
        {
            AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        var schemaStorageProvider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (schemaStorageProvider == null)
        {
            AmbientErrorContext.Provider.LogError("Unable to load schema storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var schema = await schemaStorageProvider.LoadAsync(selected.Guid, cancellationToken);
        if (schema == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load schema with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        if (string.IsNullOrWhiteSpace(settings.NewName))
        {
            AmbientErrorContext.Provider.LogError("Name of a schema cannot be empty.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        var oldName = schema.Name;
        schema.Name = settings.NewName.Trim();
        var saved = await schema.SaveAsync(cancellationToken);
        if (!saved)
        {
            AmbientErrorContext.Provider.LogError($"Unable to save schema with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        // For 'name', we know we should rebuild indexes.
        await schemaStorageProvider.RebuildIndexes(cancellationToken);
        AmbientErrorContext.Provider.LogDone($"Schema '{oldName}' renamed to '{schema.Name}'.  Please ensure your 'plural' value for this schema is accurate.");

        if (string.CompareOrdinal(Program.SelectedEntity.Guid, schema.Guid) == 0)
            Program.SelectedEntityName = schema.Name;

        return (int)ERROR_CODES.SUCCESS;
    }
}