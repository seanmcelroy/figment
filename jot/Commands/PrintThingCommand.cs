using System.Text;
using Figment;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class PrintThingCommand : CancellableAsyncCommand<ThingCommandSettings>, ICommand
{
    private enum ERROR_CODES : int
    {
        SUCCESS = Globals.GLOBAL_ERROR_CODES.SUCCESS,
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
        THING_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ThingCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To view properties on a thing, you must first 'select' a thing.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Reference.ResolveAsync(settings.Name, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .Where(x => x.Type == Reference.ReferenceType.Thing)
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

        if (selected.Type != Reference.ReferenceType.Thing)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        var thingLoaded = await Thing.LoadAsync(selected.Guid, cancellationToken);
        if (thingLoaded == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.THING_LOAD_ERROR;
        }

        Dictionary<string, Schema> schemas = [];
        if (thingLoaded.SchemaGuids != null)
        {
            foreach (var schemaGuid in thingLoaded.SchemaGuids)
            {
                var schema = await Schema.LoadAsync(schemaGuid, cancellationToken);
                if (schema != null)
                    schemas.Add(schema.Guid, schema);
                else
                    AnsiConsole.MarkupLineInterpolated($"[yellow]WARN[/]: Unable to load associated schema with Guid '{schemaGuid}'.");
            }
        }

        var propDict = new Dictionary<string, (string? schemaGuid, object? fieldValue, bool valid)>();

        var maxPropNameLen = 0;
        await foreach (var property in thingLoaded.GetProperties(cancellationToken))
        {
            maxPropNameLen = Math.Max(maxPropNameLen, property.FullDisplayName.Length);
            propDict.Add(property.FullDisplayName, (property.SchemaGuid, property.Value, property.Valid));
        }

        var schemaBuilder = new StringBuilder();
        foreach (var schema in schemas)
        {
            schemaBuilder.AppendLine($"[silver]Schema[/]     : {schema.Value.Name} [silver]({schema.Value.Guid})[/]");
        }
        if (schemaBuilder.Length == 0)
            schemaBuilder.AppendLine();

        var propBuilder = new StringBuilder();
        foreach (var prop in propDict)
        {
            // Skip built-ins
            if (string.CompareOrdinal(prop.Key, nameof(Thing.Name)) == 0
                || string.CompareOrdinal(prop.Key, nameof(Thing.Guid)) == 0
                || string.CompareOrdinal(prop.Key, nameof(Thing.SchemaGuids)) == 0
                )
                continue;

            // Coerce value if schema-bound using a field renderer.
            if (prop.Value.schemaGuid != null
                && schemas.TryGetValue(prop.Value.schemaGuid, out Schema? sch)
                && sch.Properties.TryGetValue(prop.Key[(prop.Key.IndexOf('.') + 1)..], out SchemaFieldBase? schprop))
            {
                var text = await schprop.GetMarkedUpFieldValue(prop.Value.fieldValue, cancellationToken);
                if (prop.Value.valid)
                    propBuilder.AppendLine($"   {prop.Key.PadRight(maxPropNameLen)} : {text}");
                else
                    propBuilder.AppendLine($"   {prop.Key.PadRight(maxPropNameLen)} : [red bold]{Markup.Escape(text ?? string.Empty)}[/]");
            }
            else
                propBuilder.AppendLine($"   {prop.Key.PadRight(maxPropNameLen)} : {prop.Value.fieldValue}");
        }

        var unsetPropBuilder = new StringBuilder();
        var anyUnset = await thingLoaded.GetUnsetProperties(cancellationToken);
        if (anyUnset.Count > 0)
        {
            unsetPropBuilder.AppendLine("[red]Unset Properties[/]");
            maxPropNameLen = 0;
            foreach (var grp in anyUnset.GroupBy(p => (p.SchemaGuid, p.SchemaName)))
            {
                maxPropNameLen = grp.Max(g => g.SimpleDisplayName.Length);
                unsetPropBuilder.AppendLine($"  [silver]For schema[/] [bold white]{grp.Key.SchemaName}[/] [silver]({grp.Key.SchemaGuid})[/]");
                foreach (var prop in grp)
                    unsetPropBuilder.AppendLine($"    {prop.SimpleDisplayName.PadRight(maxPropNameLen)} : [silver]{await prop.Field.GetReadableFieldTypeAsync(cancellationToken)}{(prop.Field.Required ? " (REQUIRED)" : string.Empty)}[/]");
            }
        }

        AnsiConsole.MarkupLine(
            $"""
            [silver]Instance[/]   : [bold white]{thingLoaded.Name}[/]
            [silver]GUID[/]       : '{thingLoaded.Guid}'
            {schemaBuilder}
            [chartreuse4]Properties[/] : {(propBuilder.Length == 0 ? "(None)" : string.Empty)}
            {propBuilder}
            {unsetPropBuilder}
            """);
        return (int)ERROR_CODES.SUCCESS;
    }
}