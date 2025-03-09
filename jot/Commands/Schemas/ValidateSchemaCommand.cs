using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class ValidateSchemaCommand : SchemaCancellableAsyncCommand<SchemaCommandSettings>
{
    private enum ERROR_CODES : int
    {
        SUCCESS = Globals.GLOBAL_ERROR_CODES.SUCCESS,
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
        SCHEMA_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SchemaCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
            return (int)tgs;

        if (settings.Verbose ?? false)
            AnsiConsole.WriteLine($"Validating schema {schema!.Name} ({schema.Guid}) ...");
        else
            AnsiConsole.WriteLine($"Validating schema {schema!.Name} ...");

        if (string.IsNullOrWhiteSpace(schema.Description))
            AmbientErrorContext.Provider.LogWarning("Description is not set, leading to an invalid JSON schema on disk.  Resolve with: [bold]set Description \"Sample description\"[/]");

        if (string.IsNullOrWhiteSpace(schema.Plural))
            AmbientErrorContext.Provider.LogWarning($"Plural is not set, rendering listing of all things with this schema on the REPL inaccessible.  Resolve with: [bold]set Plural {schema.Name.ToLowerInvariant()}s[/]");

        AmbientErrorContext.Provider.LogDone($"Validation has finished.");
        return (int)ERROR_CODES.SUCCESS;
    }
}