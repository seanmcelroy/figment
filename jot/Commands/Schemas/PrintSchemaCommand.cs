using System.Text;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// Renders all fields on a schema to output.
/// </summary>
public class PrintSchemaCommand : SchemaCancellableAsyncCommand<SchemaCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SchemaCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        var propBuilder = new StringBuilder();
        if (schema!.Properties != null && schema.Properties.Count > 0)
        {
            var maxPropNameLen = schema.Properties.Max(p => p.Key.Length); // In case it will be escaped
            foreach (var prop in schema.Properties)
            {
                propBuilder.AppendLine($"   {prop.Key.PadRight(maxPropNameLen)} : {Markup.Escape(await prop.Value.GetReadableFieldTypeAsync(cancellationToken))}{(prop.Value.Required ? " (REQUIRED)" : string.Empty)}");
            }
        }

        AnsiConsole.MarkupLine($"[silver]Schema[/]      : [bold white]{schema.Name}[/]");
        if (settings.Verbose ?? Program.Verbose)
        {
            AnsiConsole.MarkupLine($"[silver]GUID[/]        : {schema.Guid}");
        }

        AnsiConsole.MarkupLine($"Description : {schema.Description}");
        AnsiConsole.MarkupLine($"Plural      : {schema.Plural}");
        if (!string.IsNullOrWhiteSpace(schema.VersionGuid))
        {
            var provider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
            if (provider == null)
            {
                AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
            }

            var version = await provider.LoadAsync(schema.VersionGuid, cancellationToken);
            if (version == null)
            {
                AmbientErrorContext.Provider.LogError($"Unable to load version '{schema.VersionGuid}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR;
            }

            AnsiConsole.MarkupLine($"Version     : {version.Name}");
        }

        if (settings.Verbose ?? Program.Verbose)
        {
            AnsiConsole.MarkupLine($"[silver]Created On[/]  : {schema.CreatedOn.ToLocalTime().ToLongDateString()} at {schema.CreatedOn.ToLocalTime().ToLongTimeString()}");
            AnsiConsole.MarkupLine($"[silver]Modified On[/] : {schema.LastModified.ToLocalTime().ToLongDateString()} at {schema.LastModified.ToLocalTime().ToLongTimeString()}");
        }

        AnsiConsole.MarkupLine(
            $"""

            [chartreuse4]Properties[/]  : {(propBuilder.Length == 0 ? "(None)" : string.Empty)}
            {propBuilder}
            """);
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}