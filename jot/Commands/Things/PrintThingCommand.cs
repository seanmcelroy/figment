/*
Figment
Copyright (C) 2025  Sean McElroy

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Diagnostics;
using System.Globalization;
using System.Text;
using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace jot.Commands.Things;

/// <summary>
/// Renders the values of all properties on a <see cref="Thing"/>.
/// </summary>
public class PrintThingCommand : CancellableAsyncCommand<PrintThingCommandSettings>, ICommand
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, PrintThingCommandSettings settings, CancellationToken cancellationToken)
    {
        var verbose = settings.Verbose ?? Program.Verbose;

        Reference thingReference;
        var thingResolution = settings.ResolveThingName(cancellationToken);
        switch (thingResolution.Item1)
        {
            case Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR:
                AmbientErrorContext.Provider.LogError("To view properties on a thing, you must first 'select' a thing.");
                return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
            case Globals.GLOBAL_ERROR_CODES.NOT_FOUND:
                AmbientErrorContext.Provider.LogError($"No thing found named '{settings.ThingName}'");
                return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
            case Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH:
                AmbientErrorContext.Provider.LogError("Ambiguous match; more than one thing matches this name.");
                return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
            case Globals.GLOBAL_ERROR_CODES.SUCCESS:
                thingReference = thingResolution.thing;
                break;
            default:
                throw new NotImplementedException($"Unexpected return code {Enum.GetName(thingResolution.Item1)}");
        }

        var thingProvider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thing = await thingProvider.LoadAsync(thingReference.Guid, cancellationToken);
        if (thing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing with Guid '{thingReference.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR;
        }

        await thing.ComputeCalculatedProperties(cancellationToken);

        var schemaProvider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (schemaProvider == null)
        {
            AmbientErrorContext.Provider.LogError("Unable to load schema storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        Dictionary<string, Schema> schemas = [];
        if (thing.SchemaGuids != null)
        {
            if (schemaProvider == null)
            {
                AmbientErrorContext.Provider.LogError("Unable to load schema storage provider.");
                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
            }

            foreach (var schemaGuid in thing.SchemaGuids)
            {
                var schema = await schemaProvider.LoadAsync(schemaGuid, cancellationToken);
                if (schema != null)
                {
                    schemas.Add(schema.Guid, schema);
                }
                else
                {
                    AmbientErrorContext.Provider.LogWarning($"Unable to load associated schema with Guid '{schemaGuid}'.");
                }
            }
        }

        var linksBuilder = new StringBuilder();
        if (schemas.Count > 0)
        {
            linksBuilder.AppendLine("[red]Links[/]");
            foreach (var schema in schemas)
            {
                var linkedFields = schema.Value.Properties
                    .Where(p => p.Value.Type.Equals(SchemaRefField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal))
                    .ToDictionary(k => k.Key, v => (SchemaRefField)v.Value);

                foreach (var lf in linkedFields)
                {
                    var linkedSchema = await schemaProvider.LoadAsync(lf.Value.SchemaGuid, cancellationToken);
                    if (linkedSchema == null)
                    {
                        AmbientErrorContext.Provider.LogWarning($"Unable to load linked schema {lf.Value.SchemaGuid}.");
                    }
                    else
                    {
                        var linkedPlural = linkedSchema.Plural;
                        linksBuilder.AppendLine($"    {lf.Key} ({linkedPlural})");
                    }
                }
            }
        }

        var masterTable = new Table()
            .AddColumn(
                new TableColumn(new Text("Name", new Style(decoration: Decoration.Conceal))).Padding(0, 0, 2, 2))
            .AddColumn(
                new TableColumn(new Text("Value", new Style(decoration: Decoration.Conceal))).Padding(0, 0, 2, 2))
            .HideHeaders()
            .NoBorder();

        masterTable.AddRow("[indianred1]Instance[/]", $"[bold orange1]{Markup.Escape(thing.Name)}[/]");

        List<IRenderable> schemaRowRenderables = [];
        foreach (var schema in schemas)
        {
            if (verbose)
            {
                schemaRowRenderables.Add(new Markup($"[aqua]{Markup.Escape(schema.Value.Name)}[/] [gray]({Markup.Escape(schema.Value.Guid)})[/]"));
            }
            else
            {
                schemaRowRenderables.Add(new Markup($"[aqua]{Markup.Escape(schema.Value.Name)}[/]"));
            }
        }

        masterTable.AddRow(new Markup("[indianred1]Schemas[/]"), new Rows(schemaRowRenderables));

        if (verbose)
        {
            masterTable.AddRow("[indianred1]GUID[/]", $"[gray]{Markup.Escape(thing.Guid)}[/]");
            masterTable.AddRow("[indianred1]Created On[/]", $"[gray]{thing.CreatedOn.ToLocalTime().ToLongDateString()} at {thing.CreatedOn.ToLocalTime().ToLongTimeString()}[/]");
            masterTable.AddRow("[indianred1]Modified On[/]", $"[gray]{thing.LastModified.ToLocalTime().ToLongDateString()} at {thing.LastModified.ToLocalTime().ToLongTimeString()}[/]");
        }

        // Properties
        {
            var propertyTable = new Table();

            if (verbose)
            {
                propertyTable.AddColumn(
                    new TableColumn(new Text("Schema", new Style(decoration: Decoration.Bold | Decoration.Underline))));
            }

            propertyTable
                .AddColumn(
                    new TableColumn(new Text("Name", new Style(decoration: Decoration.Bold | Decoration.Underline))))
                .AddColumn(
                    new TableColumn(new Text("Value", new Style(decoration: Decoration.Bold | Decoration.Underline))))
                .ShowRowSeparators()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Orange1);

            var propDict = new Dictionary<string, (string? schemaGuid, object? fieldValue, bool valid)>();

            await foreach (var property in thing.GetProperties(cancellationToken))
            {
                propDict.Add(property.FullDisplayName, (property.SchemaGuid, property.Value, property.Valid));
            }

            foreach (var prop in propDict)
            {
                // Skip built-ins
                if (string.Equals(prop.Key, nameof(Thing.Name), StringComparison.Ordinal)
                    || string.Equals(prop.Key, nameof(Thing.Guid), StringComparison.Ordinal)
                    || string.Equals(prop.Key, nameof(Thing.SchemaGuids), StringComparison.Ordinal))
                {
                    continue;
                }

                // Coerce value if schema-bound using a field renderer.
                var propDisplayName = prop.Key;
                if (prop.Value.schemaGuid != null
                    && schemas.TryGetValue(prop.Value.schemaGuid, out Schema? sch)
                    && sch.Properties.TryGetValue(prop.Key[(prop.Key.IndexOf('.') + 1)..], out SchemaFieldBase? schprop))
                {
                    if (!(settings.NoPrettyDisplayNames ?? false)
                        && schprop.DisplayNames != null
                        && schprop.DisplayNames.TryGetValue(CultureInfo.CurrentCulture.Name, out string? prettyDisplayName))
                    {
                        propDisplayName = prettyDisplayName;
                    }

                    var text = await GetMarkedUpFieldValue(schprop, prop.Value.fieldValue, cancellationToken);
                    if (prop.Value.valid)
                    {
                        if (verbose)
                        {
                            propertyTable.AddRow(sch.Name, propDisplayName, text ?? string.Empty);
                        }
                        else
                        {
                            propertyTable.AddRow(propDisplayName, text ?? string.Empty);
                        }
                    }
                    else
                    {
                        if (verbose)
                        {
                            propertyTable.AddRow(new Markup(sch.Name), new Markup(propDisplayName), new Markup($"[red bold]{text}[/]"));
                        }
                        else
                        {
                            propertyTable.AddRow(new Markup(propDisplayName), new Markup($"[red bold]{text}[/]"));
                        }
                    }
                }
                else
                {
                    if (verbose)
                    {
                        propertyTable.AddRow("???", prop.Key, prop.Value.fieldValue?.ToString() ?? string.Empty);
                    }
                    else
                    {
                        propertyTable.AddRow(prop.Key, prop.Value.fieldValue?.ToString() ?? string.Empty);
                    }
                }
            }

            masterTable.AddRow(new Markup("[indianred1]Properties[/]"), propertyTable);

            if (verbose)
            {
                var unsetPropertyTable = new Table()
                    .AddColumn(
                        new TableColumn(new Text("Schema", new Style(decoration: Decoration.Bold | Decoration.Underline))))
                    .AddColumn(
                        new TableColumn(new Text("Property Name", new Style(decoration: Decoration.Bold | Decoration.Underline))))
                    .AddColumn(
                        new TableColumn(new Text("Data Type", new Style(decoration: Decoration.Bold | Decoration.Underline))))
                    .AddColumn(
                        new TableColumn(new Text("Required?", new Style(decoration: Decoration.Bold | Decoration.Underline))))
                    .ShowRowSeparators()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.IndianRed1);

                var anyUnset = await thing.GetUnsetProperties(cancellationToken);
                if (anyUnset.Count > 0)
                {
                    foreach (var grp in anyUnset.GroupBy(p => (p.SchemaGuid, p.SchemaName)))
                    {
                        foreach (var prop in grp)
                        {
                            unsetPropertyTable.AddRow(
                                new Markup($"[aqua]{grp.Key.SchemaName}[/]"),
                                new Markup($"[red]{prop.SimpleDisplayName}[/]"),
                                new Markup($"[red]{Markup.Escape(await prop.Field.GetReadableFieldTypeAsync(cancellationToken))}[/]"),
                                new Markup(prop.Field.Required ? Emoji.Known.CheckMarkButton : Emoji.Known.CrossMark));
                        }
                    }
                }

                masterTable.AddRow(new Markup("[indianred1]Unset Properties[/]"), unsetPropertyTable);
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(masterTable);
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }

    /// <summary>
    /// Gets a field value marked up for rich consoles using <see cref="IAnsiConsole"/>.
    /// </summary>
    /// <typeparam name="T">The type of the schema field to mark up for rendering.</typeparam>
    /// <param name="field">The schema field to mark up.</param>
    /// <param name="value">The value in the schema field to mark up.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The marked-up <paramref name="value"/> that can be rendered to a <see cref="IAnsiConsole"/> for a rich display or interaction experience.</returns>
    internal static async Task<string?> GetMarkedUpFieldValue<T>(T field, object? value, CancellationToken cancellationToken)
        where T : SchemaFieldBase
    {
        ArgumentNullException.ThrowIfNull(field);

        var fieldType = field.GetType();
        if (fieldType.Equals(typeof(SchemaArrayField)))
        {
            if (value == null)
            {
                return default;
            }

            if (value is not System.Collections.IEnumerable ie)
            {
                return default;
            }

            var contents = ie.Cast<object?>()
                .Select(x => x?.ToString() ?? string.Empty)
                .Aggregate((c, n) => $"{c},{n}");

            return Markup.Escape($"[{contents}]"); // This needs to be escaped
        }

        if (fieldType.Equals(typeof(SchemaDateField)))
        {
            if (value == null)
            {
                return default;
            }

            if (value is DateTimeOffset dto)
            {
                if (dto.TimeOfDay == TimeSpan.Zero)
                {
                    return dto.Date.ToShortDateString();
                }

                return $"{dto:s}";
            }

            if (value is DateTime dt)
            {
                if (dt.TimeOfDay == TimeSpan.Zero)
                {
                    return dt.Date.ToShortDateString();
                }

                return $"{dt:s}";
            }

            if (SchemaDateField.TryParseDate(value.ToString(), out DateTimeOffset dto2))
            {
                if (dto2.TimeOfDay == TimeSpan.Zero)
                {
                    return dto2.Date.ToShortDateString();
                }

                return $"{dto2:s}";
            }

            return (string?)$"[yellow]{Markup.Escape(value.ToString() ?? string.Empty)}[/]";
        }

        if (fieldType.Equals(typeof(SchemaMonthDayField)))
        {
            if (value == null)
            {
                return default;
            }

            if (value is DateTimeOffset dto)
            {
                if (dto.TimeOfDay == TimeSpan.Zero)
                {
                    return dto.Date.ToShortDateString();
                }

                return $"{dto:MMMM dd}";
            }

            if (value is DateTime dt)
            {
                if (dt.TimeOfDay == TimeSpan.Zero)
                {
                    return dt.Date.ToShortDateString();
                }

                return $"{dt:MMMM dd}";
            }

            if (SchemaMonthDayField.TryParseMonthDay(value.ToString(), out int md))
            {
                var d = new DateTime(new DateOnly(2000, md / 100, md - ((md / 100) * 100)), TimeOnly.FromTimeSpan(TimeSpan.Zero));
                return $"{d:MMMM dd}";
            }

            return (string?)$"[yellow]{Markup.Escape(value.ToString() ?? string.Empty)}[/]";
        }

        if (fieldType.Equals(typeof(SchemaPhoneField)))
        {
            if (value == null)
            {
                return default;
            }

            var str = value as string;

            if (Debugger.IsAttached
                || !AnsiConsole.Profile.Capabilities.Links
                || str?.IndexOfAny(['[', ']']) > -1)
            {
                return str; // No link wrapping.
            }

            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            return (string?)$"[link=tel:{Markup.Escape(str)}]{Markup.Escape(str)}[/]";
        }

        if (fieldType.Equals(typeof(SchemaRefField)))
        {
            if (value == null)
            {
                return default;
            }

            if (value is not string str)
            {
                return default;
            }

            var thingGuid = str[(str.IndexOf('.') + 1)..];

            var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
            if (tsp == null)
            {
                return str;
            }

            var thing = await tsp.LoadAsync(thingGuid, cancellationToken);
            if (thing == null)
            {
                return str;
            }

            return Markup.Escape(thing.Name);
        }

        var val = value?.ToString();
        if (string.IsNullOrEmpty(val))
        {
            return val;
        }

        return Markup.Escape(val);
    }
}