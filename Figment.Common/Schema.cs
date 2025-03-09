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

    //[Obsolete("Do not use outside of Schema")]
    public Dictionary<string, SchemaFieldBase> Properties { get; init; } = [];

    public List<SchemaImportMap> ImportMaps { get; init; } = [];

    public DateTime CreatedOn { get; init; }
    public DateTime LastModified { get; set; }
    public DateTime LastAccessed { get; set; }


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

    public async Task<bool> DeleteAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
            return false;

        var success = await provider.DeleteAsync(Guid, cancellationToken);
        MarkModified();
        return success;
    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
            return false;

        var success = await provider.SaveAsync(this, cancellationToken);
        MarkModified();
        return success;
    }

    public SchemaTextField AddTextField(string name, ushort? minLength = null, ushort? maxLength = null, string? pattern = null)
    {
        MarkAccessed();

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
            yield break; ;

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

    public void MarkModified()
    {
        LastModified = DateTime.UtcNow;
        LastAccessed = LastModified;
    }
    public void MarkAccessed() => LastAccessed = DateTime.UtcNow;

    public override string ToString() => Name;
}