using Figment.Common;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas.ImportMaps;

/// <summary>
/// Creates a new import map to link file fields to <see cref="Schema"/> properties.
/// </summary>
public class NewImportMapCommand : SchemaCancellableAsyncCommand<NewImportMapCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, NewImportMapCommandSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ImportMapName))
        {
            AmbientErrorContext.Provider.LogError("Import map name must be specified.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        if (string.IsNullOrWhiteSpace(settings.FileType))
        {
            AmbientErrorContext.Provider.LogError("Import map name must be specified.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        if (schema!.ImportMaps.Any(i => string.Compare(i.Name, settings.ImportMapName, StringComparison.InvariantCultureIgnoreCase) == 0))
        {
            AmbientErrorContext.Provider.LogError($"Schema '{schema.Name}' already has an import map named '{settings.ImportMapName}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var importMap = new SchemaImportMap(settings.ImportMapName, settings.FileType);

        schema!.ImportMaps.Add(importMap);
        var saved = await schema.SaveAsync(cancellationToken);
        if (!saved)
        {
            if (settings.Verbose ?? false)
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}' ({schema.Guid}).");
            }
            else
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}'.");
            }

            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        AmbientErrorContext.Provider.LogDone($"Added new import map for '{importMap.Name}' to '{schema.Name}'.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}