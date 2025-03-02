using Figment.Common;
using Figment.Common.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class NewCommand : CancellableAsyncCommand<NewCommandSettings>
{
    private enum ERROR_CODES : int
    {
        SCHEMA_CREATE_ERROR = -1002,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, NewCommandSettings settings, CancellationToken cancellationToken)
    {
        // Make thing.  Syntax 'new type [name]'
        if (string.IsNullOrWhiteSpace(settings.SchemaName))
        {
            AnsiConsole.MarkupLine("[yellow]ERROR[/]: To create a new thing, specify the type of thing and its name, like: new todo \"Call Jake\"");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var ssp = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (ssp == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        Schema? schema;

        if (string.IsNullOrWhiteSpace(settings.ThingName))
        {
            // Create schema only.
            // new todo
            var schemaRef = await ssp.FindByNameAsync(settings.SchemaName, cancellationToken);
            if (schemaRef == Reference.EMPTY)
            {
                schema = await Schema.Create(settings.SchemaName, cancellationToken);
                if (schema == null)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to create schema '{settings.SchemaName}'.");
                    return (int)ERROR_CODES.SCHEMA_CREATE_ERROR;
                }
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: A schema with that name already exists!");
                return (int)ERROR_CODES.SCHEMA_CREATE_ERROR;
            }

            AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: Schema '{settings.SchemaName}' created. ({schema.Guid})\r\n");
            return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
        }

        // new todo Call Jake
        bool createdNewSchema = false;
        {
            var schemaRef = await ssp.FindByNameAsync(settings.SchemaName, cancellationToken);
            if (schemaRef == Reference.EMPTY)
            {
                if (settings.AutoCreateSchema ?? true) // This is the key difference from the part above.  Auto-gen only matters for schema+name
                {
                    schema = await Schema.Create(settings.SchemaName, cancellationToken);
                    if (schema == null)
                    {
                        AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to create schema '{settings.SchemaName}'.");
                        return (int)ERROR_CODES.SCHEMA_CREATE_ERROR;
                    }
                    else
                        createdNewSchema = true;
                }
                else
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to create schema '{settings.SchemaName}' because automatic generation was not enabled.");
                    return (int)ERROR_CODES.SCHEMA_CREATE_ERROR;
                }
            }
            else {
                schema = await ssp.LoadAsync(schemaRef.Guid, cancellationToken);
                if (schema == null)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema '{settings.SchemaName}'.");
                    return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR;
                }
            }
        }

        //var thingName = inputSplit[2..].Aggregate((c, n) => $"{c} {n}");

        var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (tsp == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thingName = settings.ThingName;
        var thing = await tsp.CreateAsync(schema.Guid, thingName, cancellationToken);
        Program.SelectedEntity = thing;

        if (createdNewSchema)
            AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: Schema {schema.Name} created, and new instance {thingName} created.\r\n");
        else
            AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: {thingName}, a type of {schema.Name}, created.\r\n");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}