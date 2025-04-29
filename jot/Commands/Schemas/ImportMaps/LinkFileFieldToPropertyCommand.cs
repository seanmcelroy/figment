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

        var configs = importMap.FieldConfiguration
            .Where(fc => fc.ImportFieldName.Equals(settings.FileField, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (configs.Length == 0)
        {
            if (string.IsNullOrEmpty(settings.SchemaProperty))
            {
                AmbientErrorContext.Provider.LogWarning($"No file field named '{settings.FileField}' was found, but since no schema property was provided, ignoring with no changes.");
                return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
            }

            AmbientErrorContext.Provider.LogWarning($"No file field named '{settings.FileField}' was found.  Creating the mapping anew.");
            configs = [
                new SchemaImportField(settings.SchemaProperty, settings.FileField)
            ];
        }
        else if (string.IsNullOrEmpty(settings.SchemaProperty))
        {
            // Config found, but schema property is set to null.
            foreach (var config in configs)
            {
                config.SchemaPropertyName = null;
            }

            AmbientErrorContext.Provider.LogWarning($"Unmapping file field '{settings.FileField}'.");
        }
        else
        {
            var targetProperty = schema.Properties.FirstOrDefault(p => p.Key.Equals(settings.SchemaProperty, StringComparison.OrdinalIgnoreCase));
            if (targetProperty.Equals(default(KeyValuePair<string, SchemaFieldBase>)))
            {
                AmbientErrorContext.Provider.LogError($"No schema field named '{settings.SchemaProperty}' was found.");
                return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
            }

            foreach (var config in configs)
            {
                config.SchemaPropertyName = targetProperty.Key;
            }
        }

        var saved = await schema.SaveAsync(cancellationToken);
        if (!saved)
        {
            if (verbose)
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}' ({schema.Guid}).");
            }
            else
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}'.");
            }

            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        if (configs.Length == 1)
        {
            AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Import map '{importMap.Name}' file field '{settings.FileField}' now saves to '{configs[0].SchemaPropertyName}' on '{schema.Name}'.");
        }
        else
        {
            AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Import map '{importMap.Name}' file field '{settings.FileField}' now saves to {configs.Length} propertys on '{schema.Name}'.");
        }
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}