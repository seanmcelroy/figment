using Figment.Common;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas.ImportMaps;

/// <summary>
/// Lists all import maps defined on a <see cref="Schema"/>.
/// </summary>
public class LinkFileFieldToPropertyCommand : SchemaCancellableAsyncCommand<LinkFileFieldToPropertyCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, LinkFileFieldToPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        var verbose = settings.Verbose ?? Program.Verbose;

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

        importMap.EnsureMetadataFields();

        var configs = importMap.FieldConfiguration
            .Select((sif, idx) => new { SchemaImportField = sif, FieldConfigurationIndex = idx })
            .Where(fc =>
            {
                // Normal file field case, like "csv header" -> "property name"
                return (!string.IsNullOrWhiteSpace(fc.SchemaImportField.ImportFieldName)
                    && fc.SchemaImportField.ImportFieldName.Equals(settings.FileField, StringComparison.OrdinalIgnoreCase)
                    && (fc.SchemaImportField.SchemaPropertyName == null || !fc.SchemaImportField.SchemaPropertyName.StartsWith('$')))

                // Assignment to metadata property use case, like "formula" -> $Name
                || (!string.IsNullOrWhiteSpace(fc.SchemaImportField.SchemaPropertyName) && fc.SchemaImportField.SchemaPropertyName.StartsWith('$')
                    && fc.SchemaImportField.SchemaPropertyName.Equals(settings.SchemaProperty, StringComparison.OrdinalIgnoreCase));
            })
            .ToArray();

        if (configs.Length == 0)
        {
            if (string.IsNullOrEmpty(settings.SchemaProperty))
            {
                AmbientErrorContext.Provider.LogWarning($"No file field named '{settings.FileField}' was found, but since no schema property was provided, ignoring with no changes.");
                return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
            }

            AmbientErrorContext.Provider.LogWarning($"No file field named '{settings.FileField}' was found.  Creating the mapping anew.");
            importMap.FieldConfiguration.Add(new SchemaImportField(settings.SchemaProperty, settings.FileField));
        }
        else if (string.IsNullOrEmpty(settings.SchemaProperty))
        {
            // Config found, but schema property is set to null.
            foreach (var config in configs)
            {
                importMap.FieldConfiguration[config.FieldConfigurationIndex].SchemaPropertyName = null;
            }

            AmbientErrorContext.Provider.LogWarning($"Unmapped file field '{settings.FileField}'.");
        }
        else
        {
            // Special case, handle metadata properties.
            if (settings.SchemaProperty.StartsWith('$'))
            {
                foreach (var config in configs)
                {
                    var replacementSif = new SchemaImportField(
                        config.SchemaImportField.SchemaPropertyName,
                        settings.FileField)
                    {
                        SkipRecordIfInvalid = config.SchemaImportField.SkipRecordIfInvalid,
                        SkipRecordIfMissing = config.SchemaImportField.SkipRecordIfMissing,
                        TransformFormula = config.SchemaImportField.TransformFormula,
                        ValidationFormula = config.SchemaImportField.ValidationFormula,
                    };

                    importMap.FieldConfiguration[config.FieldConfigurationIndex] = replacementSif;
                }
            }
            else
            {
                // Normal case.
                var targetProperty = schema.Properties.FirstOrDefault(p => p.Key.Equals(settings.SchemaProperty, StringComparison.OrdinalIgnoreCase));
                if (targetProperty.Equals(default(KeyValuePair<string, SchemaFieldBase>)))
                {
                    AmbientErrorContext.Provider.LogError($"No schema field named '{settings.SchemaProperty}' was found.");
                    return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
                }

                foreach (var config in configs)
                {
                    importMap.FieldConfiguration[config.FieldConfigurationIndex].SchemaPropertyName = targetProperty.Key;
                }
            }
        }

        var (saved, saveMessage) = await schema.SaveAsync(cancellationToken);
        if (!saved)
        {
            if (verbose)
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}' ({schema.Guid}): {saveMessage}");
            }
            else
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}': {saveMessage}");
            }

            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        if (configs.Length == 1)
        {
            AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Import map '{importMap.Name}' file field '{settings.FileField}' now saves to '{configs[0].SchemaImportField.SchemaPropertyName}' on '{schema.Name}'.");
        }
        else
        {
            AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Import map '{importMap.Name}' file field '{settings.FileField}' now saves to {configs.Length} propertys on '{schema.Name}'.");
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}