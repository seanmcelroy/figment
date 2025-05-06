using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;

namespace jot.Commands.Schemas;

public abstract class SchemaCancellableAsyncCommand<T> : CancellableAsyncCommand<T>
    where T : SchemaCommandSettings
{
    protected async Task<(Globals.GLOBAL_ERROR_CODES result, Schema? schema, ISchemaStorageProvider? ssp)> TryGetSchema(T settings, CancellationToken cancellationToken)
    {
        return await TryGetSchema(settings.SchemaName, cancellationToken);
    }

    public static async Task<(Globals.GLOBAL_ERROR_CODES result, Schema? schema, ISchemaStorageProvider? ssp)> TryGetSchema(string guidOrNamePart, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY) || selected.Type != Reference.ReferenceType.Schema)
        {
            if (string.IsNullOrWhiteSpace(guidOrNamePart))
            {
                AmbientErrorContext.Provider.LogError("To modify a schema, you must first 'select' one.");
                return (Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR, null, null);
            }

            var possibleSchemas = Schema.ResolveAsync(guidOrNamePart, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibleSchemas.Length)
            {
                case 0:
                    AmbientErrorContext.Provider.LogError("Nothing found with that name.");
                    return (Globals.GLOBAL_ERROR_CODES.NOT_FOUND, null, null);
                case 1:
                    selected = possibleSchemas[0].Reference;
                    break;
                default:
                    AmbientErrorContext.Provider.LogError("Ambiguous match; more than one entity matches this name.");
                    return (Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH, null, null);
            }
        }

        if (selected.Type != Reference.ReferenceType.Schema)
        {
            AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE, null, null);
        }

        var ssp = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (ssp == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
            return (Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR, null, null);
        }

        var schema = await ssp.LoadAsync(selected.Guid, cancellationToken);
        if (schema == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load schema with Guid '{selected.Guid}'.");
            return (Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR, null, ssp);
        }

        return (Globals.GLOBAL_ERROR_CODES.SUCCESS, schema, ssp);
    }
}