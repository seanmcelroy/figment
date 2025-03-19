using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using jot.Commands.Schemas;
using Spectre.Console.Cli;

namespace jot.Commands.Interactive;

/// <summary>
/// Associates the currently selected <see cref="Thing"/> with the specified <see cref="Schema"/>.
/// </summary>
public class AssociateSchemaWithSelectedThingCommand : CancellableAsyncCommand<AssociateSchemaWithSelectedThingCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, AssociateSchemaWithSelectedThingCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To associate a schema to a thing, you must first 'select' a thing.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        if (selected.Type != Reference.ReferenceType.Thing)
        {
            AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }

        // Schema first
        if (string.IsNullOrWhiteSpace(settings.SchemaName))
        {
            AmbientErrorContext.Provider.LogError("Schema name must be specified.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var schemaPossibilities = Schema.ResolveAsync(settings.SchemaName, cancellationToken)
            .ToBlockingEnumerable(cancellationToken)
            .ToArray();

        Schema? schema;
        switch (schemaPossibilities.Length)
        {
            case 0:
                AmbientErrorContext.Provider.LogError($"No schema found named '{settings.SchemaName}'");
                return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
            case 1:
                {
                    var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
                    if (provider == null)
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to load schema storage provider.");
                        return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
                    }

                    schema = await provider.LoadAsync(schemaPossibilities[0].Guid, cancellationToken);
                    if (schema == null)
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to load schema '{settings.SchemaName}'.");
                        return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR;
                    }

                    break;
                }

            default:
                AmbientErrorContext.Provider.LogError("Ambiguous match; more than one schema matches this name.");
                return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
        }

        switch (selected.Type)
        {
            case Reference.ReferenceType.Thing:
                {
                    var cmd = new AssociateSchemaWithThingCommand();
                    return await cmd.ExecuteAsync(context, new AssociateSchemaWithThingCommandSettings { SchemaName = settings.SchemaName, ThingName = selected.Guid, Verbose = settings.Verbose }, cancellationToken);
                }

            default:
                AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }
    }
}