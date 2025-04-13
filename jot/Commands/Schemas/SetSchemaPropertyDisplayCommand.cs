using Figment.Common;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// Sets a 'pretty' display name for a schema field.
/// </summary>
public class SetSchemaPropertyDisplayCommand : SchemaCancellableAsyncCommand<SetSchemaPropertyDisplayCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaPropertyDisplayCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        // display Pizza
        // require pizza it-IT
        var propName = settings.PropertyName;
        var sp = schema!.Properties.FirstOrDefault(p => string.Compare(p.Key, propName, StringComparison.CurrentCultureIgnoreCase) == 0);
        if (sp.Equals(default(KeyValuePair<string, SchemaFieldBase>)))
        {
            AmbientErrorContext.Provider.LogError($"No schema field named '{propName}' was found.");
            return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
        }

        var prop = sp.Value;
        var culture = settings.Culture ?? "en-US";
        var whatChanged = string.Empty;
        if (string.IsNullOrWhiteSpace(settings.DisplayName))
        {
            // Remove display name if present.
            if (prop.DisplayNames != null
                && prop.DisplayNames.TryGetValue(culture, out string? disp))
            {
                whatChanged = $" Removed display name '{disp}' for culture {culture}";
                prop.DisplayNames.Remove(culture);
            }

            if (prop.DisplayNames?.Count == 0)
            {
                prop.DisplayNames = null;
            }
        }
        else if (prop.DisplayNames == null)
        {
            whatChanged = $" Added new display name '{settings.DisplayName}' for culture {culture}";
            prop.DisplayNames = new Dictionary<string, string>() { { culture, settings.DisplayName } };
        }
        else if (prop.DisplayNames.TryGetValue(culture, out string? value))
        {
            whatChanged = $" Changed display name from '{value}' to '{settings.DisplayName}' for culture {culture}";
            prop.DisplayNames[culture] = settings.DisplayName;
        }

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

        AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.{whatChanged}");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}