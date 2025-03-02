using System.Runtime.CompilerServices;
using Figment.Common.Data;

namespace Figment.Common;

/// <summary>
/// A set of field definitions a thing can optionally implement
/// </summary>
/// <param name="Guid">The immutable unique identifier of the schema</param>
/// <param name="Name">A display name for the schema</param>
/// <remarks>This class is not directly serialized to JSON, that is done by <see cref="SchemaDefinition"/>.
public class Schema(string Guid, string Name)
{
    public enum SchemaFieldType
    {
        Text = 0,
        Uri = 1,
        Email = 2,
    }
    public string Guid { get; init; } = Guid;
    public string Name { get; set; } = Name;
    public string EscapedName => Name.Contains(' ') && !Name.StartsWith('[') && !Name.EndsWith(']') ? $"[{Name}]" : Name;
    public string? Plural { get; set; }

    public string? Description { get; set; }

    public Dictionary<string, SchemaFieldBase> Properties { get; init; } = [];

    public static async Task<Schema?> Create(string schemaName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
            return null;

        var result = await provider.CreateAsync(schemaName, cancellationToken);
        if (!result.Success || result.NewGuid == null)
            return null;

        var newSchema = await provider.LoadAsync(result.NewGuid, cancellationToken);
        return newSchema;
    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
            return false;
    
        var success = await provider.SaveAsync(this, cancellationToken);
        return success;
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

    public static async IAsyncEnumerable<Reference> ResolveAsync(
        string guidOrNamePart,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
            yield break;;

        // Shortcut - See if it's a guid first of all.
        if (await provider.GuidExists(guidOrNamePart, cancellationToken))
        {
            yield return new Reference
            {
                Guid = guidOrNamePart,
                Type = Reference.ReferenceType.Schema
            };
            yield break;
        }

        // Nope, so name searching...
        await foreach (var possible in provider.FindByPartialNameAsync(guidOrNamePart, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            yield return possible;
        }
    }

    public override string ToString() => Name;
}