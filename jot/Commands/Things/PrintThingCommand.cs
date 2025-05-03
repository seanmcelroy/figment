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

namespace jot.Commands.Things;

/// <summary>
/// Renders the values of all properties on a <see cref="Thing"/>.
/// </summary>
public class PrintThingCommand : CancellableAsyncCommand<PrintThingCommandSettings>, ICommand
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, PrintThingCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.ThingName))
            {
                AmbientErrorContext.Provider.LogError("To view properties on a thing, you must first 'select' a thing.");
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

        if (selected.Type != Reference.ReferenceType.Thing)
        {
            AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }

        var thingProvider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thing = await thingProvider.LoadAsync(selected.Guid, cancellationToken);
        if (thing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing with Guid '{selected.Guid}'.");
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

        var propDict = new Dictionary<string, (string? schemaGuid, object? fieldValue, bool valid)>();

        var maxPropNameLen = 0;
        await foreach (var property in thing.GetProperties(cancellationToken))
        {
            maxPropNameLen = Math.Max(maxPropNameLen, property.FullDisplayName.Length);
            propDict.Add(property.FullDisplayName, (property.SchemaGuid, property.Value, property.Valid));
        }

        var schemaBuilder = new StringBuilder();
        foreach (var schema in schemas)
        {
            if (settings.Verbose ?? Program.Verbose)
            {
                schemaBuilder.AppendLine($"[silver]Schema[/]      : {schema.Value.Name} [silver]({schema.Value.Guid})[/]");
            }
            else
            {
                schemaBuilder.AppendLine($"[silver]Schema[/]      : {schema.Value.Name}");
            }
        }

        if (schemaBuilder.Length == 0)
        {
            schemaBuilder.AppendLine();
        }

        var propBuilder = new StringBuilder();
        foreach (var prop in propDict)
        {
            // Skip built-ins
            if (string.CompareOrdinal(prop.Key, nameof(Thing.Name)) == 0
                || string.CompareOrdinal(prop.Key, nameof(Thing.Guid)) == 0
                || string.CompareOrdinal(prop.Key, nameof(Thing.SchemaGuids)) == 0
                )
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
                    propBuilder.AppendLine($"   {Markup.Escape(propDisplayName.PadRight(maxPropNameLen))} : {text}");
                }
                else
                {
                    propBuilder.AppendLine($"   {Markup.Escape(propDisplayName.PadRight(maxPropNameLen))} : [red bold]{text}[/]");
                }
            }
            else
            {
                propBuilder.AppendLine($"   {Markup.Escape(prop.Key.PadRight(maxPropNameLen))} : {Markup.Escape(prop.Value.fieldValue?.ToString() ?? string.Empty)}");
            }
        }

        var unsetPropBuilder = new StringBuilder();
        if (settings.Verbose ?? Program.Verbose)
        {
            var anyUnset = await thing.GetUnsetProperties(cancellationToken);
            if (anyUnset.Count > 0)
            {
                unsetPropBuilder.AppendLine("[red]Unset Properties[/]");
                maxPropNameLen = 0;
                foreach (var grp in anyUnset.GroupBy(p => (p.SchemaGuid, p.SchemaName)))
                {
                    maxPropNameLen = grp.Max(g => g.SimpleDisplayName.Length);
                    if (settings.Verbose ?? Program.Verbose)
                    {
                        unsetPropBuilder.AppendLine($"  [silver]For schema[/] [bold white]{grp.Key.SchemaName}[/] [silver]({grp.Key.SchemaGuid})[/]");
                    }
                    else
                    {
                        unsetPropBuilder.AppendLine($"  [silver]For schema[/] [bold white]{grp.Key.SchemaName}[/] [silver][/]");
                    }

                    foreach (var prop in grp)
                    {
                        unsetPropBuilder.AppendLine($"    {prop.SimpleDisplayName.PadRight(maxPropNameLen)} : [silver]{Markup.Escape(await prop.Field.GetReadableFieldTypeAsync(cancellationToken))}{(prop.Field.Required ? " (REQUIRED)" : string.Empty)}[/]");
                    }
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
                    .Where(p => string.CompareOrdinal(p.Value.Type, SchemaRefField.SCHEMA_FIELD_TYPE) == 0)
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

        AnsiConsole.MarkupLine($"[silver]Instance[/]    : [bold white]{thing.Name}[/]");
        if (settings.Verbose ?? Program.Verbose)
        {
            AnsiConsole.MarkupLine($"[silver]GUID[/]        : {thing.Guid}");
        }

        if (settings.Verbose ?? Program.Verbose)
        {
            AnsiConsole.MarkupLine($"[silver]Created On[/]  : {thing.CreatedOn.ToLocalTime().ToLongDateString()} at {thing.CreatedOn.ToLocalTime().ToLongTimeString()}");
            AnsiConsole.MarkupLine($"[silver]Modified On[/] : {thing.LastModified.ToLocalTime().ToLongDateString()} at {thing.LastModified.ToLocalTime().ToLongTimeString()}");
        }

        static string ConditionalPrint(StringBuilder? sb)
        {
            if (sb == null || sb.Length == 0)
            {
                return string.Empty;
            }

            return $"{sb}{Environment.NewLine}";
        }

        AnsiConsole.MarkupLine(
            $"""
            {ConditionalPrint(schemaBuilder)}[chartreuse4]Properties[/]  : {(propBuilder.Length == 0 ? "(None)" : string.Empty)}
            {propBuilder}{ConditionalPrint(unsetPropBuilder)}
            """);
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

            var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
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