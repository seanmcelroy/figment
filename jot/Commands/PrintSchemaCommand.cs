using System.Text;
using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
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
                    AmbientErrorContext.Provider.LogError("Nothing found with that name");
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
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(Program.SelectedEntity.Type)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var schema = await provider.LoadAsync(Program.SelectedEntity.Guid, cancellationToken);
        if (schema == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema with Guid '{Program.SelectedEntity.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        Program.SelectedEntityName = schema.Name;

        var propBuilder = new StringBuilder();
        if (schema.Properties != null && schema.Properties.Count > 0)
        {
            var maxPropNameLen = schema.Properties.Max(p => p.Key.Length); // In case it will be escaped
            foreach (var prop in schema.Properties)
            {
                propBuilder.AppendLine($"   {prop.Key.PadRight(maxPropNameLen)} : {Markup.Escape(await prop.Value.GetReadableFieldTypeAsync(settings.Verbose ?? Program.Verbose, cancellationToken))}{(prop.Value.Required ? " (REQUIRED)" : string.Empty)}");
            }
        }

        AnsiConsole.MarkupLine($"[silver]Schema[/]      : [bold white]{schema.Name}[/]");
        if (settings.Verbose ?? Program.Verbose)
            AnsiConsole.MarkupLine($"[silver]GUID[/]        : {schema.Guid}");
        AnsiConsole.MarkupLine($"Description : {schema.Description}");
        AnsiConsole.MarkupLine($"Plural      : {schema.Plural}");

        if (settings.Verbose ?? Program.Verbose)
        {
            AnsiConsole.MarkupLine($"[silver]Created On[/]  : {schema.CreatedOn.ToLocalTime().ToLongDateString()} at {schema.CreatedOn.ToLocalTime().ToLongTimeString()}");
            AnsiConsole.MarkupLine($"[silver]Modified On[/] : {schema.LastModified.ToLocalTime().ToLongDateString()} at {schema.LastModified.ToLocalTime().ToLongTimeString()}");
        }

        AnsiConsole.MarkupLine(
            $"""

            [chartreuse4]Properties[/]  : {(propBuilder.Length == 0 ? "(None)" : string.Empty)}
            {propBuilder}
            """);
        return (int)ERROR_CODES.SUCCESS;
    }
}