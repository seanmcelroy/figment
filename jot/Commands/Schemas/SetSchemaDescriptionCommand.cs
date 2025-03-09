using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class SetSchemaDescriptionCommand : SchemaCancellableAsyncCommand<SetSchemaDescriptionCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaDescriptionCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
            return (int)tgs;

        schema!.Description = settings.Description;
        var saved = await schema.SaveAsync(cancellationToken);
        if (!saved)
        {
            if (settings.Verbose ?? false)
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}' ({schema.Guid}).");
            else
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}