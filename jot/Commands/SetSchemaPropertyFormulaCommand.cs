using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSchemaPropertyFormulaCommand : CancellableAsyncCommand<SetSchemaPropertyFormulaCommandSettings>
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

    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaPropertyFormulaCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY) || selected.Type != Reference.ReferenceType.Schema)
        {
            if (string.IsNullOrWhiteSpace(settings.SchemaName))
            {
                AmbientErrorContext.Provider.LogError("To update the formula for a field, you must first 'select' a schema.");
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
                    AmbientErrorContext.Provider.LogError("Ambiguous match; more than one entity matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        var propName = settings.PropertyName;
        if (string.IsNullOrWhiteSpace(propName))
        {
            AmbientErrorContext.Provider.LogError("To change a property on a schema, specify the property's name.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
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

        var schemaLoaded = await schemaStorageProvider.LoadAsync(selected.Guid, cancellationToken);
        if (schemaLoaded == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load schema with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        var possibleProperties = schemaLoaded.Properties
            .Where(p => string.Compare(p.Key, settings.PropertyName, StringComparison.CurrentCultureIgnoreCase) == 0)
            .ToList();

        SchemaFieldBase? selectedProperty;

        switch (possibleProperties.Count)
        {
            case 0:
                AmbientErrorContext.Provider.LogError($"No property found with name '{settings.PropertyName}'.");
                return (int)ERROR_CODES.NOT_FOUND;
            case 1:
                selectedProperty = possibleProperties[0].Value;
                break;
            default:
                AmbientErrorContext.Provider.LogError($"Ambiguous match; more than one property matches the name '{settings.PropertyName}'.");
                return (int)ERROR_CODES.AMBIGUOUS_MATCH;
        }

        if (selectedProperty is not SchemaCalculatedField scf)
        {
            AmbientErrorContext.Provider.LogError($"Cannot set formula on property '{settings.PropertyName}' as it is not a 'calculated' field type.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        scf.Formula = settings.Formula;
        schemaLoaded.Properties[propName] = scf;

        var saved = await schemaLoaded.SaveAsync(cancellationToken);
        if (!saved)
        {
            AmbientErrorContext.Provider.LogError($"Unable to save schema with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        AmbientErrorContext.Provider.LogDone($"{schemaLoaded.Name} saved.  Formula for '{settings.PropertyName}' updated.");
        return (int)ERROR_CODES.SUCCESS;
    }
}