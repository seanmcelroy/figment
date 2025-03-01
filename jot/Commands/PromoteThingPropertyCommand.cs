using Figment.Common;
using Figment.Common.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class PromoteThingPropertyCommand : CancellableAsyncCommand<PromoteThingPropertyCommandSettings>
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

    public override async Task<int> ExecuteAsync(CommandContext context, PromoteThingPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        // promote propertyname, like 

        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To promote a property on a thing, you must first 'select' a thing.");
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

        var thingProvider = StorageUtility.StorageProvider.GetThingStorageProvider();
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

        if (thingLoaded.SchemaGuids == null
            || thingLoaded.SchemaGuids.Count == 0)
        {
            AnsiConsole.MarkupInterpolated($"[red]ERROR[/]: Unable to load any schema from {thingLoaded.Name}.  Must be able to load an associated schema to promote a property to it.");
            return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        var property = thingLoaded.Properties.FirstOrDefault(p => string.CompareOrdinal(p.Key, settings.PropertyName) == 0);
        if (property.Equals(default(KeyValuePair<string, object>)))
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: No property named '{settings.PropertyName}' on thing.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        if (thingLoaded.SchemaGuids.Count > 0)
        {
            var provider = StorageUtility.StorageProvider.GetSchemaStorageProvider();
            if (provider == null)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema storage provider.");
                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
            }

            foreach (var schemaGuid in thingLoaded.SchemaGuids)
            {
                var schemaLoaded = string.IsNullOrWhiteSpace(schemaGuid)
                    ? null
                    : await provider.LoadAsync(schemaGuid, cancellationToken);

                if (schemaLoaded == null)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema '{schemaGuid}' from {thingLoaded.Name}.  Must be able to load schema to promote a property to it.");
                    return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
                }

                // TODO: Right now we promote the field to EVERY associated schema.
                // Should there be a schema chooser?

                // Put the field on the schema.
                var schemaProperty = schemaLoaded.AddTextField(property.Key);
                // Update my version of the file to point to the schema version
                thingLoaded.Properties.Remove(property.Key);
                var truePropertyName = $"{schemaLoaded.Guid}.{schemaProperty.Name}";
                thingLoaded.Properties.Add($"{schemaLoaded.Guid}.{schemaProperty.Name}", property.Value);
                var schemaSaved = await schemaLoaded.SaveAsync(cancellationToken);
                if (!schemaSaved)
                    return (int)ERROR_CODES.SCHEMA_SAVE_ERROR;
            }
        }

        // Type?  Just assume text field for now.  Deal with anything different as a schema field type change.

        var thingSaved = await thingLoaded.SaveAsync(cancellationToken);
        if (!thingSaved)
            return (int)ERROR_CODES.THING_SAVE_ERROR;

        AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: '{property.Key}' is now promoted from a one-off property on {thingLoaded.Name} to a property on associated schema(s).\r\n");
        return (int)ERROR_CODES.SUCCESS;

        // Is there a conflicting name?
    }
}