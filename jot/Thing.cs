using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using jot;
using Spectre.Console;

namespace Figment;

public class Thing(string Guid, string Name)
{
    private const string NameIndexFileName = $"_thing.names.csv";

    public string Guid { get; set; } = Guid;
    public string Name { get; set; } = Name;
    public string? SchemaGuid { get; set; }
    public Dictionary<string, object> Properties { get; init; } = [];

    private static async Task<DirectoryInfo?> GetThingDatabaseDirectory()
    {
        var thingPath = Path.Combine(Globals.DB_PATH, "things");

        if (Directory.Exists(thingPath))
            return new DirectoryInfo(thingPath);

        await Console.Error.WriteLineAsync("WARN: Thing database directory does not exist.");
        try
        {
            return Directory.CreateDirectory(thingPath);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"ERR: Cannot create thing database directory at '{thingPath}': {ex.Message}");
            return null;
        }
    }

    public static string ConvertThingNameToFileName(string thingName)
    {
        var cultureInfo = new CultureInfo("en-US", false);
        var textInfo = cultureInfo.TextInfo;
        var title = textInfo.ToTitleCase(thingName);
        var combined = title.Replace(" ", "");
        var camelCase = combined.ToLower(cultureInfo)[0] + title[1..];
        return camelCase;
    }

    public static async Task<Thing?> Create(string? schemaGuid, string thingName, CancellationToken cancellationToken)
    {
        var thingGuid = System.Guid.NewGuid().ToString();
        var thing = new Thing(thingGuid, thingName)
        {
            SchemaGuid = schemaGuid
        };

        var thingDir = await GetThingDatabaseDirectory();
        if (thingDir == null)
            return null;

        var thingFileName = $"{thingGuid}.thing.json";
        var thingFilePath = Path.Combine(thingDir.FullName, thingFileName);

        if (File.Exists(thingFilePath))
        {
            await Console.Error.WriteLineAsync($"ERR: File for thing {thingName} already exists at {thingFilePath}");
            return null;
        }

        using var fs = new FileStream(thingFilePath, FileMode.CreateNew);
        try
        {
            await JsonSerializer.SerializeAsync(fs, thing, cancellationToken: cancellationToken);
            await fs.FlushAsync(cancellationToken);
        }
        catch (Exception)
        {
            File.Delete(thingFilePath);
            throw;
        }

        // Add to name index
        {
            var indexFilePath = Path.Combine(thingDir.FullName, NameIndexFileName);
            await IndexManager.AddAsync(indexFilePath, thingName, thingFileName, cancellationToken);
        }

        // If this has a schema, add it to the schema index
        if (!string.IsNullOrWhiteSpace(schemaGuid))
        {
            var indexFilePath = Path.Combine(thingDir.FullName, $"_thing.schema.{schemaGuid}.csv");
            await IndexManager.AddAsync(indexFilePath, thing.Guid, thingFileName, cancellationToken);
        }

        return await LoadAsync(thingGuid, cancellationToken);
    }

    public async Task<bool> Delete(CancellationToken cancellationToken)
    {
        var thingDir = await GetThingDatabaseDirectory();
        if (thingDir == null)
            return false;

        var thingFileName = $"{Guid}.thing.json";
        var thingFilePath = Path.Combine(thingDir.FullName, thingFileName);

        if (!File.Exists(thingFilePath))
        {
            AnsiConsole.MarkupLineInterpolated($"[yellow]WARNING[/]: File for thing {Name} ({Guid}) does not exist at {thingFilePath}. Nothing to do.");
            return false;
        }

        try
        {
            File.Delete(thingFilePath);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return false;
        }

        // Remove from name index
        {
            var indexFilePath = Path.Combine(thingDir.FullName, NameIndexFileName);
            await IndexManager.RemoveByValueAsync(indexFilePath, thingFileName, cancellationToken);
            AnsiConsole.MarkupLineInterpolated($"[blue]Working...[/] Deleted from name index {Path.GetFileName(indexFilePath)}");
        }

        // If this has a schema, remove it from the schema index
        if (!string.IsNullOrWhiteSpace(SchemaGuid))
        {
            var indexFilePath = Path.Combine(thingDir.FullName, $"_thing.schema.{SchemaGuid}.csv");
            await IndexManager.RemoveByKeyAsync(indexFilePath, Guid, cancellationToken);
            AnsiConsole.MarkupLineInterpolated($"[blue]Working...[/] Deleted from schema index {Path.GetFileName(indexFilePath)}");
        }

        return true;
    }

    public static async IAsyncEnumerable<Thing> GetAll([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var thingDir = await GetThingDatabaseDirectory();
        if (thingDir == null)
            yield break;

        foreach (var file in thingDir.GetFiles("*.thing.json"))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var thingGuidString = file.Name.Split(".thing.json");
            if (!string.IsNullOrWhiteSpace(thingGuidString[0])
                && System.Guid.TryParse(thingGuidString[0], out Guid _))
            {
                var thing = await LoadAsync(thingGuidString[0], cancellationToken);
                if (thing != null)
                    yield return thing;
            }
        }
    }

    public static async IAsyncEnumerable<Thing> GetBySchema(string schemaGuid, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var thingDir = await GetThingDatabaseDirectory();
        if (thingDir == null)
            yield break;

        var indexFilePath = Path.Combine(thingDir.FullName, $"_thing.schema.{schemaGuid}.csv");

        await foreach (var entry in IndexManager.LookupAsync(indexFilePath, e => true, cancellationToken))
        {
            var thing = await LoadFileAsync(Path.Combine(thingDir.FullName, entry.Value), cancellationToken);
            if (thing != null)
                yield return thing;
        }
        if (!File.Exists(indexFilePath))
            yield break;
    }

    public static async Task<Thing?> LoadAsync(string thingGuid, CancellationToken cancellationToken)
    {
        var thingDir = await GetThingDatabaseDirectory();
        if (thingDir == null)
            return null;

        var fileName = $"{thingGuid}.thing.json";
        var filePath = Path.Combine(thingDir.FullName, fileName);

        if (!File.Exists(filePath))
        {
            await Console.Error.WriteLineAsync($"ERR: No file for thing {thingGuid} found at {filePath}");
            return null;
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            await Console.Error.WriteLineAsync($"ERR: Empty thing file for {thingGuid} found at {filePath}");
            fileInfo.Delete();
            return null;
        }

        var thingLoaded = new Thing(thingGuid, "");
        using var fs = new FileStream(filePath, FileMode.Open);
        try
        {
            using var doc = await JsonDocument.ParseAsync(fs, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            if (root.TryGetProperty(nameof(Name), out JsonElement nameProperty))
            {
                thingLoaded.Name = nameProperty.GetString() ?? "<UNDEFINED>";
            }

            if (root.TryGetProperty(nameof(SchemaGuid), out JsonElement schemaGuidProperty))
            {
                thingLoaded.SchemaGuid = schemaGuidProperty.GetString();
            }

            foreach (var prop in root.EnumerateObject())
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;

                if (
                  string.CompareOrdinal(prop.Name, nameof(Name)) == 0
                  || string.CompareOrdinal(prop.Name, nameof(Guid)) == 0
                  || string.CompareOrdinal(prop.Name, nameof(SchemaGuid)) == 0
                )
                {
                    // Ignore built-ins, as they're defined on root, not Properties
                    continue;
                }

                switch (prop.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        var s = prop.Value.GetString();
                        if (s != null) // We don't load nulls
                            thingLoaded.Properties.TryAdd(prop.Name, s);
                        continue;
                    case JsonValueKind.True:
                        thingLoaded.Properties.TryAdd(prop.Name, true);
                        continue;
                    case JsonValueKind.False:
                        thingLoaded.Properties.TryAdd(prop.Name, false);
                        continue;
                    case JsonValueKind.Null:
                        continue; // We don't load nulls
                    case JsonValueKind.Object:
                        {
                            if (string.CompareOrdinal(prop.Name, "Properties") == 0)
                            {
                                foreach (var sub in prop.Value.EnumerateObject())
                                {
                                    switch (sub.Value.ValueKind)
                                    {
                                        case JsonValueKind.String:
                                            var s2 = sub.Value.GetString();
                                            if (s2 != null) // We don't load nulls
                                                thingLoaded.Properties.TryAdd(sub.Name, s2);
                                            continue;
                                        case JsonValueKind.True:
                                            thingLoaded.Properties.TryAdd(sub.Name, true);
                                            continue;
                                        case JsonValueKind.False:
                                            thingLoaded.Properties.TryAdd(sub.Name, false);
                                            continue;
                                        case JsonValueKind.Null:
                                            continue; // We don't load nulls
                                        case JsonValueKind.Object:
                                            continue; // We don't load sub-object graphs
                                        default:
                                            continue;
                                    }
                                }
                                continue;
                            }
                            else
                                continue;
                        }
                    default:
                        continue;
                }
            }

            return thingLoaded;
        }
        catch (JsonException je)
        {
            await Console.Error.WriteLineAsync($"ERR: Unable to deserialize thing {thingGuid} from {filePath}: {je.Message}");
            return null;
        }
    }

    public static async Task<Thing?> LoadFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            await Console.Error.WriteLineAsync($"ERR: No file for thing found at {filePath}");
            return null;
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            await Console.Error.WriteLineAsync($"ERR: Empty thing file found at {filePath}");
            fileInfo.Delete();
            return null;
        }

        var thingLoaded = new Thing("", "");
        using var fs = new FileStream(filePath, FileMode.Open);
        try
        {
            using var doc = await JsonDocument.ParseAsync(fs, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            if (root.TryGetProperty(nameof(Guid), out JsonElement guidProperty))
            {
                thingLoaded.Guid = guidProperty.GetString() ?? "<UNDEFINED>";
            }

            if (root.TryGetProperty(nameof(Name), out JsonElement nameProperty))
            {
                thingLoaded.Name = nameProperty.GetString() ?? "<UNDEFINED>";
            }

            if (root.TryGetProperty(nameof(SchemaGuid), out JsonElement schemaGuidProperty))
            {
                thingLoaded.SchemaGuid = schemaGuidProperty.GetString();
            }

            foreach (var prop in root.EnumerateObject())
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;

                if (
                  string.CompareOrdinal(prop.Name, nameof(Name)) == 0
                  || string.CompareOrdinal(prop.Name, nameof(Guid)) == 0
                  || string.CompareOrdinal(prop.Name, nameof(SchemaGuid)) == 0
                )
                {
                    // Ignore built-ins, as they're defined on root, not Properties
                    continue;
                }

                switch (prop.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        var s = prop.Value.GetString();
                        if (s != null) // We don't load nulls
                            thingLoaded.Properties.TryAdd(prop.Name, s);
                        continue;
                    case JsonValueKind.True:
                        thingLoaded.Properties.TryAdd(prop.Name, true);
                        continue;
                    case JsonValueKind.False:
                        thingLoaded.Properties.TryAdd(prop.Name, false);
                        continue;
                    case JsonValueKind.Null:
                        continue; // We don't load nulls
                    case JsonValueKind.Object:
                        {
                            if (string.CompareOrdinal(prop.Name, "Properties") == 0)
                            {
                                foreach (var sub in prop.Value.EnumerateObject())
                                {
                                    switch (sub.Value.ValueKind)
                                    {
                                        case JsonValueKind.String:
                                            var s2 = sub.Value.GetString();
                                            if (s2 != null) // We don't load nulls
                                                thingLoaded.Properties.TryAdd(sub.Name, s2);
                                            continue;
                                        case JsonValueKind.True:
                                            thingLoaded.Properties.TryAdd(sub.Name, true);
                                            continue;
                                        case JsonValueKind.False:
                                            thingLoaded.Properties.TryAdd(sub.Name, false);
                                            continue;
                                        case JsonValueKind.Null:
                                            continue; // We don't load nulls
                                        case JsonValueKind.Object:
                                            continue; // We don't load sub-object graphs
                                        default:
                                            continue;
                                    }
                                }
                                continue;
                            }
                            else
                                continue;
                        }
                    default:
                        continue;
                }
            }

            return thingLoaded;
        }
        catch (JsonException je)
        {
            await Console.Error.WriteLineAsync($"ERR: Unable to deserialize thing from {filePath}: {je.Message}");
            return null;
        }
    }


    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        var thingDir = await GetThingDatabaseDirectory();
        if (thingDir == null)
            return false;

        var fileName = $"{Guid}.thing.json";
        var filePath = Path.Combine(thingDir.FullName, fileName);

        using var fs = File.Create(filePath);
        try
        {
            await JsonSerializer.SerializeAsync(fs, this, cancellationToken: cancellationToken);
            await fs.FlushAsync(cancellationToken);
            return true;
        }
        catch (Exception je)
        {
            await Console.Error.WriteLineAsync($"ERR: Unable to serialize thing {Guid} from {filePath}: {je.Message}");
            return false;
        }
    }

    public static async Task<bool> GuidExists(string thingGuid, CancellationToken _)
    {
        if (string.IsNullOrWhiteSpace(thingGuid))
            return false;

        var thingDir = await GetThingDatabaseDirectory();
        if (thingDir == null)
            return false;

        var fileName = $"{thingGuid}.thing.json";
        var filePath = Path.Combine(thingDir.FullName, fileName);
        if (!File.Exists(filePath))
            return false;

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            fileInfo.Delete();
            return false;
        }

        return true;
    }

    public static async IAsyncEnumerable<Reference> ResolvePartialNameAsync(string thingNamePart, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var thingDir = await GetThingDatabaseDirectory();
        if (thingDir == null)
            yield break;

        // Load index
        var indexFilePath = Path.Combine(thingDir.FullName, NameIndexFileName);
        if (!File.Exists(indexFilePath))
            yield break; // Happens on new install if no items, nothing in index, and so no file

        await foreach (var guid in Program.ResolveGuidFromPartialNameAsync(indexFilePath, thingNamePart, cancellationToken))
        {
            yield return new Reference
            {
                Type = Reference.ReferenceType.Thing,
                Guid = guid
            };
        }
    }

    public async IAsyncEnumerable<Schema> GetAssociatedSchemas([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Does this thing adhere to any schemas?
        if (!string.IsNullOrWhiteSpace(SchemaGuid))
        {
            var schema = await Schema.LoadAsync(SchemaGuid, cancellationToken);
            if (schema != null)
                yield return schema;
        }
        yield break;
    }

    public static (string escapedPropKey, string fullDisplayName, string simpleDisplayName)
        CarvePropertyName(string truePropertyName, Schema? schema)
    {
        if (schema != default)
        {
            // Yes, this property belongs to a schema, so chop the schema guid off it for display purposes.
            var choppedPropName = truePropertyName[(schema.Guid.Length + 1)..];
            var escapedPropKey = choppedPropName.Contains(' ') && !choppedPropName.StartsWith('[') && !choppedPropName.EndsWith(']') ? $"[{choppedPropName}]" : choppedPropName;
            var fullDisplayName = $"{schema.EscapedName}.{escapedPropKey}";
            var simpleDisplayName = choppedPropName;

            // Watch out, the schema field could have been deleted but it's still there on the instance.
            if (!schema.Properties.TryGetValue(choppedPropName, out SchemaFieldBase? schemaField))
            {
                AnsiConsole.MarkupLineInterpolated($"[yellow]WARN[/]: Found property {truePropertyName} ({escapedPropKey}) on thing, but it doesn't appear on schema {schema.Name} ({schema.Guid}).");
                escapedPropKey = truePropertyName.Contains(' ') && !truePropertyName.StartsWith('[') && !truePropertyName.EndsWith(']') ? $"[{truePropertyName}]" : truePropertyName;
                fullDisplayName = escapedPropKey; // b0c1592e-5d79-4fe4-8814-aa6e534d2b7f.phone
                simpleDisplayName = truePropertyName; // b0c1592e-5d79-4fe4-8814-aa6e534d2b7f.phone
            }

            return (escapedPropKey, fullDisplayName, simpleDisplayName);
        }
        else
        {
            var escapedPropKey = truePropertyName.Contains(' ') && !truePropertyName.StartsWith('[') && !truePropertyName.EndsWith(']') ? $"[{truePropertyName}]" : truePropertyName;
            var fullDisplayName = escapedPropKey;
            var simpleDisplayName = truePropertyName;
            return (escapedPropKey, fullDisplayName, simpleDisplayName);
        }
    }

    public async IAsyncEnumerable<ThingProperty> GetProperties([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (Properties == null || Properties.Count == 0)
            yield break;

        // Does this thing adhere to any schemas?
        List<Schema> thingSchemas = [];
        await foreach (var schema in GetAssociatedSchemas(cancellationToken))
        {
            thingSchemas.Add(schema);
        }

        var unsetSchemaFields = thingSchemas
            .Select(s => new { Schema = s, s.Properties })
            .SelectMany(s => s.Properties.Select(p => (s.Schema, p.Key, p.Value)))
            .ToList();

        foreach (var thingProp in Properties)
        {
            // Does this property belong to a schema?
            bool valid = true;
            var schema = thingSchemas.FirstOrDefault(s => thingProp.Key.StartsWith($"{s.Guid}."));
            var (escapedPropKey, fullDisplayName, simpleDisplayName) = CarvePropertyName(thingProp.Key, schema);
            if (schema != default)
            {
                // Watch out, the schema field could have been deleted but it's still there on the instance.
                if (schema.Properties.TryGetValue(simpleDisplayName, out SchemaFieldBase? schemaField))
                    valid = schemaField == null || await schemaField.IsValidAsync(thingProp.Value, cancellationToken); // Valid if no schema.
            }

            yield return new ThingProperty
            {
                TruePropertyName = thingProp.Key,
                FullDisplayName = fullDisplayName,
                SimpleDisplayName = simpleDisplayName,
                SchemaGuid = schema?.Guid,
                Value = thingProp.Value,
                Valid = valid
            };
        }
    }

    public async Task<List<ThingUnsetProperty>> GetUnsetProperties(CancellationToken cancellationToken)
    {
        // Does this thing adhere to any schemas?
        List<Schema> thingSchemas = [];
        await foreach (var schema in GetAssociatedSchemas(cancellationToken))
        {
            thingSchemas.Add(schema);
        }

        var unsetSchemaFields = thingSchemas
            .Select(s => new { Schema = s, s.Properties })
            .SelectMany(s => s.Properties.Select(p => (s, s.Schema.Guid, s.Schema.Name, p.Key)))
            .ToDictionary(
                k => (k.Guid, k.Key),
                v =>
                {
                    var (escapedPropKey, fullDisplayName, simpleDisplayName) = CarvePropertyName(
                        $"{v.Guid}.{v.Key}",
                        v.s.Schema);
                    return new ThingUnsetProperty
                    {
                        FullDisplayName = fullDisplayName,
                        SimpleDisplayName = simpleDisplayName,
                        SchemaGuid = v.Guid,
                        SchemaName = v.Name,
                        Field = v.s.Properties[v.Key]
                    };
                });

        await foreach (var thingProperty in GetProperties(cancellationToken))
        {
            if (thingProperty.SchemaGuid != null) // Remove from list of schema properties once we note it's set on the thing.
                _ = unsetSchemaFields.Remove((thingProperty.SchemaGuid, thingProperty.SimpleDisplayName));
        }

        return unsetSchemaFields.Values.ToList();
    }

    public async Task<bool> Set(string propName, string? propValue, CancellationToken cancellationToken)
    {
        // If prop name came in unescaped, and it should be escaled, then escape it here for comparisons.
        if (propName.Contains(' ') && !propName.StartsWith('[') && !propName.EndsWith(']'))
            propName = $"[{propName}]";

        // Is this property alerady set?
        List<ThingProperty> candidateProperties = [];

        // Step 1, Check EXISTING properties on this thing.
        await foreach (var prop in GetProperties(cancellationToken))
        {
            if (string.Compare(propName, prop.FullDisplayName, StringComparison.CurrentCultureIgnoreCase) == 0
                && string.Compare(propName, nameof(Schema.Plural), StringComparison.OrdinalIgnoreCase) != 0 // Ignore schema built-in
            )
            {
                // For instance, user does set vendor.[Work Phone]=+12125551234
                candidateProperties.Add(prop);
            }
            else if (string.Compare(propName, prop.SimpleDisplayName, StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                // For instance, user does set [Work Phone]=+12125551234
                candidateProperties.Add(prop);
            }
        }

        // Step 2, Check properties on associated schemas NOT already set on this object
        await foreach (var schema in GetAssociatedSchemas(cancellationToken))
        {
            foreach (var schemaProperty in schema.Properties)
            {
                var truePropertyName = $"{schema.Guid}.{schemaProperty.Key}";

                // If this value was for this property, would it be valid?
                var wouldBeValid = await schemaProperty.Value.IsValidAsync(propValue, cancellationToken);
                var candidatesMatch = false;
                for (int i = 0; i < candidateProperties.Count; i++)
                {
                    if (string.CompareOrdinal(candidateProperties[i].TruePropertyName, truePropertyName) == 0)
                    {
                        //AnsiConsole.MarkupLineInterpolated($"[blue]DEBUG[/]: if {truePropertyName} were {propValue} validity would be {wouldBeValid}");
                        candidateProperties[i] = new ThingProperty
                        {
                            TruePropertyName = candidateProperties[i].TruePropertyName,
                            FullDisplayName = candidateProperties[i].FullDisplayName,
                            SimpleDisplayName = candidateProperties[i].SimpleDisplayName,
                            SchemaGuid = candidateProperties[i].SchemaGuid,
                            Value = candidateProperties[i].Value,
                            Valid = wouldBeValid
                        };
                        candidatesMatch = true;
                    }
                }
                if (candidatesMatch)
                    continue;// Already set, no need to add a phantom.

                //if (candidateProperties.Any(c => string.CompareOrdinal(c.TruePropertyName, truePropertyName) == 0))
                //    continue; // Already set, no need to add a phantom.

                var fullDisplayName = $"{schema.EscapedName}.{schemaProperty.Key}";
                var simpleDisplayName = schemaProperty.Key.Contains(' ') && !schemaProperty.Key.StartsWith('[') && !schemaProperty.Key.EndsWith(']') ? $"[{schemaProperty.Key}]" : schemaProperty.Key;
                var phantomProp = new ThingProperty
                {
                    TruePropertyName = truePropertyName,
                    FullDisplayName = fullDisplayName,
                    SimpleDisplayName = simpleDisplayName,
                    SchemaGuid = schema.Guid,
                    Value = null,
                    Valid = wouldBeValid
                };

                if (string.Compare(propName, fullDisplayName, StringComparison.CurrentCultureIgnoreCase) == 0
                    && string.Compare(propName, "plural", StringComparison.OrdinalIgnoreCase) != 0 // Ignore schema built-in
                )
                {
                    // For instance, user does set vendor.[Work Phone]=+12125551234
                    candidateProperties.Add(phantomProp);
                }
                if (string.Compare(propName, simpleDisplayName, StringComparison.CurrentCultureIgnoreCase) == 0
                    && string.Compare(propName, "plural", StringComparison.OrdinalIgnoreCase) != 0 // Ignore schema built-in
                )
                {
                    // For instance, user does set [Work Phone]=+12125551234
                    candidateProperties.Add(phantomProp);
                }
            }
        }

        switch (candidateProperties.Count)
        {
            case 0:
                // No existing property by this name on the thing (nor in any associated schema), so we're going to add it.
                if (string.IsNullOrWhiteSpace(propValue))
                    Properties.Remove(propName);
                else
                    Properties[propName] = propValue;

                // Special case for Name.
                if (string.Compare(propName, nameof(Name), StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (string.IsNullOrWhiteSpace(propValue))
                    {
                        AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Value of {nameof(Name)} cannot be empty.\r\n");
                        return false;
                    }
                    Name = propValue;
                }

                return await SaveAsync(cancellationToken);
            case 1:
                // Exactly one, we need to update:
                if (string.IsNullOrWhiteSpace(propValue))
                    Properties.Remove(candidateProperties[0].TruePropertyName);
                else
                    Properties[candidateProperties[0].TruePropertyName] = propValue;

                // Special case for Name.
                if (string.Compare(propName, nameof(Name), StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (string.IsNullOrWhiteSpace(propValue))
                    {
                        AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Value of {nameof(Name)} cannot be empty.\r\n");
                        return false;
                    }
                    Name = propValue;
                }

                if (!candidateProperties[0].Valid)
                    AnsiConsole.MarkupLineInterpolated($"[yellow]WARNING[/]: Value of {propName} is invalid.\r\n");

                return await SaveAsync(cancellationToken);
            default:
                // Ambiguous
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to determine which property between {candidateProperties.Select(x => x.TruePropertyName).Aggregate((c, n) => $"{c}, {n}")} to update.\r\n");
                return false;
        }

    }

    public static async Task<bool> RebuildIndexes(CancellationToken cancellationToken)
    {
        var thingDir = await GetThingDatabaseDirectory();
        if (thingDir == null)
            return false;

        Dictionary<string, Dictionary<string, string>> indexesToWrite = [];
        Dictionary<string, Dictionary<string, string>> schemaGuidsAndThingIndexes = [];

        await foreach (var schema in Schema.GetAll(cancellationToken))
        {
            schemaGuidsAndThingIndexes.Add(schema.Guid, []);
        }

        Dictionary<string, string> namesIndex = [];
        foreach (var thingFileName in thingDir.GetFiles("*.thing.json"))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var thing = await LoadFileAsync(thingFileName.FullName, cancellationToken);
            if (thing == null)
                continue;

            namesIndex.Add(thing.Name, thingFileName.Name);
            if (!string.IsNullOrWhiteSpace(thing.SchemaGuid)
                && schemaGuidsAndThingIndexes.TryGetValue(thing.SchemaGuid, out Dictionary<string, string>? value))
            {
                value.Add(thing.Guid, thingFileName.Name);
            }
        }
        indexesToWrite.Add(Path.Combine(thingDir.FullName, NameIndexFileName), namesIndex);
        foreach (var kvp in schemaGuidsAndThingIndexes)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            indexesToWrite.Add(Path.Combine(thingDir.FullName, $"_thing.schema.{kvp.Key}.csv"), kvp.Value);
        }

        foreach (var index in indexesToWrite)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            if (index.Value.Count == 0)
            {
                if (File.Exists(index.Key)) { }
                File.Delete(index.Key);
                continue;
            }

            using var fs = File.Create(index.Key);
            await IndexManager.AddAsync(fs, index.Value, cancellationToken);
        }

        return true;
    }

    public override string ToString() => Name;
}
