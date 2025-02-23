using Figment;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ValidateThingCommand : CancellableAsyncCommand<ThingCommandSettings>
{
    private enum ERROR_CODES : int
    {
        SUCCESS = Globals.GLOBAL_ERROR_CODES.SUCCESS,
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
        SCHEMA_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR,
        THING_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ThingCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To validate a thing, you must first 'select' a thing.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Thing.ResolveAsync(settings.Name, cancellationToken)
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
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one thing matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (selected.Type != Reference.ReferenceType.Thing)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Markup.Escape(Enum.GetName(selected.Type) ?? string.Empty)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        var thing = await Thing.LoadAsync(selected.Guid, cancellationToken);
        if (thing == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing with Guid '{Markup.Escape(selected.Guid)}'.");
            return (int)ERROR_CODES.THING_LOAD_ERROR;
        }

        await Console.Out.WriteLineAsync($"Validating {thing.Name} ({thing.Guid}) ...");

        List<ThingProperty> thingProperties = [];
        await foreach (var property in thing.GetProperties(cancellationToken))
        {
            thingProperties.Add(property);
            if (!property.Valid)
            {
                AnsiConsole.MarkupLineInterpolated($"[yellow]WARN[/]: Property {Markup.Escape(property.SimpleDisplayName)} ({Markup.Escape(property.TruePropertyName)}) has an invalid value of '{Markup.Escape(property.Value?.ToString() ?? string.Empty)}'.");
            }
        }

        if (thing.SchemaGuids == null
            || thing.SchemaGuids.Count == 0)
        {
            AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: Validation has finished.\r\n");
            return (int)ERROR_CODES.SUCCESS;
        }

        foreach (var schemaGuid in thing.SchemaGuids)
        {
            var schemaLoaded = string.IsNullOrWhiteSpace(schemaGuid)
                ? null
                : await Schema.LoadAsync(schemaGuid, cancellationToken);

            if (schemaLoaded == null)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema '{Markup.Escape(schemaGuid)}' from {Markup.Escape(thing.Name)}.  Must be able to load schema to promote a property to it.");
                return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
            }

            foreach (var sp in schemaLoaded.Properties
                .Where(sp => sp.Value.Required
                    && !thingProperties.Any(
                        tp => tp.SchemaGuid == schemaLoaded.Guid
                        && string.CompareOrdinal(tp.SimpleDisplayName, sp.Key) == 0)))
            {
                AnsiConsole.MarkupLineInterpolated($"[yellow]WARN[/]: Schema property {Markup.Escape(sp.Key)} is required but is not set!");
            }
        }

        AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: Validation has finished.\r\n");
        return (int)ERROR_CODES.SUCCESS;
    }
}