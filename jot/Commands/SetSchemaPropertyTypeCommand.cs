using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSchemaPropertyTypeCommand : CancellableAsyncCommand<SetSchemaPropertyTypeCommandSettings>
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

    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaPropertyTypeCommandSettings settings, CancellationToken cancellationToken)
    {
        // set work phone=+1 (212) 555-5555
        // auto-selects text
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY) || selected.Type != Reference.ReferenceType.Schema)
        {
            if (string.IsNullOrWhiteSpace(settings.SchemaName))
            {
                AmbientErrorContext.Provider.LogError("To modify a thing, you must first 'select' one.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Schema.ResolveAsync(settings.SchemaName, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibilities.Length)
            {
                case 0:
                    AmbientErrorContext.Provider.LogError("Nothing found with that name.");
                    return (int)ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibilities[0];
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

        // Handle built-ins
        if (string.IsNullOrWhiteSpace(settings.FieldType))
        {
            // Deletes a property.
            var propToDelete = schemaLoaded.Properties.FirstOrDefault(p => string.Compare(p.Key, propName, StringComparison.OrdinalIgnoreCase) == 0);
            if (propToDelete.Equals(default(KeyValuePair<string, SchemaFieldBase>)))
            {
                AmbientErrorContext.Provider.LogError($"No property named '{propName}' found on schema '{schemaLoaded.Name}'");
                return (int)ERROR_CODES.NOT_FOUND;
            }

            schemaLoaded.Properties.Remove(propToDelete.Key);
            AmbientErrorContext.Provider.LogWarning($"Deleted property name '{propName}'.");
        }
        else if (string.CompareOrdinal(settings.FieldType, SchemaArrayField.SCHEMA_FIELD_TYPE) == 0)
        {
            // Array
            var saf = new SchemaArrayField(propName)
            {
                Items = new SchemaArrayField.SchemaArrayFieldItems
                {
                    Type = "string",
                }
            };
            schemaLoaded.Properties[propName] = saf;
        }
        else if (string.CompareOrdinal(settings.FieldType, SchemaBooleanField.SCHEMA_FIELD_TYPE) == 0)
        {
            // Boolean
            var sbf = new SchemaBooleanField(propName);
            schemaLoaded.Properties[propName] = sbf;
        }
        else if (string.CompareOrdinal(settings.FieldType, SchemaCalculatedField.SCHEMA_FIELD_TYPE) == 0)
        {
            // Calculated
            var scf = new SchemaCalculatedField(propName);
            schemaLoaded.Properties[propName] = scf;
            // Formula is null at this point.
        }
        else if (string.CompareOrdinal(settings.FieldType, SchemaDateField.SCHEMA_FIELD_TYPE) == 0)
        {
            // Date
            var sdf = new SchemaDateField(propName);
            schemaLoaded.Properties[propName] = sdf;
        }
        else if (string.CompareOrdinal(settings.FieldType, SchemaEmailField.SCHEMA_FIELD_TYPE) == 0)
        {
            // Email
            var sef = new SchemaEmailField(propName);
            schemaLoaded.Properties[propName] = sef;
        }
        else if (string.CompareOrdinal(settings.FieldType, SchemaIntegerField.SCHEMA_FIELD_TYPE) == 0)
        {
            // Number (integer)
            var sif = new SchemaIntegerField(propName);
            schemaLoaded.Properties[propName] = sif;
        }
        else if (string.CompareOrdinal(settings.FieldType, SchemaPhoneField.SCHEMA_FIELD_TYPE) == 0)
        {
            // Number (double)
            var snf = new SchemaNumberField(propName);
            schemaLoaded.Properties[propName] = snf;
        }
        else if (string.CompareOrdinal(settings.FieldType, SchemaPhoneField.SCHEMA_FIELD_TYPE) == 0)
        {
            // Phone
            var spf = new SchemaPhoneField(propName);
            schemaLoaded.Properties[propName] = spf;
        }
        else if (string.CompareOrdinal(settings.FieldType, SchemaSchemaField.SCHEMA_FIELD_TYPE) == 0)
        {
            // Email
            var ssf = new SchemaSchemaField(propName);
            schemaLoaded.Properties[propName] = ssf;
        }
        else if (string.CompareOrdinal(settings.FieldType, "text") == 0)
        {
            // Text
            var stf = new SchemaTextField(propName);
            schemaLoaded.Properties[propName] = stf;
        }
        else if (string.CompareOrdinal(settings.FieldType, SchemaUriField.SCHEMA_FIELD_TYPE) == 0)
        {
            // Uri
            var suf = new SchemaUriField(propName);
            schemaLoaded.Properties[propName] = suf;
        }
        else if (settings.FieldType != null 
            && settings.FieldType.StartsWith('[') 
            && settings.FieldType.EndsWith(']') 
            && settings.FieldType.Length >= 5 
            && settings.FieldType.Contains(','))
        {
            // Enum
            var enumValues = settings.FieldType[1..^1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var sef = new SchemaEnumField(propName, enumValues);
            schemaLoaded.Properties[propName] = sef;
        }
        else
        {
            // Maybe this is the name of a schema.
            var refSchema = await schemaStorageProvider.FindByNameAsync(settings.FieldType!, cancellationToken);
            if (refSchema == Reference.EMPTY)
            {
                AmbientErrorContext.Provider.LogError($"I do not understand that type of field ({settings.FieldType}).");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var srf = new SchemaRefField(propName, refSchema.Guid);
            schemaLoaded.Properties[propName] = srf;
        }

        var saved = await schemaLoaded.SaveAsync(cancellationToken);
        if (!saved)
        {
            AmbientErrorContext.Provider.LogError($"Unable to save schema with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        AmbientErrorContext.Provider.LogDone($"{schemaLoaded.Name} saved.");
        return (int)ERROR_CODES.SUCCESS;
    }
}