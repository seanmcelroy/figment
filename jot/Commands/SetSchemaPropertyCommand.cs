using Figment;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSchemaPropertyCommand : CancellableAsyncCommand<SetSchemaPropertyCommandSettings>
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

    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        // set work phone=+1 (212) 555-5555
        // auto-selects text
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY) || selected.Type != Reference.ReferenceType.Schema)
        {
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To view properties on a thing, you must first 'select' a thing.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Reference.ResolveAsync(settings.Name, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .Where(x => x.Type == Reference.ReferenceType.Schema)
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

        var propName = settings.PropertyName;
        if (string.IsNullOrWhiteSpace(propName))
        {
            AnsiConsole.MarkupLine("[yellow]ERROR[/]: To change a property on a schema, specify the property's name.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        if (selected.Type != Reference.ReferenceType.Schema)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        var schemaLoaded = await Schema.LoadAsync(selected.Guid, cancellationToken);
        if (schemaLoaded == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        var propType = settings.Value;

        // Handle built-ins
        if (string.Compare("name", propName, StringComparison.CurrentCultureIgnoreCase) == 0)
        {
            // Built-in
            if (string.IsNullOrWhiteSpace(propType))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: The name of a schema cannot be empty.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            schemaLoaded.Name = propType;
            await schemaLoaded.SaveAsync(cancellationToken);
            await Schema.RebuildIndexes(cancellationToken);
        }
        else if (string.Compare("plural", propName, StringComparison.CurrentCultureIgnoreCase) == 0)
        {
            // Built-in
            schemaLoaded.Plural = propType;
            await schemaLoaded.SaveAsync(cancellationToken);
            await Schema.RebuildIndexes(cancellationToken);
        }
        else if (string.Compare("description", propName, StringComparison.CurrentCultureIgnoreCase) == 0)
        {
            // Built-in
            schemaLoaded.Description = propType;
            await schemaLoaded.SaveAsync(cancellationToken);
            await Schema.RebuildIndexes(cancellationToken);
        }
        else if (string.IsNullOrWhiteSpace(propType))
        {
            // Deletes a property.
            var propToDelete = schemaLoaded.Properties.FirstOrDefault(p => string.Compare(p.Key, propName, StringComparison.OrdinalIgnoreCase) == 0);
            if (propToDelete.Equals(default(KeyValuePair<string, SchemaFieldBase>)))
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: No property named '{propName}' found on schema '{schemaLoaded.Name}'");
                return (int)ERROR_CODES.NOT_FOUND;
            }

            schemaLoaded.Properties.Remove(propToDelete.Key);
            AnsiConsole.MarkupLineInterpolated($"[yellow]WARN[/]: Deleted property name '{propName}'.");
        }
        else if (string.CompareOrdinal(propType, "bool") == 0)
        {
            var sbf = new SchemaBooleanField(propName);
            schemaLoaded.Properties[propName] = sbf;
        }
        else if (string.CompareOrdinal(propType, "date") == 0)
        {
            var sdf = new SchemaDateField(propName);
            schemaLoaded.Properties[propName] = sdf;
        }
        else if (string.CompareOrdinal(propType, "email") == 0)
        {
            var sef = new SchemaEmailField(propName);
            schemaLoaded.Properties[propName] = sef;
        }
        else if (
            string.CompareOrdinal(propType, "integer") == 0
            || string.CompareOrdinal(propType, "int") == 0
        )
        {
            var sif = new SchemaIntegerField(propName);
            schemaLoaded.Properties[propName] = sif;
        }
        else if (
            string.CompareOrdinal(propType, "number") == 0
            || string.CompareOrdinal(propType, "num") == 0
            || string.CompareOrdinal(propType, "double") == 0
            || string.CompareOrdinal(propType, "float") == 0
        )
        {
            var snf = new SchemaNumberField(propName);
            schemaLoaded.Properties[propName] = snf;
        }
        else if (string.CompareOrdinal(propType, "phone") == 0)
        {
            var spf = new SchemaPhoneField(propName);
            schemaLoaded.Properties[propName] = spf;
        }
        else if (string.CompareOrdinal(propType, "text") == 0)
        {
            var stf = new SchemaTextField(propName);
            schemaLoaded.Properties[propName] = stf;
        }
        else if (string.CompareOrdinal(propType, "uri") == 0)
        {
            var suf = new SchemaUriField(propName);
            schemaLoaded.Properties[propName] = suf;
        }
        else if (propType != null && propType.StartsWith('[') && propType.EndsWith(']') && propType.Length >= 5 && propType.Contains(','))
        {
            var enumValues = propType[1..^1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var sef = new SchemaEnumField(propName, enumValues);
            schemaLoaded.Properties[propName] = sef;
        }
        else
        {
            // Maybe this is the name of a schema.
            var refSchema = await Schema.FindAsync(propType!, cancellationToken);
            if (refSchema == null)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: I do not understand that type of field ({propType}).");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var srf = new SchemaRefField(propName, refSchema.Guid);
            schemaLoaded.Properties[propName] = srf;
        }

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