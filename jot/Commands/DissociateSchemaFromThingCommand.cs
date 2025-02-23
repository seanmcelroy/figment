using Figment;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class DissociateSchemaFromThingCommand : CancellableAsyncCommand<AssociateSchemaWithThingCommandSettings>
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
        THING_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR,
        THING_SAVE_ERROR = Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, AssociateSchemaWithThingCommandSettings settings, CancellationToken cancellationToken)
    {
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
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: No schema found named '{Markup.Escape(settings.SchemaName)}'");
                return (int)ERROR_CODES.NOT_FOUND;
            case 1:
                schema = await Schema.LoadAsync(schemaPossibilities[0].Guid, cancellationToken);
                if (schema == null)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema '{Markup.Escape(settings.SchemaName)}'.");
                    return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
                }
                break;
            default:
                AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one schema matches this name.");
                return (int)ERROR_CODES.AMBIGUOUS_MATCH;
        }

        // Now on to the thing.
        if (string.IsNullOrWhiteSpace(settings.ThingName))
        {
            AnsiConsole.MarkupLine("[yellow]ERROR[/]: Thing name must be specified.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        var thingPossibilities = Thing.ResolveAsync(settings.ThingName, cancellationToken)
            .ToBlockingEnumerable(cancellationToken)
            .ToArray();

        Thing? thing;
        switch (thingPossibilities.Length)
        {
            case 0:
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: No thing found named '{Markup.Escape(settings.ThingName)}'");
                return (int)ERROR_CODES.NOT_FOUND;
            case 1:
                thing = await Thing.LoadAsync(thingPossibilities[0].Guid, cancellationToken);
                if (thing == null)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema '{Markup.Escape(settings.ThingName)}'.");
                    return (int)ERROR_CODES.THING_LOAD_ERROR;
                }
                break;
            default:
                AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one thing matches this name.");
                return (int)ERROR_CODES.AMBIGUOUS_MATCH;
        }

        if (!thing.SchemaGuids.Remove(schema.Guid))
        {
            AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: {Markup.Escape(thing.Name)} is not associated with schema {Markup.Escape(schema.Name)}. No change.\r\n");
            return (int)ERROR_CODES.SUCCESS;
        }

        var thingSaved = await thing.SaveAsync(cancellationToken);
        if (!thingSaved)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to edit thing with Guid '{Markup.Escape(thing.Guid)}'.");
            return (int)ERROR_CODES.THING_SAVE_ERROR;
        }

        if (thing.SchemaGuids.Count == 0)
            AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: {Markup.Escape(thing.Name)} is no longer associated to any schemas.\r\n");
        else
            AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: {Markup.Escape(thing.Name)} is no longer a '{Markup.Escape(schema.Name)}'.\r\n");

        return (int)ERROR_CODES.SUCCESS;
    }
}