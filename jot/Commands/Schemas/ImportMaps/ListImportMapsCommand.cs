using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas.ImportMaps;

/// <summary>
/// Lists all import maps defined on a <see cref="Figment.Common.Schema"/>.
/// </summary>
public class ListImportMapsCommand : SchemaCancellableAsyncCommand<ListImportMapsCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ListImportMapsCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        if (settings.AsTable ?? false)
        {
            Table table = new();
            table.AddColumn("Name");
            table.AddColumn("Type");
            table.AddColumn("Field Count");

            foreach (var map in schema!.ImportMaps)
            {
                table.AddRow(map.Name, map.Format, map.FieldConfiguration.Count.ToString());
            }

            AnsiConsole.Write(table);
        }
        else
        {
            foreach (var map in schema!.ImportMaps)
            {
                Console.WriteLine(map.Name);
            }
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}