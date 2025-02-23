using System.Text;
using Figment;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class PrintSchemaCommand : CancellableAsyncCommand<SchemaCommandSettings>
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
        if (Program.SelectedEntity.Equals(Reference.EMPTY) || Program.SelectedEntity.Type != Reference.ReferenceType.Schema)
        {
            if (string.IsNullOrWhiteSpace(settings.SchemaName))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To view properties on a schema, you must first 'select' a schema.");
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
                    Program.SelectedEntity = possibilities[0];
                    break;
                default:
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one entity matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (Program.SelectedEntity.Type != Reference.ReferenceType.Schema)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Markup.Escape(Enum.GetName(Program.SelectedEntity.Type) ?? string.Empty)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        var schemaLoaded = await Schema.LoadAsync(Program.SelectedEntity.Guid, cancellationToken);
        if (schemaLoaded == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema with Guid '{Markup.Escape(Program.SelectedEntity.Guid)}'.");
            return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        var propBuilder = new StringBuilder();
        if (schemaLoaded.Properties != null && schemaLoaded.Properties.Count > 0)
        {
            var maxPropNameLen = schemaLoaded.Properties.Max(p => p.Key.Length); // In case it will be escaped
            foreach (var prop in schemaLoaded.Properties)
            {
                propBuilder.AppendLine($"   {prop.Key.PadRight(maxPropNameLen)} : {await prop.Value.GetReadableFieldTypeAsync(cancellationToken)}{(prop.Value.Required ? " (REQUIRED)" : string.Empty)}");
            }
        }

        Console.WriteLine(
            $"""
            Instance    : {schemaLoaded.Name}
            GUID        : {schemaLoaded.Guid}
            Type        : {nameof(Schema)}

            Description : {schemaLoaded.Description}
            Plural      : {schemaLoaded.Plural}

            Properties  : {(propBuilder.Length == 0 ? "(None)" : string.Empty)}
            {propBuilder}
            """);
        return (int)ERROR_CODES.SUCCESS;
    }
}