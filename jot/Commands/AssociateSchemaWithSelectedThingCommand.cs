using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class AssociateSchemaWithSelectedThingCommand : CancellableAsyncCommand<AssociateSchemaWithSelectedThingCommandSettings>
{
    private enum ERROR_CODES : int
    {
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
        SCHEMA_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, AssociateSchemaWithSelectedThingCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.ThingName))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To associate a schema to a thing, you must first 'select' a thing.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Thing.ResolveAsync(settings.ThingName, cancellationToken)
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
                    AmbientErrorContext.Provider.LogError("Ambiguous match; more than one thing matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (selected.Type != Reference.ReferenceType.Thing)
        {
            AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        // Schema first
        if (string.IsNullOrWhiteSpace(settings.SchemaName))
        {
            AnsiConsole.MarkupLine("[yellow]ERROR[/]: Schema name must be specified.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        var schemaPossibilities = Schema.ResolveAsync(settings.SchemaName, cancellationToken)
            .ToBlockingEnumerable(cancellationToken)
            .ToArray();

        Schema? schema;
        switch (schemaPossibilities.Length)
        {
            case 0:
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: No schema found named '{settings.SchemaName}'");
                return (int)ERROR_CODES.NOT_FOUND;
            case 1:
                {
                    var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
                    if (provider == null)
                    {
                        AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema storage provider.");
                        return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
                    }

                    schema = await provider.LoadAsync(schemaPossibilities[0].Guid, cancellationToken);
                    if (schema == null)
                    {
                        AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema '{settings.SchemaName}'.");
                        return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
                    }
                    break;
                }
            default:
                AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one schema matches this name.");
                return (int)ERROR_CODES.AMBIGUOUS_MATCH;
        }

        switch (selected.Type)
        {
            case Reference.ReferenceType.Thing:
                {
                    var cmd = new AssociateSchemaWithThingCommand();
                    return await cmd.ExecuteAsync(context, new AssociateSchemaWithThingCommandSettings { SchemaName = settings.SchemaName, ThingName = selected.Guid }, cancellationToken);
                }
            default:
                AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
                return (int)ERROR_CODES.UNKNOWN_TYPE;
        }
    }
}