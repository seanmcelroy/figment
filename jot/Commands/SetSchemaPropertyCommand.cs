using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSchemaPropertyCommand : CancellableAsyncCommand<SetSchemaPropertyCommandSettings>
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

    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        // set work phone=+1 (212) 555-5555
        // auto-selects text
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY) || selected.Type != Reference.ReferenceType.Schema)
        {
            if (string.IsNullOrWhiteSpace(settings.SchemaName))
            {
                AmbientErrorContext.ErrorProvider.LogError("To view properties on a thing, you must first 'select' a thing.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Schema.ResolveAsync(settings.SchemaName, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibilities.Length)
            {
                case 0:
                    AmbientErrorContext.ErrorProvider.LogError("Nothing found with that name.");
                    return (int)ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibilities[0];
                    break;
                default:
                    AmbientErrorContext.ErrorProvider.LogError("Ambiguous match; more than one entity matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        var propName = settings.PropertyName;
        if (string.IsNullOrWhiteSpace(propName))
        {
            AmbientErrorContext.ErrorProvider.LogError("To change a property on a schema, specify the property's name.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        if (selected.Type != Reference.ReferenceType.Schema)
        {
            AmbientErrorContext.ErrorProvider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        var schemaStorageProvider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (schemaStorageProvider == null)
        {
            AmbientErrorContext.ErrorProvider.LogError("Unable to load schema storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var schemaLoaded = await schemaStorageProvider.LoadAsync(selected.Guid, cancellationToken);
        if (schemaLoaded == null)
        {
            AmbientErrorContext.ErrorProvider.LogError($"Unable to load schema with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        var propType = settings.Value;

        // Handle built-ins
        if (string.Compare("name", propName, StringComparison.CurrentCultureIgnoreCase) == 0)
        {
            // Built-in
            if (string.IsNullOrWhiteSpace(propType))
            {
                AmbientErrorContext.ErrorProvider.LogError("The name of a schema cannot be empty.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            schemaLoaded.Name = propType;
            await schemaLoaded.SaveAsync(cancellationToken);

            // For 'name', we know we should rebuild indexes.
            await schemaStorageProvider.RebuildIndexes(cancellationToken);
        }
        else if (string.Compare("plural", propName, StringComparison.CurrentCultureIgnoreCase) == 0)
        {
            // Built-in
            schemaLoaded.Plural = propType;
            await schemaLoaded.SaveAsync(cancellationToken);

            // For 'plural', we know we should rebuild indexes.
            await schemaStorageProvider.RebuildIndexes(cancellationToken);
        }
        else if (string.Compare("description", propName, StringComparison.CurrentCultureIgnoreCase) == 0)
        {
            // Built-in
            schemaLoaded.Description = propType;
            await schemaLoaded.SaveAsync(cancellationToken);
        }
        else if (string.IsNullOrWhiteSpace(propType))
        {
            // Deletes a property.
            var propToDelete = schemaLoaded.Properties.FirstOrDefault(p => string.Compare(p.Key, propName, StringComparison.OrdinalIgnoreCase) == 0);
            if (propToDelete.Equals(default(KeyValuePair<string, SchemaFieldBase>)))
            {
                AmbientErrorContext.ErrorProvider.LogError($"No property named '{propName}' found on schema '{schemaLoaded.Name}'");
                return (int)ERROR_CODES.NOT_FOUND;
            }

            schemaLoaded.Properties.Remove(propToDelete.Key);
            AmbientErrorContext.ErrorProvider.LogWarning($"Deleted property name '{propName}'.");
        }
        else if (string.CompareOrdinal(propType, "array") == 0)
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
        else if (
            string.CompareOrdinal(propType, "bool") == 0
            || string.CompareOrdinal(propType, "boolean") == 0
            )
        {
            // Boolean
            var sbf = new SchemaBooleanField(propName);
            schemaLoaded.Properties[propName] = sbf;
        }
        else if (
            string.CompareOrdinal(propType, "calc") == 0
            || string.CompareOrdinal(propType, "calculated") == 0
            )
        {
            // Calculated
            var scf = new SchemaCalculatedField(propName);
            schemaLoaded.Properties[propName] = scf;
        }
        else if (string.CompareOrdinal(propType, "date") == 0)
        {
            // Date
            var sdf = new SchemaDateField(propName);
            schemaLoaded.Properties[propName] = sdf;
        }
        else if (string.CompareOrdinal(propType, "email") == 0)
        {
            // Email
            var sef = new SchemaEmailField(propName);
            schemaLoaded.Properties[propName] = sef;
        }
        else if (
            string.CompareOrdinal(propType, "integer") == 0
            || string.CompareOrdinal(propType, "int") == 0
        )
        {
            // Number (integer)
            var sif = new SchemaIntegerField(propName);
            schemaLoaded.Properties[propName] = sif;
        }
        else if (
            string.CompareOrdinal(propType, "number") == 0
            || string.CompareOrdinal(propType, "num") == 0
            || string.CompareOrdinal(propType, "double") == 0
            || string.CompareOrdinal(propType, "float") == 0
        )
        {
            // Number (double)
            var snf = new SchemaNumberField(propName);
            schemaLoaded.Properties[propName] = snf;
        }
        else if (string.CompareOrdinal(propType, "phone") == 0)
        {
            // Phone
            var spf = new SchemaPhoneField(propName);
            schemaLoaded.Properties[propName] = spf;
        }
        else if (string.CompareOrdinal(propType, "schema") == 0)
        {
            // Email
            var ssf = new SchemaSchemaField(propName);
            schemaLoaded.Properties[propName] = ssf;
        }
        else if (string.CompareOrdinal(propType, "text") == 0)
        {
            // Text
            var stf = new SchemaTextField(propName);
            schemaLoaded.Properties[propName] = stf;
        }
        else if (string.CompareOrdinal(propType, "uri") == 0)
        {
            // Uri
            var suf = new SchemaUriField(propName);
            schemaLoaded.Properties[propName] = suf;
        }
        else if (propType != null && propType.StartsWith('[') && propType.EndsWith(']') && propType.Length >= 5 && propType.Contains(','))
        {
            // Enum
            var enumValues = propType[1..^1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var sef = new SchemaEnumField(propName, enumValues);
            schemaLoaded.Properties[propName] = sef;
        }
        else
        {
            // Maybe this is the name of a schema.
            var refSchema = await schemaStorageProvider.FindByNameAsync(propType!, cancellationToken);
            if (refSchema == Reference.EMPTY)
            {
                AmbientErrorContext.ErrorProvider.LogError($"I do not understand that type of field ({propType}).");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var srf = new SchemaRefField(propName, refSchema.Guid);
            schemaLoaded.Properties[propName] = srf;
        }

        var saved = await schemaLoaded.SaveAsync(cancellationToken);
        if (!saved)
        {
            AmbientErrorContext.ErrorProvider.LogError($"Unable to save schema with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        AmbientErrorContext.ErrorProvider.LogDone($"{schemaLoaded.Name} saved.");
        return (int)ERROR_CODES.SUCCESS;
    }
}