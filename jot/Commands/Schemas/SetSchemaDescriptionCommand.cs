using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class SetSchemaDescriptionCommand : SchemaCancellableAsyncCommand<SetSchemaDescriptionCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaDescriptionCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        var oldDescription = schema!.Description;

        schema.Description = string.IsNullOrWhiteSpace(settings.Description)
            ? null
            : settings.Description.Trim();

        if (string.Equals(oldDescription, schema.Description, StringComparison.InvariantCultureIgnoreCase))
        {
            AmbientErrorContext.Provider.LogWarning($"Description for {schema.Name} is already the same value. Nothing to do.");
            return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
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

        if (schema.Description == null)
        {
            AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Description was '{oldDescription}' but is now removed.");
        }
        else
        {
            AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Description was '{oldDescription}' but is now '{settings.Description}'.");
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}