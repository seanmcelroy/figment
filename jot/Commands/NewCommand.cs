using System.Runtime.CompilerServices;
using Figment;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class NewCommand : CancellableAsyncCommand<NewCommandSettings>
{
    private enum ERROR_CODES : int
    {
        SUCCESS = Globals.GLOBAL_ERROR_CODES.SUCCESS,
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        SCHEMA_CREATE_ERROR = -1002,
        THING_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, NewCommandSettings settings, CancellationToken cancellationToken)
    {
        // Make thing.  Syntax 'new type [name]'
        if (string.IsNullOrWhiteSpace(settings.SchemaName))
        {
            AnsiConsole.MarkupLine("[yellow]ERROR[/]: To create a new thing, specify the type of thing and its name, like: new todo \"Call Jake\"");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        Schema? schema;

        if (string.IsNullOrWhiteSpace(settings.ThingName))
        {
            // Create schema only.
            // new todo

            schema = await Schema.FindAsync(settings.SchemaName, cancellationToken);
            if (schema == null && (settings.AutoCreateSchema ?? true))
                schema = await Schema.Create(settings.SchemaName, cancellationToken);
            if (schema == null)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to create schema '{settings.SchemaName}'.");
                return (int)ERROR_CODES.SCHEMA_CREATE_ERROR;
            }

            AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: Schema {schema.Name} created.\r\n");
            return (int)ERROR_CODES.SUCCESS;
        }

        // new todo Call Jake
        bool createdNewSchema = false;
        schema = await Schema.FindAsync(settings.SchemaName, cancellationToken);
        if (schema == null && (settings.AutoCreateSchema ?? true))
        {
            schema = await Schema.Create(settings.SchemaName, cancellationToken);
            createdNewSchema = true;
        }
        if (schema == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to create schema '{settings.SchemaName}'.");
            return (int)ERROR_CODES.SCHEMA_CREATE_ERROR;
        }

        //var thingName = inputSplit[2..].Aggregate((c, n) => $"{c} {n}");
        var thingName = settings.ThingName;
        var thing = await Thing.Create(schema.Guid, thingName, cancellationToken);
        Program.SelectedEntity = thing;

        if (createdNewSchema)
            AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: Schema {schema.Name} created, and new instance {thingName} created.\r\n");
        else
            AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: {thingName}, a type of {schema.Name}, created.\r\n");
        return (int)ERROR_CODES.SUCCESS;
    }
}