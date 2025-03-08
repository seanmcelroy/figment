using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSchemaPropertyDisplayCommand : CancellableAsyncCommand<SetSchemaPropertyDisplayCommandSettings>
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

    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaPropertyDisplayCommandSettings settings, CancellationToken cancellationToken)
    {
        // display Pizza
        // require pizza it-IT 
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY) || selected.Type != Reference.ReferenceType.Schema)
        {
            if (string.IsNullOrWhiteSpace(settings.SchemaName))
            {
                AmbientErrorContext.Provider.LogError("To update the display name for a field, you must first 'select' a schema.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Schema.ResolveAsync(settings.SchemaName, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibilities.Length)
            {
                case 0:
                    AmbientErrorContext.Provider.LogError("Nothing found with that name");
                    return (int)ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibilities[0];
                    break;
                default:
                    AmbientErrorContext.Provider.LogError("Ambiguous match; more than one entity matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (selected.Type != Reference.ReferenceType.Schema)
        {
            AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load schema storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var schema = await provider.LoadAsync(selected.Guid, cancellationToken);
        if (schema == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load schema with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        var propName = settings.PropertyName;
        var sp = schema.Properties.FirstOrDefault(p => string.Compare(p.Key, propName, StringComparison.CurrentCultureIgnoreCase) == 0);
        if (sp.Equals(default(KeyValuePair<string, SchemaFieldBase>)))
        {
            AmbientErrorContext.Provider.LogError($"No schema field named '{propName}' was found.");
            return (int)ERROR_CODES.NOT_FOUND;
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
                prop.DisplayNames = null;
        }
        else if (prop.DisplayNames == null)
        {
            whatChanged = $" Added new display name '{settings.DisplayName}' for culture {culture}";
            prop.DisplayNames = new Dictionary<string, string>() { { culture, settings.DisplayName } };
        }
        else if (prop.DisplayNames.ContainsKey(culture)) {
            whatChanged = $" Changed display name from '{prop.DisplayNames[culture]}' to '{settings.DisplayName}' for culture {culture}";
            prop.DisplayNames[culture] = settings.DisplayName;
        }

        var saved = await schema.SaveAsync(cancellationToken);
        if (!saved)
        {
            AmbientErrorContext.Provider.LogError($"Unable to save schema with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.{whatChanged}");
        return (int)ERROR_CODES.SUCCESS;
    }
}