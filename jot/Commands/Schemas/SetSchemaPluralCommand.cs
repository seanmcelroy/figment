using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// Sets the plural name for a <see cref="Schema"/>.
/// </summary>
public class SetSchemaPluralCommand : SchemaCancellableAsyncCommand<SetSchemaPluralCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaPluralCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, ssp) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        var oldPlural = schema!.Plural;

        schema.Plural = string.IsNullOrWhiteSpace(settings.Plural)
            ? null
            : settings.Plural.Trim();

        if (string.Equals(oldPlural, schema.Plural, StringComparison.InvariantCultureIgnoreCase))
        {
            AmbientErrorContext.Provider.LogWarning($"Plural for {schema.Name} is already '{schema.Plural}'. Nothing to do.");
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

        // For 'plural', we know we should rebuild indexes.
        await ssp!.RebuildIndexes(cancellationToken);

        if (schema.Plural == null)
        {
            AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Plural keyword was '{oldPlural}' but is now removed.");
        }
        else
        {
            AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Plural keyword was '{oldPlural}' but is now '{settings.Plural}'.");
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}