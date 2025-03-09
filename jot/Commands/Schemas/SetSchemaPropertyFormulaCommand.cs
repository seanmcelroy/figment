using Figment.Common;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class SetSchemaPropertyFormulaCommand : SchemaCancellableAsyncCommand<SetSchemaPropertyFormulaCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaPropertyFormulaCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, ssp) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
            return (int)tgs;

        var propName = settings.PropertyName;
        if (string.IsNullOrWhiteSpace(propName))
        {
            AmbientErrorContext.Provider.LogError("To change a property on a schema, specify the property's name.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var possibleProperties = schema!.Properties
            .Where(p => string.Compare(p.Key, settings.PropertyName, StringComparison.CurrentCultureIgnoreCase) == 0)
            .ToList();

        SchemaFieldBase? selectedProperty;

        switch (possibleProperties.Count)
        {
            case 0:
                AmbientErrorContext.Provider.LogError($"No property found with name '{settings.PropertyName}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
            case 1:
                selectedProperty = possibleProperties[0].Value;
                break;
            default:
                AmbientErrorContext.Provider.LogError($"Ambiguous match; more than one property matches the name '{settings.PropertyName}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
        }

        if (selectedProperty is not SchemaCalculatedField scf)
        {
            AmbientErrorContext.Provider.LogError($"Cannot set formula on property '{settings.PropertyName}' as it is not a 'calculated' field type.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        scf.Formula = settings.Formula;
        schema.Properties[propName] = scf;

        var saved = await schema.SaveAsync(cancellationToken);
        if (!saved)
        {
            AmbientErrorContext.Provider.LogError($"Unable to save schema with Guid '{schema.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Formula for '{settings.PropertyName}' updated.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}