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

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        // Required for $ref properties with type descriminator
        AllowOutOfOrderMetadataProperties = true,
#if DEBUG
        WriteIndented = true,
#endif
    };

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
    public override async IAsyncEnumerable<Thing> LoadAll([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var thingDir = new DirectoryInfo(ThingDirectoryPath);
        if (thingDir == null || !thingDir.Exists)
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

            var thingGuidString = file.Name.Split(".thing.json");
            if (!string.IsNullOrWhiteSpace(thingGuidString[0])
                && Guid.TryParse(thingGuidString[0], out Guid _))
            {
                var thing = await LoadAsync(thingGuidString[0], cancellationToken);
                if (thing != null)
                    yield return thing;
            }
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<Reference> GetBySchemaAsync(string schemaGuid, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        var thingDir = new DirectoryInfo(ThingDirectoryPath);
        if (thingDir == null || !thingDir.Exists)
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
            yield break;
        }

        // No index, so go the expensive route
        AmbientErrorContext.Provider.LogWarning($"Missing index at: {indexFilePath}");
        await foreach (var thingRef in GetAll(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var thing = await LoadAsync(thingRef.reference.Guid, cancellationToken);
            if (thing != null && thing.SchemaGuids.Any(s => string.Equals(s, schemaGuid, StringComparison.Ordinal)))
                yield return thingRef.reference;
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

    private static async Task<Thing?> LoadFileAsync(string filePath, CancellationToken cancellationToken)
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

            var thingLoaded = new Thing(guid, thingName)
            {
                CreatedOn = fileInfo.CreationTimeUtc,
                LastModified = fileInfo.LastWriteTimeUtc,
                LastAccessed = fileInfo.LastAccessTimeUtc
            };

            // Legacy
            if (root.TryGetProperty("SchemaGuid", out JsonElement schemaGuidProperty))
                thingLoaded.SchemaGuids = [schemaGuidProperty.GetString()];
            else
                thingLoaded.SchemaGuids = [];

            if (root.TryGetProperty(nameof(Thing.SchemaGuids), out JsonElement schemaGuidsProperty))
            {
                var arrayCount = schemaGuidsProperty.GetArrayLength();
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
                                                        case JsonValueKind.String:
                                                            array[ai] = element.GetString();
                                                            break;
                                                        default:
                                                            AmbientErrorContext.Provider.LogWarning($"Unable to parse property {sub.Name} with unsupported value type '{Enum.GetName(sub.Value.ValueKind)}' in array position {ai} (value='{element.GetString()}') from: {filePath}");
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
        if (thingDir == null || !thingDir.Exists)
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
            await JsonSerializer.SerializeAsync(fs, thing, jsonSerializerOptions, cancellationToken: cancellationToken);
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
                File.Move(backupFilePath, filePath);

            return (false, errorMessage);
        }
    }

    /// <inheritdoc/>
    public override async Task<Thing?> CreateAsync(string? schemaGuid, string thingName, CancellationToken cancellationToken)
    {
        var thingGuid = Guid.NewGuid().ToString();
        var thing = new Thing(thingGuid, thingName)
        {
            SchemaGuids = [schemaGuid],
            CreatedOn = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
        };

        var thingFileName = $"{thingGuid}.thing.json";
        var thingFilePath = Path.Combine(ThingDirectoryPath, thingFileName);

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
            var indexFilePath = Path.Combine(ThingDirectoryPath, NameIndexFileName);
            await IndexManager.AddAsync(indexFilePath, thingName, thingFileName, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(schemaGuid))
            await AssociateWithSchemaInternal(thing, schemaGuid, cancellationToken);

        // Load fresh to handle any schema defaults/calculated fields
        return await LoadAsync(thingGuid, cancellationToken);
    }

    /// <summary>
    /// Associates a <see cref="Thing"/> with a <see cref="Schema"/>.
    /// </summary>
    /// <param name="thingGuid">Unique identifier of the <see cref="Thing"/> to associate with the <see cref="Schema"/> specified by <paramref name="schemaGuid"/>.</param>
    /// <param name="schemaGuid">Unique identiifer of the <see cref="Schema"/> to which the <see cref="Thing"/> specified by <paramref name="thingGuid"/> shall be associated.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task returning a <see cref="bool"/> indicating whether the operation was successful and an updated <see cref="Thing"/> loaded from the data store after the modification was made, if successful.</returns>
    public override async Task<(bool, Thing?)> AssociateWithSchemaAsync(string thingGuid, string schemaGuid, CancellationToken cancellationToken)
    {
        var thing = await LoadAsync(thingGuid, cancellationToken);
        if (thing == null)
            return (false, null);

        return await AssociateWithSchemaInternal(thing, schemaGuid, cancellationToken);
    }

    private async Task<(bool, Thing?)> AssociateWithSchemaInternal(Thing thing, string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(thing);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        var thingFileName = $"{thing.Guid}.thing.json";
        var thingFilePath = Path.Combine(ThingDirectoryPath, thingFileName);

        if (!File.Exists(thingFilePath))
            return (false, null); // Thing doesn't exist...

        if (!thing.SchemaGuids.Contains(schemaGuid))
        {
            thing.SchemaGuids.Add(schemaGuid);
            var (saved, _) = await thing.SaveAsync(cancellationToken);
            if (!saved)
                return (false, null);
        }

        // Add to schema name index, if applicable
        {
            var indexFilePath = Path.Combine(ThingDirectoryPath, $"_thing.names.schema.{schemaGuid}.csv");
            await IndexManager.AddAsync(indexFilePath, thing.Name, thingFileName, cancellationToken);
        }

        // Add it to the schema index
        {
            var indexFilePath = Path.Combine(ThingDirectoryPath, $"_thing.schema.{schemaGuid}.csv");
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
        if (thingDir == null || !thingDir.Exists)
            return false;

        Dictionary<string, Dictionary<string, string>> indexesToWrite = [];
        Dictionary<string, Dictionary<string, string>> schemaGuidsAndThingIndexes = [];
        Dictionary<string, Dictionary<string, string>> schemaGuidsAndThingNames = [];
        Dictionary<string, SchemaIncrementField> schemaGuidsAndIncrementFields = [];
        Dictionary<string, Dictionary<Reference, (long existingId, DateTime createdOn, string path)>> schemaGuidsAndThingIncrementMetadata = [];

        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (ssp != null)
            await foreach (var schema in ssp.LoadAll(cancellationToken))
            {
                schemaGuidsAndThingIndexes.TryAdd(schema.Guid, []);
                schemaGuidsAndThingNames.TryAdd(schema.Guid, []);

                // Does this schema have an increment field?
                var incProperty = schema.Properties
                    .OrderBy(p => p.Key)
                    .Where(p => p.Value is SchemaIncrementField)
                    .Select(p => p.Value)
                    .Cast<SchemaIncrementField>()
                    .FirstOrDefault();
                if (incProperty != default)
                {
                    schemaGuidsAndIncrementFields.TryAdd(schema.Guid, incProperty);
                }
            }

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
                        if (!string.IsNullOrWhiteSpace(schemaGuid)
                            && schemaGuidsAndThingIndexes.TryGetValue(schemaGuid, out Dictionary<string, string>? value))
                            value?.Add(thing.Guid, thingFileName.Name);

                if (thing.SchemaGuids != null)
                    foreach (var schemaGuid in thing.SchemaGuids)
                        if (!string.IsNullOrWhiteSpace(schemaGuid)
                    && schemaGuidsAndThingNames.TryGetValue(schemaGuid, out Dictionary<string, string>? value2))
                            value2?.Add(thing.Name, thingFileName.Name);
            }
            else
            {
                AmbientErrorContext.Provider.LogError($"Unable to index named thing '{thing.Name}' in '{thingFileName.Name}'.");
            }

            // INCREMENT index
            foreach (var schemaGuid in thing.SchemaGuids ?? [])
            {
                // Does this schema have an increment field?
                if (schemaGuidsAndIncrementFields.TryGetValue(schemaGuid, out SchemaIncrementField? incrementProperty))
                {
                    // Yes, so do we already have a metadata list?
                    if (!schemaGuidsAndThingIncrementMetadata.TryGetValue(schemaGuid, out Dictionary<Reference, (long existingId, DateTime createdOn, string path)>? metadata))
                    {
                        // No, create one and add it.
                        metadata = [];
                        schemaGuidsAndThingIncrementMetadata.TryAdd(schemaGuid, metadata);
                    }

                    long existingId = 0;
                    if (thing.Properties.TryGetValue(incrementProperty.Name, out object? eid)
                        && long.TryParse(eid.ToString(), out long eidLong))
                    {
                        existingId = eidLong;
                    }

                    metadata.Add(thing, (existingId, thing.CreatedOn, thingFileName.Name));
                }
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

        // Write INCREMENT index
        foreach (var (schemaGuid, incrementField) in schemaGuidsAndIncrementFields)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (!schemaGuidsAndThingIncrementMetadata.TryGetValue(schemaGuid, out Dictionary<Reference, (long existingId, DateTime createdOn, string path)>? metadata))
            {
                AmbientErrorContext.Provider.LogWarning($"Consistency issue: Could not get increment metadata for schema {schemaGuid} but did find increment field '{incrementField.Name}'.");
                continue;
            }

            var reorderedBase = metadata
                .OrderBy(x => x.Value.existingId)
                .ThenBy(x => x.Value.createdOn)
                .Select((x, i) => new { reference = x.Key, index = i + 1, x.Value.path })
                .ToArray();

            var reorderedIndex = reorderedBase
                .ToDictionary(k => k.index.ToString(), v => v.path);

            indexesToWrite.Add(Path.Combine(thingDir.FullName, $"_thing.inc.schema.{schemaGuid}.csv"), reorderedIndex);

            var reorderedBulk = reorderedBase
                .ToDictionary(k => k.reference, v => new List<(string, object)>() { (incrementField.Name, v.index) });

            var (bulkSuccess, _) = await TryBulkUpdate(reorderedBulk, cancellationToken);
            if (!bulkSuccess)
            {
                AmbientErrorContext.Provider.LogWarning($"Bulk update of increment values on {schemaGuid} failed.");
            }
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

    /// <inheritdoc/>
    public override async Task<bool> DeleteAsync(string thingGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingGuid);

        var thingDir = new DirectoryInfo(ThingDirectoryPath);
        if (thingDir == null || !thingDir.Exists)
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

        // Remove schema name index, if applicable
        if (thing.SchemaGuids != null)
            foreach (var schemaGuid in thing.SchemaGuids)
            {
                if (!string.IsNullOrWhiteSpace(schemaGuid))
                {
                    var indexFilePath = Path.Combine(thingDir.FullName, $"_thing.names.schema.{schemaGuid}.csv");
                    if (File.Exists(indexFilePath))
                    {
                        await IndexManager.RemoveByValueAsync(indexFilePath, thingFileName, cancellationToken);
                        AmbientErrorContext.Provider.LogProgress($"Deleted from name schema index {Path.GetFileName(indexFilePath)}");
                    }
                }
            }

        // If this has a schema, remove it from the schema index
        if (thing.SchemaGuids != null)
            foreach (var schemaGuid in thing.SchemaGuids)
            {
                if (!string.IsNullOrWhiteSpace(schemaGuid))
                {
                    var indexFilePath = Path.Combine(thingDir.FullName, $"_thing.schema.{schemaGuid}.csv");
                    if (File.Exists(indexFilePath))
                    {
                        await IndexManager.RemoveByKeyAsync(indexFilePath, thing.Guid, cancellationToken);
                        AmbientErrorContext.Provider.LogProgress($"Deleted from schema index {Path.GetFileName(indexFilePath)}");
                    }
                }
            }

        return true;
    }
}
