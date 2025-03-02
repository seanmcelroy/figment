using Figment.Common;
using Figment.Common.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class RequireSchemaPropertyCommand : CancellableAsyncCommand<RequireSchemaPropertyCommandSettings>
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

    public override async Task<int> ExecuteAsync(CommandContext context, RequireSchemaPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        // require "work phone" true 
        // require "work phone"
        // require "work phone" false 
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY) || selected.Type != Reference.ReferenceType.Schema)
        {
            if (string.IsNullOrWhiteSpace(settings.SchemaName))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To update a property on a schema, you must first 'select' a schema.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Schema.ResolveAsync(settings.SchemaName, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibilities.Length)
            {
                case 0:
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Nothing found with that name");
                    return (int)ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibilities[0];
                    break;
                default:
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one entity matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (selected.Type != Reference.ReferenceType.Schema)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var schemaLoaded = await provider.LoadAsync(selected.Guid, cancellationToken);
        if (schemaLoaded == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        var propName = settings.PropertyName;
        var required = settings.Required ?? true;

        var sp = schemaLoaded.Properties.FirstOrDefault(p => string.Compare(p.Key, propName, StringComparison.CurrentCultureIgnoreCase) == 0);
        if (sp.Equals(default(KeyValuePair<string, SchemaFieldBase>)))
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: No schema field named '{propName}' was found.");
            return (int)ERROR_CODES.NOT_FOUND;
        }

        sp.Value.Required = required;

        var saved = await schemaLoaded.SaveAsync(cancellationToken);
        if (!saved)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to save schema with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: {schemaLoaded.Name} saved.\r\n");
        return (int)ERROR_CODES.SUCCESS;
    }
}