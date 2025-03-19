using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

/// <summary>
/// Promotes a property on one <see cref="Thing"/> to become a property defined on a <see cref="Schema"/>.
/// </summary>
public class PromoteThingPropertyCommand : CancellableAsyncCommand<PromoteThingPropertyCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, PromoteThingPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        // promote propertyname, like
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.ThingName))
            {
                AmbientErrorContext.Provider.LogError("To promote a property on a thing, you must first 'select' a thing.");
                return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Thing.ResolveAsync(settings.ThingName, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibilities.Length)
            {
                case 0:
                    AmbientErrorContext.Provider.LogError("Nothing found with that name");
                    return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibilities[0];
                    break;
                default:
                    AmbientErrorContext.Provider.LogError("Ambiguous match; more than one entity matches this name.");
                    return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (string.IsNullOrWhiteSpace(settings.PropertyName))
        {
            AmbientErrorContext.Provider.LogError("To promote a property on a thing, you must first specify the property name.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var thingProvider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thingLoaded = await thingProvider.LoadAsync(selected.Guid, cancellationToken);
        if (thingLoaded == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing with Guid '{selected.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR;
        }

        if (thingLoaded.SchemaGuids == null
            || thingLoaded.SchemaGuids.Count == 0)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load any schema from {thingLoaded.Name}.  Must be able to load an associated schema to promote a property to it.");
            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        var property = thingLoaded.GetPropertyByName(settings.PropertyName, cancellationToken)
            .ToBlockingEnumerable(cancellationToken)
            .FirstOrDefault();
        if (property.Equals(default(KeyValuePair<string, object>)))
        {
            AmbientErrorContext.Provider.LogError($"No property named '{settings.PropertyName}' on thing.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        if (thingLoaded.SchemaGuids.Count > 0)
        {
            var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
            if (provider == null)
            {
                AmbientErrorContext.Provider.LogError($"Unable to load schema storage provider.");
                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
            }

            foreach (var schemaGuid in thingLoaded.SchemaGuids)
            {
                var schema = string.IsNullOrWhiteSpace(schemaGuid)
                    ? null
                    : await provider.LoadAsync(schemaGuid, cancellationToken);

                if (schema == null)
                {
                    AmbientErrorContext.Provider.LogError($"Unable to load schema '{schemaGuid}' from {thingLoaded.Name}.  Must be able to load schema to promote a property to it.");
                    return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR;
                }

                // TODO: Right now we promote the field to EVERY associated schema.
                // Should there be a schema chooser?

                // Put the field on the schema.
                var schemaProperty = schema.AddTextField(property.TruePropertyName);

                // Update my version of the file to point to the schema version
                thingLoaded.TryRemoveProperty(property.TruePropertyName);

                // var truePropertyName = $"{schemaLoaded.Guid}.{schemaProperty.Name}";
                if (property.Value != null)
                {
                    thingLoaded.TryAddProperty($"{schema.Guid}.{schemaProperty.Name}", property.Value);
                }

                var schemaSaved = await schema.SaveAsync(cancellationToken);
                if (!schemaSaved)
                {
                    if (settings.Verbose ?? false)
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}' ({schema.Guid}).");
                    }
                    else
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}'.");
                    }

                    return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR;
                }
            }
        }

        // But what field type?  Just assume text field for now.  Deal with anything different as a schema field type change.
        var thingSaved = await thingLoaded.SaveAsync(cancellationToken);
        if (!thingSaved)
        {
            return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
        }

        AmbientErrorContext.Provider.LogDone($"'{property.FullDisplayName}' is now promoted from a one-off property on {thingLoaded.Name} to a property on associated schema(s).");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;

        // Is there a conflicting name?
    }
}