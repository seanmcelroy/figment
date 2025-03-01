
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using jot;
using Spectre.Console;

namespace Figment.Data.Local;

public class LocalDirectorySchemaStorageProvider(string SchemaDirectoryPath) : ISchemaStorageProvider
{
    private const string NameIndexFileName = $"_schema.names.csv";

    private const string PluralIndexFileName = $"_schema.plurals.csv";

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        // Required for $ref properties with type descriminator
        AllowOutOfOrderMetadataProperties = true,
#if DEBUG
        WriteIndented = true,
#endif
    };

    public async Task<CreateSchemaResult> CreateAsync(string schemaName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        var schemaGuid = Guid.NewGuid().ToString();
        var schemaDefinition = new JsonSchemaDefinition(schemaGuid, schemaName, null, null);

        var schemaFileName = $"{schemaGuid}.schema.json";
        var schemaFilePath = Path.Combine(SchemaDirectoryPath, schemaFileName);

        if (File.Exists(schemaFilePath))
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: File for schema {schemaName} already exists at {schemaFilePath}");
            return new CreateSchemaResult { Success = false };
        }

        using var fs = new FileStream(schemaFilePath, FileMode.CreateNew);
        try
        {
            await JsonSerializer.SerializeAsync(fs, schemaDefinition, jsonSerializerOptions, cancellationToken);
            await fs.FlushAsync(cancellationToken);
        }
        catch (Exception)
        {
            File.Delete(schemaFilePath);
            throw;
        }

        // Add to index
        var indexFilePath = Path.Combine(SchemaDirectoryPath, NameIndexFileName);
        var indexAdded = await IndexManager.AddAsync(indexFilePath, schemaName, schemaFileName, cancellationToken);
        if (!indexAdded)
            AnsiConsole.MarkupLineInterpolated($"[yellow]WARNING[/]: Unable to update index at: {indexFilePath}");

        return new CreateSchemaResult { Success = true, NewGuid = schemaGuid };
    }

    /// <summary>
    /// Gets all schemas
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to abort the enumerator</param>
    /// <returns>Each schema</returns>
    /// <remarks>This may be a very expensive operation</remarks>
    public async IAsyncEnumerable<(Reference reference, string? name)> GetAll([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Shortcut - try to get them by name index first.
        var indexFilePath = Path.Combine(SchemaDirectoryPath, NameIndexFileName);
        if (File.Exists(indexFilePath))
        {
            await foreach (var indexMatch in FindByNameAsync(x => true, cancellationToken))
            {
                yield return indexMatch;
            }
            yield break;
        }

        var schemaDir = new DirectoryInfo(SchemaDirectoryPath);
        if (schemaDir == null || !schemaDir.Exists)
            yield break;

        foreach (var file in schemaDir.EnumerateFiles("*.schema.json"
            , new EnumerationOptions
            {
                AttributesToSkip = FileAttributes.Offline,
                IgnoreInaccessible = true,
            })
        )
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var schemaGuidString = file.Name.Split(".schema.json");
            if (!string.IsNullOrWhiteSpace(schemaGuidString[0])
                && Guid.TryParse(schemaGuidString[0], out Guid _))
            {
                var schema = await LoadAsync(schemaGuidString[0], cancellationToken);
                yield return (new Reference
                {
                    Guid = schemaGuidString[0],
                    Type = Reference.ReferenceType.Schema
                }, schema?.Name);
            }
        }
    }

    public async Task<Schema?> LoadAsync(string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        var fileName = $"{schemaGuid}.schema.json";
        var filePath = Path.Combine(SchemaDirectoryPath, fileName);
        return await LoadFileAsync(filePath, cancellationToken);
    }

    private static async Task<Schema?> LoadFileAsync(string filePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema. No file found at {filePath}");
            return null;
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema. Empty schema file found at {filePath}");
            fileInfo.Delete();
            return null;
        }

        using var fs = new FileStream(filePath, FileMode.Open);
        try
        {
            var schemaDefinition = await JsonSerializer.DeserializeAsync<JsonSchemaDefinition>(fs, jsonSerializerOptions, cancellationToken);
            if (schemaDefinition == null)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to deserialize schema from {filePath}");
                return null;
            }
            return schemaDefinition.ToSchema();
        }
        catch (JsonException je)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to deserialize schema from {filePath}");
            AnsiConsole.WriteException(je);
            return null;
        }
    }

    public async Task<bool> SaveAsync(Schema schema, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var fileName = $"{schema.Guid}.schema.json";
        var filePath = Path.Combine(SchemaDirectoryPath, fileName);

        // Convert schema to definition file for serialization in JSON Schema format
        var schemaDefinition = new JsonSchemaDefinition(schema);

        using var fs = File.Create(filePath);
        try
        {
            await JsonSerializer.SerializeAsync(fs, schemaDefinition, jsonSerializerOptions, cancellationToken);
            await fs.FlushAsync(cancellationToken);
            return true;
        }
        catch (Exception je)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to serialize schema {schema.Guid} from {filePath}");
            AnsiConsole.WriteException(je);
            return false;
        }
    }

    public async Task<bool> RebuildIndexes(CancellationToken cancellationToken)
    {
        var schemaDir = new DirectoryInfo(SchemaDirectoryPath);
        if (schemaDir == null || !schemaDir.Exists)
            return false;

        Dictionary<string, string> namesIndex = [];
        Dictionary<string, string> pluralsIndex = [];
        foreach (var schemaFileName in schemaDir.GetFiles("*.schema.json"))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var schema = await LoadFileAsync(schemaFileName.FullName, cancellationToken);
            if (schema == null)
                continue;
            namesIndex.Add(schema.Name, schemaFileName.Name);
            if (!string.IsNullOrWhiteSpace(schema.Plural))
                pluralsIndex.Add(schema.Plural, schemaFileName.Name);
        }

        Dictionary<string, Dictionary<string, string>> indexesToWrite = new() {
            {Path.Combine(schemaDir.FullName, NameIndexFileName), namesIndex},
            {Path.Combine(schemaDir.FullName, PluralIndexFileName), pluralsIndex},
        };

        foreach (var index in indexesToWrite)
        {
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

    public async Task<Reference> FindByNameAsync(string schemaName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        // Add to index
        var indexFilePath = Path.Combine(SchemaDirectoryPath, NameIndexFileName);
        if (!File.Exists(indexFilePath))
            return Reference.EMPTY;

        await foreach (var entry in IndexManager.LookupAsync(
            indexFilePath
            , e => string.Compare(e.Key, schemaName, StringComparison.CurrentCultureIgnoreCase) == 0
            , cancellationToken))
        {
            var schemaFileName = entry.Value;
            var schemaGuid = schemaFileName.Split('.')[0];
            return new Reference
            {
                Guid = schemaGuid,
                Type = Reference.ReferenceType.Schema
            };
        }

        return Reference.EMPTY;
    }

    private async IAsyncEnumerable<(Reference reference, string name)> FindByNameAsync(Func<string, bool> selector, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);

        // Add to index
        var indexFilePath = Path.Combine(SchemaDirectoryPath, NameIndexFileName);
        if (!File.Exists(indexFilePath))
            yield break;

        await foreach (var entry in IndexManager.LookupAsync(
            indexFilePath
            , e => selector(e.Key)
            , cancellationToken))
        {
            var schemaFileName = entry.Value;
            var schemaGuid = schemaFileName.Split('.')[0];
            var schema = await LoadAsync(schemaGuid, cancellationToken);
            if (schema != null)
                yield return (new Reference
                {
                    Guid = schemaGuid,
                    Type = Reference.ReferenceType.Schema
                }, schema.Name);
        }

        yield break;
    }

    public async IAsyncEnumerable<Reference> FindByPartialNameAsync(string thingNamePart, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingNamePart);

        // Load index
        var indexFilePath = Path.Combine(SchemaDirectoryPath, NameIndexFileName);
        if (!File.Exists(indexFilePath))
            yield break; // Happens on new install if no items, nothing in index, and so no file

        await foreach (var guid in IndexManager.ResolveGuidFromPartialNameAsync(indexFilePath, thingNamePart, cancellationToken))
        {
            yield return new Reference
            {
                Type = Reference.ReferenceType.Schema,
                Guid = guid
            };
        }
    }

    public Task<bool> GuidExists(string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        var fileName = $"{schemaGuid}.schema.json";
        var filePath = Path.Combine(SchemaDirectoryPath, fileName);
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
                AnsiConsole.MarkupLineInterpolated($"[yellow]WARN[/]: Zero length file found but could not be deleted at: {filePath}");
                AnsiConsole.WriteException(ex);
            }
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public async IAsyncEnumerable<Reference> FindByPluralNameAsync(string plural, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plural);

        // Load index
        var indexFilePath = Path.Combine(SchemaDirectoryPath, PluralIndexFileName);
        if (!File.Exists(indexFilePath))
            yield break; // Happens on new install if no items, nothing in index, and so no file

        await foreach (var guid in LocalDirectoryStorageProvider.ResolveGuidFromExactNameAsync(indexFilePath, plural, cancellationToken))
        {
            yield return new Reference
            {
                Type = Reference.ReferenceType.Schema,
                Guid = guid
            };
        }
    }
}
