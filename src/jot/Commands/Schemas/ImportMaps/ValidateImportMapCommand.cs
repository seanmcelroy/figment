using Figment.Common.Calculations.Parsing;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas.ImportMaps;

/// <summary>
/// Prints the details of an import map from a <see cref="Figment.Common.Schema"/> configuration.
/// </summary>
public class ValidateImportMapCommand : SchemaCancellableAsyncCommand<ValidateImportMapCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ValidateImportMapCommandSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ImportMapName))
        {
            AmbientErrorContext.Provider.LogError("Import map name must be specified.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        var importMap = schema!.ImportMaps.FirstOrDefault(i => string.Equals(i.Name, settings.ImportMapName, StringComparison.InvariantCultureIgnoreCase));

        if (importMap == null)
        {
            AmbientErrorContext.Provider.LogError($"Schema '{schema.Name}' does not have an import map named '{settings.ImportMapName}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
        }

        foreach (var fc in importMap.FieldConfiguration)
        {
            if (fc.SchemaPropertyName != null
                && fc.SchemaPropertyName.StartsWith('$')
                && fc.SkipRecordIfMissing
                && string.IsNullOrWhiteSpace(fc.ImportFieldName))
            {
                AmbientErrorContext.Provider.LogWarning($"Metadata property {fc.SchemaPropertyName} is required but has no file field or expression configured for it.");
            }
            else if (!string.IsNullOrWhiteSpace(fc.ImportFieldName)
                && string.IsNullOrWhiteSpace(fc.SchemaPropertyName)
                && fc.SkipRecordIfMissing)
            {
                AmbientErrorContext.Provider.LogWarning($"File field {fc.ImportFieldName} is marked required for import, but it is not associated with a schema property.");
            }

            // If the field name starts with '=', parse it as a formula.
            if (!string.IsNullOrWhiteSpace(fc.ImportFieldName)
                && fc.ImportFieldName.StartsWith('='))
            {
                if (string.IsNullOrWhiteSpace(fc.SchemaPropertyName))
                {
                    AmbientErrorContext.Provider.LogWarning($"Formula defined as '{fc.ImportFieldName}' but the schema property it targets is not set.");
                    continue;
                }

                if (!ExpressionParser.TryParse(fc.ImportFieldName, out NodeBase? node))
                {
                    AmbientErrorContext.Provider.LogWarning($"Unable to parse '{fc.ImportFieldName}' as an expression.");
                    continue;
                }

                if (!node.TryEvaluate(schema, out ExpressionResult expressionResult))
                {
                    AmbientErrorContext.Provider.LogWarning($"Unable to evaluate formula for property '{fc.SchemaPropertyName}': ({Enum.GetName(expressionResult.ErrorType)}) {expressionResult.Message}");
                    continue;
                }
            }
        }

        AmbientErrorContext.Provider.LogDone($"Validation has finished.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}