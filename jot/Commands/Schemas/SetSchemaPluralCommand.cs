using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class SetSchemaPluralCommand : SchemaCancellableAsyncCommand<SetSchemaPluralCommandSettings>
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

    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaPluralCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, ssp) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
            return (int)tgs;

        var oldPlural = schema!.Plural;

        schema.Plural = string.IsNullOrWhiteSpace(settings.Plural)
            ? null
            : settings.Plural;

        if (string.Compare(oldPlural, schema.Plural, StringComparison.InvariantCultureIgnoreCase) == 0)
        {
            AmbientErrorContext.Provider.LogWarning($"Plural for {schema.Name} is already '{schema.Plural}'. Nothing to do.");
            return (int)ERROR_CODES.SUCCESS;
        }

        var saved = await schema.SaveAsync(cancellationToken);
        if (!saved)
        {
            if (settings.Verbose ?? false)
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}' ({schema.Guid}).");
            else
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}'.");
            return (int)ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        // For 'plural', we know we should rebuild indexes.
        await ssp!.RebuildIndexes(cancellationToken);

        if (schema.Plural == null)
            AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Plural keyword was '{oldPlural}' but is now removed.");
        else
            AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Plural keyword was '{oldPlural}' but is now  '{settings.Plural}'.");
        return (int)ERROR_CODES.SUCCESS;
    }
}