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
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thing = await thingProvider.LoadAsync(thingReference.Guid, cancellationToken);
        if (thing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing with Guid '{thingReference.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR;
        }

        await thing.ComputeCalculatedProperties(cancellationToken);

        var schemaProvider = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (schemaProvider == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        Dictionary<string, Schema> schemas = [];
        if (thing.SchemaGuids != null)
        {
            var schemaTasks = thing.SchemaGuids.Select(guid => schemaProvider.LoadAsync(guid, cancellationToken));
            var schemaResults = await Task.WhenAll(schemaTasks);

            for (int i = 0; i < schemaResults.Length; i++)
            {
                var schema = schemaResults[i];
                if (schema != null)
                {
                    schemas.Add(schema.Guid, schema);
                }
                else
                {
                    AmbientErrorContext.Provider.LogWarning($"Unable to load associated schema with Guid '{thing.SchemaGuids[i]}'.");
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
            var escapedGuid = Markup.Escape(thing.Guid);
            var createdLocalTime = thing.CreatedOn.ToLocalTime();
            var modifiedLocalTime = thing.LastModified.ToLocalTime();

            masterTable.AddRow("[indianred1]GUID[/]", $"[gray]{escapedGuid}[/]");
            masterTable.AddRow("[indianred1]Created On[/]", $"[gray]{createdLocalTime:D} at {createdLocalTime:T}[/]");
            masterTable.AddRow("[indianred1]Modified On[/]", $"[gray]{modifiedLocalTime:D} at {modifiedLocalTime:T}[/]");
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
                if (!string.IsNullOrWhiteSpace(property.FullDisplayName))
                {
                    propDict.Add(property.FullDisplayName, (property.SchemaGuid, property.Value, property.Valid));
                }
            }

            // Pre-load all referenced things to avoid N+1 query problem
            var referencedThingGuids = new HashSet<string>();
            foreach (var prop in propDict)
            {
                if (prop.Value.schemaGuid != null
                    && schemas.TryGetValue(prop.Value.schemaGuid, out Schema? sch))
                {
                    var dotIndex = prop.Key.IndexOf('.');
                    var propertyKey = dotIndex >= 0 ? prop.Key[(dotIndex + 1)..] : prop.Key;

                    if (sch.Properties.TryGetValue(propertyKey, out SchemaFieldBase? schprop)
                        && schprop is SchemaRefField
                        && prop.Value.fieldValue is string refValue)
                    {
                        var refDotIndex = refValue.IndexOf('.');
                        var thingGuid = refDotIndex >= 0 ? refValue[(refDotIndex + 1)..] : refValue;
                        referencedThingGuids.Add(thingGuid);
                    }
                }
            }

            var referencedThings = new Dictionary<string, Thing>();
            if (referencedThingGuids.Count > 0)
            {
                var thingTasks = referencedThingGuids.Select(guid => thingProvider.LoadAsync(guid, cancellationToken));
                var thingResults = await Task.WhenAll(thingTasks);

                for (int i = 0; i < thingResults.Length; i++)
                {
                    var loadedThing = thingResults[i];
                    if (loadedThing != null)
                    {
                        referencedThings.Add(loadedThing.Guid, loadedThing);
                    }
                }
            }

            var currentCultureName = CultureInfo.CurrentCulture.Name;
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
                    && schemas.TryGetValue(prop.Value.schemaGuid, out Schema? sch))
                {
                    var dotIndex = prop.Key.IndexOf('.');
                    var propertyKey = dotIndex >= 0 ? prop.Key[(dotIndex + 1)..] : prop.Key;

                    // This is the intended behavior, if prop.Key has no dot, we return the whole string.
                    if (sch.Properties.TryGetValue(propertyKey, out SchemaFieldBase? schprop))
                    {
                        if (!(settings.NoPrettyDisplayNames ?? false)
                            && schprop.DisplayNames != null
                            && schprop.DisplayNames.TryGetValue(currentCultureName, out string? prettyDisplayName))
                        {
                            propDisplayName = prettyDisplayName;
                        }

                        var text = await GetMarkedUpFieldValue(schprop, prop.Value.fieldValue, referencedThings, cancellationToken);
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
                                new Markup($"[aqua]{grp.Key.SchemaName ?? string.Empty}[/]"),
                                new Markup($"[red]{prop.SimpleDisplayName}[/]"),
                                new Markup($"[red]{Markup.Escape(await prop.Field.GetReadableFieldTypeAsync(cancellationToken))}[/]"),
                                new Markup(prop.Field.Required ? Emoji.Known.CheckMarkButton : Emoji.Known.CrossMark));
                        }
                    }

                    masterTable.AddRow(new Markup("[indianred1]Unset Properties[/]"), unsetPropertyTable);
                }
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
    /// <param name="referencedThings">Pre-loaded referenced things to avoid database calls.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The marked-up <paramref name="value"/> that can be rendered to a <see cref="IAnsiConsole"/> for a rich display or interaction experience.</returns>
    internal static async Task<string?> GetMarkedUpFieldValue<T>(T field, object? value, Dictionary<string, Thing>? referencedThings, CancellationToken cancellationToken)
        where T : SchemaFieldBase
    {
        ArgumentNullException.ThrowIfNull(field);

        return field switch
        {
            SchemaArrayField => HandleArrayField(value),
            SchemaDateField => HandleDateField(value),
            SchemaMonthDayField => HandleMonthDayField(value),
            SchemaPhoneField => HandlePhoneField(value),
            SchemaRefField => await HandleRefField(value, referencedThings, cancellationToken),
            _ => HandleDefaultField(value)
        };
    }

    private static string? HandleArrayField(object? value)
    {
        if (value == null)
        {
            return default;
        }

        if (value is not System.Collections.IEnumerable ie)
        {
            return default;
        }

        var contentsArray = ie.Cast<object?>() // This is the expected behavior, it should always be castable to a nullable System.Object.
            .Select(x => x?.ToString() ?? string.Empty)
            .ToArray();

        // Use string.Join for efficient concatenation
        if (contentsArray.Length == 0)
        {
            return Markup.Escape("[]"); // This needs to be escaped
        }

        var contents = string.Join(",", contentsArray);
        return Markup.Escape($"[{contents}]"); // This needs to be escaped
    }

    private static string? HandleDateField(object? value)
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

    private static string? HandleMonthDayField(object? value)
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
            var month = md / 100;
            var day = md - (month * 100);

            if (md < 100 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(2000, month))
            {
                return (string?)$"[yellow]{Markup.Escape(value.ToString() ?? string.Empty)}[/]";
            }

            var d = new DateTime(new DateOnly(2000, month, day), TimeOnly.FromTimeSpan(TimeSpan.Zero));
            return $"{d:MMMM dd}";
        }

        return (string?)$"[yellow]{Markup.Escape(value.ToString() ?? string.Empty)}[/]";
    }

    private static string? HandlePhoneField(object? value)
    {
        if (value == null)
        {
            return default;
        }

        var str = value as string;

        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        if (Debugger.IsAttached
            || !AnsiConsole.Profile.Capabilities.Links
            || str.IndexOfAny(['[', ']']) > -1)
        {
            return str; // No link wrapping.
        }

        return (string?)$"[link=tel:{Markup.Escape(str)}]{Markup.Escape(str)}[/]";
    }

    private static async Task<string?> HandleRefField(object? value, Dictionary<string, Thing>? referencedThings, CancellationToken cancellationToken)
    {
        if (value == null)
        {
            return default;
        }

        if (value is not string str)
        {
            return default;
        }

        var dotIndex = str.IndexOf('.');
        var thingGuid = dotIndex >= 0 ? str[(dotIndex + 1)..] : str; // This is the intended behavior - if there is no dot, then we return the entire string.

        // Use pre-loaded things cache to avoid database call
        if (referencedThings != null && referencedThings.TryGetValue(thingGuid, out Thing? cachedThing))
        {
            return Markup.Escape(cachedThing.Name);
        }

        // Fallback to database call if not in cache
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

    private static string? HandleDefaultField(object? value)
    {
        var val = value?.ToString();
        if (string.IsNullOrEmpty(val))
        {
            return val;
        }

        return Markup.Escape(val);
    }
}