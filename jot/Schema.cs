using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using jot;
using Spectre.Console;

namespace Figment;

/// <summary>
/// A set of field definitions a thing can optionally implement
/// </summary>
/// <param name="Guid">The immutable unique identifier of the schema</param>
/// <param name="Name">A display name for the schema</param>
/// <remarks>This class is not directly serialized to JSON, that is done by <see cref="SchemaDefinition"/>.
public class Schema(string Guid, string Name)
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        // Required for $ref properties with type descriminator
        AllowOutOfOrderMetadataProperties = true,
#if DEBUG
        WriteIndented = true,
#endif
    };

    public enum SchemaFieldType
    {
        Text = 0,
        Uri = 1,
        Email = 2,
    }
    private const string NameIndexFileName = $"_schema.names.csv";

    private const string PluralIndexFileName = $"_schema.plurals.csv";

    public string Guid { get; init; } = Guid;
    public string Name { get; set; } = Name;
    public string EscapedName => Name.Contains(' ') && !Name.StartsWith('[') && !Name.EndsWith(']') ? $"[{Name}]" : Name;
    public string? Plural { get; set; }

    public string? Description { get; set; }

    public Dictionary<string, SchemaFieldBase> Properties { get; init; } = [];


    internal Schema(SchemaDefinition schemaDefinition) : this(schemaDefinition.Guid, schemaDefinition.Name)
    {
        // Optional built-ins
        Description = schemaDefinition.Description;
        Plural = schemaDefinition.Plural;

        foreach (var prop in schemaDefinition.Properties)
        {
            var required =
                schemaDefinition.RequiredProperties != null &&
                schemaDefinition.RequiredProperties.Any(sdr => string.CompareOrdinal(sdr, prop.Key) == 0);

            prop.Value.Required = required;

            Properties.Add(prop.Key, prop.Value);
        }
    }

    private static async Task<DirectoryInfo?> GetSchemaDatabaseDirectory()
    {
        var schemaPath = Path.Combine(Globals.DB_PATH, "schemas");

        if (Directory.Exists(schemaPath))
            return new DirectoryInfo(schemaPath);

        await Console.Error.WriteLineAsync("WARN: Schema database directory does not exist.");
        try
        {
            return Directory.CreateDirectory(schemaPath);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"ERR: Cannot create schema database directory at '{schemaPath}': {ex.Message}");
            return null;
        }
    }

    public static string ConvertSchemaNameToFileName(string schemaName)
    {
        var cultureInfo = new CultureInfo("en-US", false);
        var textInfo = cultureInfo.TextInfo;
        var title = textInfo.ToTitleCase(schemaName);
        var combined = title.Replace(" ", "");
        var camelCase = combined.ToLower(cultureInfo)[0] + title[1..];
        return camelCase;
    }

    public static async Task<Schema?> Create(string schemaName, CancellationToken cancellationToken)
    {
        var schemaGuid = System.Guid.NewGuid().ToString();
        var schemaDefinition = new SchemaDefinition(schemaGuid, schemaName, null, null);

        var schemaDir = await GetSchemaDatabaseDirectory();
        if (schemaDir == null)
            return null;

        var schemaFileName = $"{schemaGuid}.schema.json";
        var schemaFilePath = Path.Combine(schemaDir.FullName, schemaFileName);

        if (File.Exists(schemaFilePath))
        {
            await Console.Error.WriteLineAsync($"ERR: File for schema {schemaName} already exists at {schemaFilePath}");
            return null;
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
        var indexFilePath = Path.Combine(schemaDir.FullName, NameIndexFileName);
        await IndexManager.AddAsync(indexFilePath, schemaName, schemaFileName, cancellationToken);

        return await LoadAsync(schemaGuid, cancellationToken);
    }

    public static async IAsyncEnumerable<Schema> GetAll([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var schemaDir = await GetSchemaDatabaseDirectory();
        if (schemaDir == null)
            yield break;

        foreach (var file in schemaDir.GetFiles("*.schema.json"))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var schemaGuidString = file.Name.Split(".schema.json");
            if (!string.IsNullOrWhiteSpace(schemaGuidString[0])
                && System.Guid.TryParse(schemaGuidString[0], out Guid _))
            {
                var schema = await LoadAsync(schemaGuidString[0], cancellationToken);
                if (schema != null)
                    yield return schema;
            }
        }
    }

    public static async Task<Schema?> LoadAsync(string schemaGuid, CancellationToken cancellationToken)
    {
        var schemaDir = await GetSchemaDatabaseDirectory();
        if (schemaDir == null)
            return null;

        var fileName = $"{schemaGuid}.schema.json";
        var filePath = Path.Combine(schemaDir.FullName, fileName);

        return await LoadFileAsync(filePath, cancellationToken);
    }

    private static async Task<Schema?> LoadFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema. No file found at {Markup.Escape(filePath)}");
            return null;
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema. Empty schema file found at {Markup.Escape(filePath)}");
            fileInfo.Delete();
            return null;
        }

        using var fs = new FileStream(filePath, FileMode.Open);
        try
        {
            var schemaDefinition = await JsonSerializer.DeserializeAsync<SchemaDefinition>(fs, jsonSerializerOptions, cancellationToken);
            if (schemaDefinition == null)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to deserialize schema from {Markup.Escape(filePath)}");
                return null;
            }
            return new Schema(schemaDefinition);
        }
        catch (JsonException je)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to deserialize schema from {Markup.Escape(filePath)}");
            AnsiConsole.WriteException(je);
            return null;
        }
    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        var schemaDir = await GetSchemaDatabaseDirectory();
        if (schemaDir == null)
            return false;

        var fileName = $"{Guid}.schema.json";
        var filePath = Path.Combine(schemaDir.FullName, fileName);

        // Convert schema to definition file for serialization in JSON Schema format
        var schemaDefinition = new SchemaDefinition(this);

        using var fs = File.Create(filePath);
        try
        {
            await JsonSerializer.SerializeAsync(fs, schemaDefinition, jsonSerializerOptions, cancellationToken);
            await fs.FlushAsync(cancellationToken);
            return true;
        }
        catch (Exception je)
        {
            await Console.Error.WriteLineAsync($"ERR: Unable to serialize schema {Guid} from {filePath}: {je.Message}");
            return false;
        }
    }

    public static async Task<Schema?> FindAsync(string schemaName, CancellationToken cancellationToken)
    {
        var schemaDir = await GetSchemaDatabaseDirectory();
        if (schemaDir == null)
            return null;

        // Add to index
        var indexFilePath = Path.Combine(schemaDir.FullName, NameIndexFileName);
        if (!File.Exists(indexFilePath))
            return null;

        using var swIndex = new StreamReader(indexFilePath, Encoding.UTF8);

        await foreach (var entry in IndexManager.LookupAsync(
            indexFilePath
            , e => string.Compare(e.Key, schemaName, StringComparison.CurrentCultureIgnoreCase) == 0
            , cancellationToken))
        {
            var schemaFileName = entry.Value;
            var schemaGuid = schemaFileName.Split('.')[0];
            var schema = await LoadAsync(schemaGuid, cancellationToken);
            if (schema != null)
                return schema;
        }

        return null;
    }

    public SchemaTextField AddTextField(string name, ushort? minLength = null, ushort? maxLength = null, string? pattern = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (minLength.HasValue && maxLength.HasValue)
            ArgumentOutOfRangeException.ThrowIfGreaterThan(minLength.Value, maxLength.Value, nameof(minLength));

        if (Properties.ContainsKey(name))
            throw new ArgumentException($"A field named '{name}' already exists on this schema", nameof(name));

        var stf = new SchemaTextField(name)
        {
            MinLength = minLength,
            MaxLength = maxLength,
            Pattern = pattern
        };
        Properties.Add(name, stf);
        return stf;
    }

    public static async Task<bool> GuidExists(string schemaGuid, CancellationToken _)
    {
        if (string.IsNullOrWhiteSpace(schemaGuid))
            return false;

        var schemaDir = await GetSchemaDatabaseDirectory();
        if (schemaDir == null)
            return false;

        var fileName = $"{schemaGuid}.schema.json";
        var filePath = Path.Combine(schemaDir.FullName, fileName);
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

    public static async IAsyncEnumerable<Reference> ResolveAsync(
        string guidOrNamePart,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Shortcut - See if it's a guid first of all.
        if (await GuidExists(guidOrNamePart, cancellationToken))
        {
            yield return new Reference
            {
                Guid = guidOrNamePart,
                Type = Reference.ReferenceType.Schema
            };
            yield break;
        }

        // Nope, so name searching...
        await foreach (var possible in ResolvePartialNameAsync(guidOrNamePart, cancellationToken))
        {
            yield return possible;
        }
    }

    public static async IAsyncEnumerable<Reference> ResolvePartialNameAsync(string thingNamePart, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var schemaDir = await GetSchemaDatabaseDirectory();
        if (schemaDir == null)
            yield break;

        // Load index
        var indexFilePath = Path.Combine(schemaDir.FullName, NameIndexFileName);
        if (!File.Exists(indexFilePath))
            yield break; // Happens on new install if no items, nothing in index, and so no file

        await foreach (var guid in Program.ResolveGuidFromPartialNameAsync(indexFilePath, thingNamePart, cancellationToken))
        {
            yield return new Reference
            {
                Type = Reference.ReferenceType.Schema,
                Guid = guid
            };
        }
    }

    public static async IAsyncEnumerable<Reference> ResolvePluralNameAsync(string plural, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var schemaDir = await GetSchemaDatabaseDirectory();
        if (schemaDir == null)
            yield break;

        // Load index
        var indexFilePath = Path.Combine(schemaDir.FullName, PluralIndexFileName);
        if (!File.Exists(indexFilePath))
            yield break; // Happens on new install if no items, nothing in index, and so no file

        await foreach (var guid in Program.ResolveGuidFromExactNameAsync(indexFilePath, plural, cancellationToken))
        {
            yield return new Reference
            {
                Type = Reference.ReferenceType.Schema,
                Guid = guid
            };
        }
    }

    public static async Task<bool> RebuildIndexes(CancellationToken cancellationToken)
    {
        var schemaDir = await GetSchemaDatabaseDirectory();
        if (schemaDir == null)
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

    public override string ToString() => Name;
}