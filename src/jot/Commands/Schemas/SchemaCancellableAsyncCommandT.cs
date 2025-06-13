using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;

namespace jot.Commands.Schemas;

/// <summary>
/// The base class implementation for cancelable asynchronous commands used by <see cref="Spectre.Console.Cli"/>.
/// </summary>
/// <typeparam name="T">The type of the settings this command requires.</typeparam>
public abstract class SchemaCancellableAsyncCommand<T> : CancellableAsyncCommand<T>
    where T : SchemaCommandSettings
{
    /// <summary>
    /// Attempts to load the <see cref="Schema"/> from settings implementation <typeparamref name="T"/>.
    /// </summary>
    /// <param name="settings">The settings from which to load the <see cref="Schema"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple with a result which can be returned by the entry point if it is an error, the schema, if loaded, and the storage provider, if constructed.</returns>
    protected async Task<(Globals.GLOBAL_ERROR_CODES result, Schema? schema, ISchemaStorageProvider? ssp)> TryGetSchema(T settings, CancellationToken cancellationToken)
    {
        return await TryGetSchema(settings.SchemaName, cancellationToken);
    }

    /// <summary>
    /// Attempts to load the <see cref="Schema"/> by name or reference.
    /// </summary>
    /// <param name="guidOrNamePart">The <see cref="Schema.Guid"/> or <see cref="Schema.Name"/> a schema to match and return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple with a result which can be returned by the entry point if it is an error, the schema, if loaded, and the storage provider, if constructed.</returns>
    public static async Task<(Globals.GLOBAL_ERROR_CODES result, Schema? schema, ISchemaStorageProvider? ssp)> TryGetSchema(string? guidOrNamePart, CancellationToken cancellationToken)
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

        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
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