using System.Diagnostics;
using System.Text;
using Figment.Common;
using Figment.Common.Data;
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
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one entity matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (selected.Type != Reference.ReferenceType.Thing)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        var thingProvider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thingLoaded = await thingProvider.LoadAsync(selected.Guid, cancellationToken);
        if (thingLoaded == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.THING_LOAD_ERROR;
        }

        var schemaProvider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();

        Dictionary<string, Schema> schemas = [];
        if (thingLoaded.SchemaGuids != null)
        {
            if (schemaProvider == null)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema storage provider.");
                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
            }

            foreach (var schemaGuid in thingLoaded.SchemaGuids)
            {
                var schema = await schemaProvider.LoadAsync(schemaGuid, cancellationToken);
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
                var text = await GetMarkedUpFieldValue(schprop, prop.Value.fieldValue, cancellationToken);
                if (prop.Value.valid)
                    propBuilder.AppendLine($"   {prop.Key.PadRight(maxPropNameLen)} : {text}");
                else
                    propBuilder.AppendLine($"   {prop.Key.PadRight(maxPropNameLen)} : [red bold]{text}[/]");
            }
            else
                propBuilder.AppendLine($"   {prop.Key.PadRight(maxPropNameLen)} : {Markup.Escape(prop.Value.fieldValue?.ToString() ?? string.Empty)}");
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
                    unsetPropBuilder.AppendLine($"    {prop.SimpleDisplayName.PadRight(maxPropNameLen)} : [silver]{Markup.Escape(await prop.Field.GetReadableFieldTypeAsync(cancellationToken))}{(prop.Field.Required ? " (REQUIRED)" : string.Empty)}[/]");
            }
        }

        var linksBuilder = new StringBuilder();
        if (schemas.Count > 0)
        {
            linksBuilder.AppendLine("[red]Links[/]");
            foreach (var schema in schemas)
            {
                var linkedFields = schema.Value.Properties
                    .Where(p => string.CompareOrdinal(p.Value.Type, SchemaRefField.TYPE) == 0)
                    .ToDictionary(k => k.Key, v => (SchemaRefField)v.Value);

                foreach (var lf in linkedFields)
                {
                    var linkedSchema = await schemaProvider.LoadAsync(lf.Value.SchemaGuid, cancellationToken);
                    var linkedPlural = linkedSchema.Plural;
                    linksBuilder.AppendLine($"    {lf.Key} ({linkedPlural})");
                }
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
            {linksBuilder}
            """);
        return (int)ERROR_CODES.SUCCESS;
    }

    private static async Task<string?> GetMarkedUpFieldValue<T>(T field, object? value, CancellationToken cancellationToken) where T : SchemaFieldBase
    {
        ArgumentNullException.ThrowIfNull(field);

        var fieldType = field.GetType();
        if (fieldType.Equals(typeof(SchemaArrayField)))
        {
            if (value == null)
                return default;

            if (value is not System.Collections.IEnumerable ie)
                return default;

            var contents = ie.Cast<object?>()
                .Select(x => x?.ToString() ?? string.Empty)
                .Aggregate((c, n) => $"{c},{n}");

            return Markup.Escape($"[{contents}]"); // This needs to be escaped
        }
        else if (fieldType.Equals(typeof(SchemaPhoneField)))
        {
            if (value == null)
                return default;

            var str = value as string;

            if (Debugger.IsAttached
                || !AnsiConsole.Profile.Capabilities.Links
                || str?.IndexOfAny(['[', ']']) > -1)
                return str; // No link wrapping.

            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            return (string?)$"[link=tel:{Markup.Escape(str)}]{Markup.Escape(str)}[/]";
        }
        else if (fieldType.Equals(typeof(SchemaRefField)))
        {
            if (value == null)
                return default;

            if (value is not string str)
                return default;

            var thingGuid = str[(str.IndexOf('.') + 1)..];

            var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
            if (tsp == null)
                return str;

            var thing = await tsp.LoadAsync(thingGuid, cancellationToken);
            if (thing == null)
                return str;

            return Markup.Escape(thing.Name);
        }
        else
        {
            var val = value?.ToString();
            if (string.IsNullOrEmpty(val))
                return val;

            return Markup.Escape(val);
        }

    }
}