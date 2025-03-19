using Figment.Common;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class SetSchemaPropertyRequiredCommand : SchemaCancellableAsyncCommand<SetSchemaPropertyRequiredCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaPropertyRequiredCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, ssp) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        // require "work phone" true 
        // require "work phone"
        // require "work phone" false 

        var propName = settings.PropertyName;
        var required = settings.Required;

        var sp = schema!.Properties.FirstOrDefault(p => string.Compare(p.Key, propName, StringComparison.CurrentCultureIgnoreCase) == 0);
        if (sp.Equals(default(KeyValuePair<string, SchemaFieldBase>)))
        {
            AmbientErrorContext.Provider.LogError($"No schema field named '{propName}' was found.");
            return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
        }

        sp.Value.Required = required;

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

        AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}