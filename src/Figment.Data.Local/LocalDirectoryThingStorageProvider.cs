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

using System.Runtime.CompilerServices;
using System.Text.Json;
using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;

namespace Figment.Data.Local;

/// <summary>
/// A <see cref="Thing"/> storage provider implementation that stores objects in files on a local file system.
/// </summary>
/// <param name="ThingDirectoryPath">The path to the <see cref="Thing"/> subdirectory under the root of the file system database.</param>
public class LocalDirectoryThingStorageProvider(string ThingDirectoryPath) : ThingStorageProviderBase, IThingStorageProvider
{
    private const string NameIndexFileName = $"_thing.names.csv";

    /// <inheritdoc/>
    public override async Task<Reference> FindByNameAsync(string exactName, CancellationToken cancellationToken, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
    {
        await foreach (var reference in FindByNameAsync(
            new Func<string, bool>(x => string.Equals(x, exactName, comparisonType)), cancellationToken))
        {
            return reference.reference; // Returns the first match
        }

        return Reference.EMPTY;
    }

    private async IAsyncEnumerable<(Reference reference, string name)> FindByNameAsync(Func<string, bool> selector, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);

        // Add to index
        var indexFilePath = Path.Combine(ThingDirectoryPath, NameIndexFileName);
        if (!File.Exists(indexFilePath))
            yield break;

        await foreach (var entry in IndexManager.LookupAsync(
            indexFilePath
            , e => selector(e.Key)
            , cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var thingFileName = entry.Value;
            var thingGuid = thingFileName.Split('.')[0];
            var thing = await LoadAsync(thingGuid, cancellationToken);
            if (thing != null)
                yield return (new Reference
                {
                    Guid = thingGuid,
                    Type = Reference.ReferenceType.Thing
                }, thing.Name);
        }

        yield break;
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<PossibleNameMatch> FindByPartialNameAsync(string schemaGuid, string thingNamePart, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);
        ArgumentException.ThrowIfNullOrWhiteSpace(thingNamePart);

        // Load index
        var indexFilePath = Path.Combine(ThingDirectoryPath, $"_thing.names.schema.{schemaGuid}.csv");
        if (!File.Exists(indexFilePath))
            yield break; // Happens on new install if no items, nothing in index, and so no file

        await foreach (var guid in IndexManager.ResolveGuidFromPartialNameAsync(indexFilePath, thingNamePart, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var thing = await LoadAsync(guid, cancellationToken);
            if (thing == null || string.IsNullOrWhiteSpace(thing.Name))
                continue;
            yield return new PossibleNameMatch
            {
                Reference = new()
                {
                    Type = Reference.ReferenceType.Thing,
                    Guid = guid
                },
                Name = thing.Name
            };
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<(Reference reference, string? name)> GetAll([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Shortcut - try to get them by name index first.
        var indexFilePath = Path.Combine(ThingDirectoryPath, NameIndexFileName);
        if (File.Exists(indexFilePath))
        {
            await foreach (var indexMatch in FindByNameAsync(x => true, cancellationToken))
            {
                yield return indexMatch;
            }
            yield break;
        }

        await foreach (var thing in LoadAll(cancellationToken))
        {
            yield return (new Reference
            {
                Guid = thing.Guid,
                Type = Reference.ReferenceType.Thing
            }, thing?.Name);
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<LocalThing> LoadAll([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var thingDir = new DirectoryInfo(ThingDirectoryPath);
        if (!thingDir.Exists)
            yield break;

        foreach (var file in thingDir.EnumerateFiles("*.thing.json", new EnumerationOptions
        {
            AttributesToSkip = FileAttributes.Offline,
            IgnoreInaccessible = true,
        })
        )
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var thingGuidString = file.Name.Split(".thing.json"); // We know this is at the end because of the EnumerateFiles() searchPattern above.
            if (!string.IsNullOrWhiteSpace(thingGuidString[0])
                && Guid.TryParse(thingGuidString[0], out Guid _))
            {
                var thing = await LoadAsync(thingGuidString[0], cancellationToken);
                if (thing != null)
                    yield return (LocalThing)thing;
            }
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<Reference> GetBySchemaAsync(string schemaGuid, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        var thingDir = new DirectoryInfo(ThingDirectoryPath);
        if (!thingDir.Exists)
            yield break;

        var indexFilePath = Path.Combine(thingDir.FullName, $"_thing.schema.{schemaGuid}.csv");
        if (File.Exists(indexFilePath))
        {
            // Use index
            await foreach (var entry in IndexManager.LookupAsync(indexFilePath, e => true, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                var guid = Path.GetFileName(entry.Value).Split('.')[0];
                yield return new Reference
                {
                    Guid = guid,
                    Type = Reference.ReferenceType.Thing
                };
            }
        }
        else
        {
            // No index, so go the expensive route
            AmbientErrorContext.Provider.LogWarning($"Missing index at: {indexFilePath}");
            await foreach (var (reference, name) in GetAll(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                var thing = await LoadAsync(reference.Guid, cancellationToken);
                if (thing != null && thing.SchemaGuids.Any(s => string.Equals(s, schemaGuid, StringComparison.Ordinal)))
                    yield return reference;
            }
        }
    }

    /// <inheritdoc/>
    public override Task<bool> GuidExists(string thingGuid, CancellationToken _)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingGuid);

        var fileName = $"{thingGuid}.thing.json";
        var filePath = Path.Combine(ThingDirectoryPath, fileName);
        if (!File.Exists(filePath))
            return Task.FromResult(false);

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            try
            {
                fileInfo.Delete();
            }
            catch (Exception ex)
            {
                AmbientErrorContext.Provider.LogException(ex, $"Zero length file found but could not be deleted at: {filePath}");
            }
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public override async Task<Thing?> LoadAsync(string thingGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingGuid);

        var fileName = $"{thingGuid}.thing.json";
        var filePath = Path.Combine(ThingDirectoryPath, fileName);
        return await LoadFileAsync(filePath, cancellationToken);
    }

    private static async Task<LocalThing?> LoadFileAsync(string filePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing. No file found at {filePath}");
            return null;
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing. Empty thing file found at {filePath}");
            fileInfo.Delete();
            return null;
        }

        using var fs = new FileStream(filePath, FileMode.Open);
        try
        {
            using var doc = await JsonDocument.ParseAsync(fs, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            if (!root.TryGetProperty(nameof(Thing.Guid), out JsonElement guidProperty))
            {
                AmbientErrorContext.Provider.LogError($"Unable to load thing. No required property {nameof(Thing.Guid)} found in file {filePath}");
                return null;
            }

            var guid = guidProperty.GetString();
            if (string.IsNullOrWhiteSpace(guid))
            {
                AmbientErrorContext.Provider.LogError($"Unable to load thing. Required property {nameof(Thing.Guid)} was null or blank in file {filePath}");
                return null;
            }

            string thingName;
            if (root.TryGetProperty(nameof(Thing.Name), out JsonElement nameProperty))
            {
                thingName = nameProperty.GetString() ?? "<UNDEFINED>";
                if (!Thing.IsThingNameValid(thingName))
                {
                    AmbientErrorContext.Provider.LogError($"Unable to load thing. Required property {nameof(Thing.Name)} is '{thingName}', which is not valid.");
                    return null;
                }
            }
            else
            {
                AmbientErrorContext.Provider.LogError($"Unable to load thing. Required property {nameof(Thing.Name)} was null or blank in file {filePath}");
                return null;
            }

            var thingLoaded = new LocalThing(guid, thingName, filePath)
            {
                CreatedOn = fileInfo.CreationTimeUtc,
                LastModified = fileInfo.LastWriteTimeUtc,
                LastAccessed = fileInfo.LastAccessTimeUtc
            };

            // Legacy
            if (root.TryGetProperty("SchemaGuid", out JsonElement schemaGuidProperty))
            {
                var guidString = schemaGuidProperty.GetString();
                if (!string.IsNullOrWhiteSpace(guidString))
                {
                    thingLoaded.SchemaGuids = [guidString];
                }
            }
            else
                thingLoaded.SchemaGuids = [];

            if (root.TryGetProperty(nameof(Thing.SchemaGuids), out JsonElement schemaGuidsProperty))
            {
                foreach (var element in schemaGuidsProperty.EnumerateArray())
                {
                    var elementValue = element.GetString();
                    if (!string.IsNullOrWhiteSpace(elementValue))
                    {
                        if (!thingLoaded.SchemaGuids.Contains(elementValue))
                        {
                            thingLoaded.SchemaGuids.Add(elementValue);
                        }
                    }
                }
            }

            foreach (var prop in root.EnumerateObject())
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;

                if (
                    string.Equals(prop.Name, nameof(Thing.Name), StringComparison.Ordinal)
                    || string.Equals(prop.Name, nameof(Thing.Guid), StringComparison.Ordinal)
                    || string.Equals(prop.Name, nameof(Thing.SchemaGuids), StringComparison.Ordinal))
                {
                    // Ignore built-ins, as they're defined on root, not Properties
                    continue;
                }

                switch (prop.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        var s = prop.Value.GetString();
                        if (s != null) // We don't load nulls
                            thingLoaded.TryAddProperty(prop.Name, s);
                        continue;
                    case JsonValueKind.True:
                        thingLoaded.TryAddProperty(prop.Name, true);
                        continue;
                    case JsonValueKind.False:
                        thingLoaded.TryAddProperty(prop.Name, false);
                        continue;
                    case JsonValueKind.Null:
                        continue; // We don't load nulls
                    case JsonValueKind.Object:
                        {
                            if (string.Equals(prop.Name, "Properties", StringComparison.Ordinal))
                            {
                                foreach (var sub in prop.Value.EnumerateObject())
                                {
                                    switch (sub.Value.ValueKind)
                                    {
                                        case JsonValueKind.String:
                                            var s2 = sub.Value.GetString();
                                            if (s2 != null) // We don't load nulls
                                                thingLoaded.TryAddProperty(sub.Name, s2);
                                            continue;
                                        case JsonValueKind.Number:
                                            if (sub.Value.TryGetUInt64(out ulong u64))
                                                thingLoaded.TryAddProperty(sub.Name, u64);
                                            else if (sub.Value.TryGetDouble(out double dbl))
                                                thingLoaded.TryAddProperty(sub.Name, dbl);
                                            else
                                                AmbientErrorContext.Provider.LogWarning($"Unable to parse property {sub.Name} value '{sub.Value}' as number from: {filePath}");
                                            continue;
                                        case JsonValueKind.True:
                                            thingLoaded.TryAddProperty(sub.Name, true);
                                            continue;
                                        case JsonValueKind.False:
                                            thingLoaded.TryAddProperty(sub.Name, false);
                                            continue;
                                        case JsonValueKind.Null:
                                            AmbientErrorContext.Provider.LogWarning($"Unable to parse property {sub.Name} with unsupported value type '{Enum.GetName(sub.Value.ValueKind)}' from: {filePath}");
                                            continue; // We don't load nulls
                                        case JsonValueKind.Object:
                                            AmbientErrorContext.Provider.LogWarning($"Unable to parse property {sub.Name} with unsupported value type '{Enum.GetName(sub.Value.ValueKind)}' from: {filePath}");
                                            continue; // We don't load sub-object graphs
                                        case JsonValueKind.Array:
                                            {
                                                var aLen = sub.Value.GetArrayLength();
                                                var array = new object?[aLen];
                                                var ai = 0;

                                                foreach (var element in sub.Value.EnumerateArray())
                                                {
                                                    switch (element.ValueKind)
                                                    {
                                                        case JsonValueKind.Null:
                                                            array[ai] = null;
                                                            break;
                                                        case JsonValueKind.String:
                                                            array[ai] = element.GetString();
                                                            break;
                                                        default:
                                                            AmbientErrorContext.Provider.LogWarning($"Unable to parse property {sub.Name} with unsupported value type '{Enum.GetName(sub.Value.ValueKind)}' in array position {ai} (value='{element}') from: {filePath}");
                                                            break;
                                                    }
                                                    ai++;
                                                }

                                                thingLoaded.TryAddProperty(sub.Name, array);
                                                continue;
                                            }
                                        default:
                                            AmbientErrorContext.Provider.LogWarning($"Unable to parse property {sub.Name} with unsupported value type '{Enum.GetName(sub.Value.ValueKind)}' from: {filePath}");
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
            AmbientErrorContext.Provider.LogException(je, $"Unable to deserialize thing from {filePath}");
            return null;
        }
    }

    /// <inheritdoc/>
    public override async Task<(bool success, string? message)> SaveAsync(Thing thing, CancellationToken cancellationToken)
    {
        var thingDir = new DirectoryInfo(ThingDirectoryPath);
        if (!thingDir.Exists)
        {
            return (false, $"Directory '{ThingDirectoryPath}' not found.");
        }

        if (string.IsNullOrWhiteSpace(thing.Guid))
        {
            return (false, $"Thing '{thing.Name ?? "<UNNAMED>"}' has a missing GUID.");
        }

        if (!Thing.IsThingNameValid(thing.Name))
        {
            return (false, $"Name '{thing.Name}' is invalid for thing '{thing.Guid}'.");
        }

        if (thing.SchemaGuids == null || thing.SchemaGuids.Count == 0)
        {
            return (false, $"Thing '{thing.Name}' has no schema(s).");
        }

        var fileName = $"{thing.Guid}.thing.json";
        var filePath = Path.Combine(thingDir.FullName, fileName);
        var backupFileName = $"{thing.Guid}.thing.json.backup";
        var backupFilePath = Path.Combine(thingDir.FullName, backupFileName);

        if (File.Exists(filePath))
            File.Move(filePath, backupFilePath, true);

        using var fs = File.Create(filePath);
        try
        {
            await JsonSerializer.SerializeAsync(
                fs,
                thing,
                ThingSourceGenerationContext.Default.Thing,
                cancellationToken: cancellationToken);
            await fs.FlushAsync(cancellationToken);

            if (File.Exists(backupFilePath))
                File.Delete(backupFilePath);

            return (true, $"{thing.Guid} saved to {filePath}.");
        }
        catch (Exception je)
        {
            var errorMessage = $"Unable to serialize thing {thing.Guid} from {filePath}";
            AmbientErrorContext.Provider.LogException(je, errorMessage);

            if (File.Exists(backupFilePath))
                File.Move(backupFilePath, filePath, true);

            return (false, errorMessage);
        }
    }

    /// <inheritdoc/>
    public override async Task<CreateThingResult> CreateAsync(Schema? schema, string thingName, Dictionary<string, object?> properties, CancellationToken cancellationToken)
    {
        var thingGuid = Guid.NewGuid().ToString();
        var thing = new Thing(thingGuid, thingName)
        {
            SchemaGuids = schema == null ? [] : [schema.Guid],
            CreatedOn = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
        };

        var thingFileName = $"{thingGuid}.thing.json";
        var thingFilePath = Path.Combine(ThingDirectoryPath, thingFileName);

        if (File.Exists(thingFilePath))
        {
            AmbientErrorContext.Provider.LogError($"File for thing {thingName} already exists at {thingFilePath}");
            return new CreateThingResult { Success = false, Message = $"File for thing {thingName} already exists at {thingFilePath}" };
        }

        using var fs = new FileStream(thingFilePath, FileMode.CreateNew);
        try
        {
            await JsonSerializer.SerializeAsync(fs, thing!, ThingSourceGenerationContext.Default.Thing, cancellationToken: cancellationToken);
            await fs.FlushAsync(cancellationToken);
        }
        catch (Exception)
        {
            fs.Close();
            File.Delete(thingFilePath);
            throw;
        }

        // Add to name index
        {
            var indexFilePath = Path.Combine(ThingDirectoryPath, NameIndexFileName);
            await IndexManager.AddAsync(indexFilePath, thingName, thingFileName, cancellationToken);
        }

        if (schema != null)
            await AssociateWithSchemaInternal(thing, schema, cancellationToken);

        // Load fresh to handle any schema defaults/calculated fields
        thing = await LoadAsync(thingGuid, cancellationToken);

        if (thing == null)
        {
            return new CreateThingResult { Success = false, Message = $"Unable to load thing {thingGuid}" };
        }

        // If this schema has an increment field, set its value.
        var caifi = await CreateAsyncIncrementFieldInternal(schema, thing, cancellationToken);
        if (!caifi.success)
        {
            return new CreateThingResult { Success = caifi.success, Message = caifi.message };
        }

        var tsr = await thing.Set(properties, cancellationToken);
        if (!tsr.Success)
        {
            return new CreateThingResult { Success = false, Message = (tsr.Messages == null || tsr.Messages.Length == 0) ? "No error message provided." : string.Join("; ", tsr.Messages) };
        }

        var (success, message) = await SaveAsync(thing, cancellationToken);
        if (!success)
        {
            return new CreateThingResult { Success = false, Message = message };
        }

        return new CreateThingResult { Success = thing != null, NewThing = thing };
    }

    /// <inheritdoc/>
    public override async Task<(bool, Thing?)> AssociateWithSchemaAsync(string thingGuid, Schema schema, CancellationToken cancellationToken)
    {
        var thing = await LoadAsync(thingGuid, cancellationToken);
        if (thing == null)
            return (false, null);

        return await AssociateWithSchemaInternal(thing, schema, cancellationToken);
    }

    private async Task<(bool, Thing?)> AssociateWithSchemaInternal(Thing thing, Schema schema, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(thing);
        ArgumentNullException.ThrowIfNull(schema);

        var thingFileName = $"{thing.Guid}.thing.json";
        var thingFilePath = Path.Combine(ThingDirectoryPath, thingFileName);

        if (!File.Exists(thingFilePath))
            return (false, null); // Thing doesn't exist...

        if (!thing.SchemaGuids.Contains(schema.Guid))
        {
            thing.SchemaGuids.Add(schema.Guid);
            var (saved, _) = await thing.SaveAsync(cancellationToken);
            if (!saved)
                return (false, null);
        }

        // Add to schema name index, if applicable
        {
            var indexFilePath = Path.Combine(ThingDirectoryPath, $"_thing.names.schema.{schema.Guid}.csv");
            await IndexManager.AddAsync(indexFilePath, thing.Name, thingFileName, cancellationToken);
        }

        // Add it to the schema index
        {
            var indexFilePath = Path.Combine(ThingDirectoryPath, $"_thing.schema.{schema.Guid}.csv");
            await IndexManager.AddAsync(indexFilePath, thing.Guid, thingFileName, cancellationToken);
        }

        return (true, thing);
    }

    /// <inheritdoc/>
    public override async Task<(bool, Thing?)> DissociateFromSchemaAsync(string thingGuid, string schemaGuid, CancellationToken cancellationToken)
    {
        var thing = await LoadAsync(thingGuid, cancellationToken);
        if (thing == null)
            return (false, null);

        return await DissociateFromSchemaInternal(thing, schemaGuid, cancellationToken);
    }

    private async Task<(bool, Thing?)> DissociateFromSchemaInternal(Thing thing, string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(thing);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        var thingFileName = $"{thing.Guid}.thing.json";
        var thingFilePath = Path.Combine(ThingDirectoryPath, thingFileName);

        if (!File.Exists(thingFilePath))
            return (false, null); // Thing doesn't exist...

        if (thing.SchemaGuids.Contains(schemaGuid))
        {
            thing.SchemaGuids.RemoveAll(new Predicate<string>(s => string.Equals(schemaGuid, s, StringComparison.InvariantCultureIgnoreCase)));
            var (saved, saveMessage) = await thing.SaveAsync(cancellationToken);
            if (!saved)
                return (false, null);
        }

        // Remove from schema name index, if applicable, and ignore any errors
        {
            var indexFilePath = Path.Combine(ThingDirectoryPath, $"_thing.names.schema.{schemaGuid}.csv");
            var edited = await IndexManager.RemoveByValueAsync(indexFilePath, thingFileName, cancellationToken);
            if (!edited)
                AmbientErrorContext.Provider.LogWarning($"Unable to remove from schema names index at: {indexFilePath}");
        }

        // Remove from the schema index
        {
            var indexFilePath = Path.Combine(ThingDirectoryPath, $"_thing.schema.{schemaGuid}.csv");
            var edited = await IndexManager.RemoveByValueAsync(indexFilePath, thingFileName, cancellationToken);
            if (!edited)
                AmbientErrorContext.Provider.LogWarning($"Unable to remove from schema index at: {indexFilePath}");
        }

        return (true, thing);
    }

    /// <inheritdoc/>
    public override async Task<bool> RebuildIndexes(CancellationToken cancellationToken)
    {
        var thingDir = new DirectoryInfo(ThingDirectoryPath);
        if (!thingDir.Exists)
            return false;

        Dictionary<string, Dictionary<string, string>> indexesToWrite = [];
        Dictionary<string, Dictionary<string, string>> schemaGuidsAndThingIndexes = [];
        Dictionary<string, Dictionary<string, string>> schemaGuidsAndThingNames = [];

        // Build NAMES index and INCREMENT index
        Dictionary<string, string> namesIndex = [];
        foreach (var thingFileName in thingDir.GetFiles("*.thing.json"))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var thing = await LoadFileAsync(thingFileName.FullName, cancellationToken);
            if (thing == null)
                continue;

            // NAMES index
            if (namesIndex.TryGetValue(thing.Name, out string? existingNamedThingPath))
            {
                AmbientErrorContext.Provider.LogError($"Unable to index named thing '{thing.Name}' in '{thingFileName.Name}'.  Another thing of the same name is already in '{existingNamedThingPath}'.");
            }
            else if (namesIndex.TryAdd(thing.Name, thingFileName.Name))
            {
                if (thing.SchemaGuids != null)
                    foreach (var schemaGuid in thing.SchemaGuids)
                        if (!string.IsNullOrWhiteSpace(schemaGuid))
                        {
                            if (!schemaGuidsAndThingIndexes.TryAdd(schemaGuid, new Dictionary<string, string>() { { thing.Guid, thingFileName.Name } }))
                            {
                                if (schemaGuidsAndThingIndexes.TryGetValue(schemaGuid, out Dictionary<string, string>? value))
                                {
                                    value?.TryAdd(thing.Guid, thingFileName.Name);
                                }
                            }
                        }

                if (thing.SchemaGuids != null)
                    foreach (var schemaGuid in thing.SchemaGuids)
                        if (!string.IsNullOrWhiteSpace(schemaGuid))
                        {
                            if (!schemaGuidsAndThingNames.TryAdd(schemaGuid, new Dictionary<string, string>() { { thing.Name, thingFileName.Name } }))
                            {
                                if (schemaGuidsAndThingNames.TryGetValue(schemaGuid, out Dictionary<string, string>? value))
                                {
                                    value?.TryAdd(thing.Name, thingFileName.Name);
                                }
                            }
                        }
            }
            else
            {
                AmbientErrorContext.Provider.LogError($"Unable to index named thing '{thing.Name}' in '{thingFileName.Name}'.");
            }
        }
        indexesToWrite.Add(Path.Combine(thingDir.FullName, NameIndexFileName), namesIndex);

        // Write Things-of-Schema index
        foreach (var kvp in schemaGuidsAndThingIndexes)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            indexesToWrite.Add(Path.Combine(thingDir.FullName, $"_thing.schema.{kvp.Key}.csv"), kvp.Value);
        }

        // Write NAMES index
        foreach (var kvp in schemaGuidsAndThingNames)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            indexesToWrite.Add(Path.Combine(thingDir.FullName, $"_thing.names.schema.{kvp.Key}.csv"), kvp.Value);
        }

        foreach (var index in indexesToWrite)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            if (index.Value.Count == 0)
            {
                if (File.Exists(index.Key))
                    File.Delete(index.Key);
                continue;
            }

            using var fs = File.Create(index.Key);
            await IndexManager.AddAsync(fs, index.Value, cancellationToken);
        }

        // Renumber increment fields on any associated schemas.
        foreach (var schemaGuid in schemaGuidsAndThingIndexes.Keys)
            await RenumberIncrementField(schemaGuid, cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public override async Task<bool> RenumberIncrementField(string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        var thingDir = new DirectoryInfo(ThingDirectoryPath);
        if (!thingDir.Exists)
            return false;

        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (ssp == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
            return false;
        }

        var schema = await ssp.LoadAsync(schemaGuid, cancellationToken);
        if (schema == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load schema '{schemaGuid}'.");
            return false;
        }

        // Does this schema have an increment field?  If so, choose the first, ordered by the key.
        var incrementProperty = schema.GetIncrementField();
        if (incrementProperty == default)
        {
            return false;
        }

        Dictionary<string, Dictionary<string, string>> indexesToWrite = [];

        Dictionary<Reference, (long existingId, DateTime createdOn, string path)>? metadata = [];

        await foreach (var thingRef in GetBySchemaAsync(schemaGuid, cancellationToken))
        {
            var thing = (LocalThing?)await LoadAsync(thingRef.Guid, cancellationToken);
            if (thing == null)
                continue;

            long existingId = 0;
            if (thing.Properties.TryGetValue(incrementProperty.Name, out object? eid)
                && long.TryParse(eid.ToString() ?? string.Empty, out long eidLong))
            {
                existingId = eidLong;
            }

            metadata.Add(thing, (existingId, thing.CreatedOn, thing.FilePath));
        }

        // Write INCREMENT index
        var reorderedBase = metadata
            .OrderBy(x => x.Value.existingId)
            .ThenBy(x => x.Value.createdOn)
            .Select((x, i) => new { reference = x.Key, index = (ulong)i + 1, x.Value.path })
            .ToArray();

        var reorderedIndex = reorderedBase
            .ToDictionary(k => k.index.ToString(), v => v.path);

        indexesToWrite.Add(Path.Combine(thingDir.FullName, $"_thing.inc.schema.{schemaGuid}.csv"), reorderedIndex);

        var reorderedBulk = reorderedBase
            .ToDictionary(k => k.reference, v => new Dictionary<string, object?> { { incrementProperty.Name, v.index } });

        // Update things with updated indexes
        var (bulkSuccess, _) = await TryBulkUpdate(reorderedBulk, cancellationToken);
        if (!bulkSuccess)
        {
            AmbientErrorContext.Provider.LogWarning($"Bulk update of increment values on {schemaGuid} failed.");
        }

        foreach (var index in indexesToWrite)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (index.Value.Count == 0)
            {
                if (File.Exists(index.Key))
                    File.Delete(index.Key);
                continue;
            }

            using var fs = File.Create(index.Key);
            await IndexManager.AddAsync(fs, index.Value, cancellationToken);
        }

        // Save new maximum to the increment field definition on the schema.
        incrementProperty.NextValue = reorderedBase.Max(x => x.index) + 1;
        var (schemaSaveSuccess, schemaSaveMessage) = await schema.SaveAsync(cancellationToken);
        if (!schemaSaveSuccess)
        {
            AmbientErrorContext.Provider.LogError($"Unable to update increment field {incrementProperty.Name} on {schema.Name}: {schemaSaveMessage}");
            return false;
        }

        AmbientErrorContext.Provider.LogInfo($"Renumbered increments on schema: {schema.Name}");
        return true;
    }

    /// <inheritdoc/>
    public override async Task<bool> DeleteAsync(string thingGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingGuid);

        var thingDir = new DirectoryInfo(ThingDirectoryPath);
        if (!thingDir.Exists)
            return false;

        var thingFileName = $"{thingGuid}.thing.json";
        var thingFilePath = Path.Combine(thingDir.FullName, thingFileName);

        if (!File.Exists(thingFilePath))
        {
            AmbientErrorContext.Provider.LogWarning($"File for thing {thingGuid} does not exist at {thingFilePath}. Nothing to do.");
            return false;
        }

        // Must do this before delete
        var thing = await LoadAsync(thingGuid, cancellationToken);

        try
        {
            File.Delete(thingFilePath);
        }
        catch (Exception ex)
        {
            AmbientErrorContext.Provider.LogException(ex, $"Unable to delete thing file '{thingFilePath}'");
            return false;
        }

        // Remove from name index
        {
            var indexFilePath = Path.Combine(thingDir.FullName, NameIndexFileName);
            if (File.Exists(indexFilePath))
            {
                await IndexManager.RemoveByValueAsync(indexFilePath, thingFileName, cancellationToken);
                AmbientErrorContext.Provider.LogProgress($"Deleted from name index {Path.GetFileName(indexFilePath)}");
            }
        }

        if (thing == null)
        {
            AmbientErrorContext.Provider.LogWarning($"Unable to load thing {thingGuid}, so it may still exist in schema indexes.  Rebuild thing indexes to be sure.");
            return true;
        }

        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (ssp == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
            AmbientErrorContext.Provider.LogWarning($"Rebuild thing indexes to be sure of consistency.");
            return true;
        }

        // Remove from schema indexes, as applicable.
        var schemaCache = new Dictionary<string, Schema>();
        if (thing.SchemaGuids != null)
        {
            foreach (var schemaGuid in thing.SchemaGuids)
            {
                if (!string.IsNullOrWhiteSpace(schemaGuid))
                {
                    // Remove schema index, if applicable
                    var schemaIndexFilePath = Path.Combine(thingDir.FullName, $"_thing.schema.{schemaGuid}.csv");
                    if (File.Exists(schemaIndexFilePath))
                    {
                        await IndexManager.RemoveByKeyAsync(schemaIndexFilePath, thing.Guid, cancellationToken);
                        AmbientErrorContext.Provider.LogProgress($"Deleted from schema index {Path.GetFileName(schemaIndexFilePath)}");
                    }

                    // Remove schema name index, if applicable
                    var namesIndexFilePath = Path.Combine(thingDir.FullName, $"_thing.names.schema.{schemaGuid}.csv");
                    if (File.Exists(namesIndexFilePath))
                    {
                        await IndexManager.RemoveByValueAsync(namesIndexFilePath, thingFileName, cancellationToken);
                        AmbientErrorContext.Provider.LogProgress($"Deleted from name schema index {Path.GetFileName(namesIndexFilePath)}");
                    }

                    if (!schemaCache.TryGetValue(schemaGuid, out Schema? schema))
                    {
                        schema = await ssp.LoadAsync(schemaGuid, cancellationToken);
                        if (schema != null)
                        {
                            schemaCache.TryAdd(schemaGuid, schema);
                        }
                    }

                    if (schema != null)
                    {
                        var increment = schema.GetIncrementField();
                        if (increment != null)
                        {
                            var incrementIndexFilePath = Path.Combine(thingDir.FullName, $"_thing.inc.schema.{schemaGuid}.csv");
                            var incrementProp = await thing.GetPropertyByTrueNameAsync($"{schemaGuid}.{increment.Name}", cancellationToken);
                            var incrementValue = incrementProp?.AsString(); // We cast it as a string here because that is how its stored in the CSV file.
                            if (incrementValue != null)
                            {
                                await IndexManager.RemoveByKeyAsync(incrementIndexFilePath, incrementValue, cancellationToken);
                                AmbientErrorContext.Provider.LogProgress($"Deleted from name increment index {Path.GetFileName(incrementIndexFilePath)}");

                                // If the value we are deleting happens to be next-1, then reset schema next to next-1 for an immediate recycle.
                                var sif = schema.GetIncrementField();
                                if (sif != null)
                                {
                                    if (sif.NextValue == incrementProp!.Value.AsUInt64() + 1)
                                    {
                                        sif.NextValue--;
                                        await schema.SaveAsync(cancellationToken);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return true;
    }
}
