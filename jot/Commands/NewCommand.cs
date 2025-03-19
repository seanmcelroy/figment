using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// Creates new types (<see cref="Schema"/>s) or instances of <see cref="Thing"/>s.
/// </summary>
public class NewCommand : CancellableAsyncCommand<NewCommandSettings>
{
    private enum ERROR_CODES : int
    {
        SCHEMA_CREATE_ERROR = -1002,
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, NewCommandSettings settings, CancellationToken cancellationToken)
    {
        // Make thing.  Syntax 'new type [name]'
        if (string.IsNullOrWhiteSpace(settings.SchemaName))
        {
            AmbientErrorContext.Provider.LogError("To create a new thing, specify the type of thing and its name, like: new todo \"Call Jake\"");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var ssp = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (ssp == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load schema storage provider.");
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
                    AmbientErrorContext.Provider.LogError($"Unable to create schema '{settings.SchemaName}'.");
                    return (int)ERROR_CODES.SCHEMA_CREATE_ERROR;
                }
            }
            else
            {
                AmbientErrorContext.Provider.LogError($"A schema with that name already exists!");
                return (int)ERROR_CODES.SCHEMA_CREATE_ERROR;
            }

            AmbientErrorContext.Provider.LogDone($"Schema '{settings.SchemaName}' created. ({schema.Guid})");
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
                        AmbientErrorContext.Provider.LogError($"Unable to create schema '{settings.SchemaName}'.");
                        return (int)ERROR_CODES.SCHEMA_CREATE_ERROR;
                    }
                    else
                    {
                        createdNewSchema = true;
                    }
                }
                else
                {
                    AmbientErrorContext.Provider.LogError($"Unable to create schema '{settings.SchemaName}' because automatic generation was not enabled.");
                    return (int)ERROR_CODES.SCHEMA_CREATE_ERROR;
                }
            }
            else
            {
                schema = await ssp.LoadAsync(schemaRef.Guid, cancellationToken);
                if (schema == null)
                {
                    AmbientErrorContext.Provider.LogError($"Unable to load schema '{settings.SchemaName}'.");
                    return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR;
                }
            }
        }

        var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thingName = settings.ThingName;
        var thing = await tsp.CreateAsync(schema.Guid, thingName, cancellationToken);
        Program.SelectedEntity = thing;
        Program.SelectedEntityName = thing?.Name ?? thing?.Guid ?? string.Empty;

        if (createdNewSchema)
        {
            AmbientErrorContext.Provider.LogDone($"Schema {schema.Name} created, and new instance {thingName} created.");
        }
        else
        {
            AmbientErrorContext.Provider.LogDone($"{thingName}, a type of {schema.Name}, created.");
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}