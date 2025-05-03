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
/// A set of field definitions a thing can optionally implement.
/// </summary>
/// <param name="Guid">The immutable unique identifier of the schema.</param>
/// <param name="Name">A display name for the schema.</param>
/// <remarks>This class is not directly serialized to JSON, that is done by <see cref="SchemaDefinition"/>.</remarks>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class Schema(string Guid, string Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// Gets the globally unique identifier for the schema.
    /// </summary>
    public string Guid { get; init; } = Guid;

    /// <summary>
    /// Gets or sets the name of the schema.
    /// </summary>
    public string Name { get; set; } = Name;

    /// <summary>
    /// Gets a version of <see cref="Name"/> that is encased in brackets if the value contains spaces.
    /// </summary>
    /// <seealso cref="Thing.CarvePropertyName(string, Schema?)"/>
    public string EscapedName => Name.Contains(' ') && !Name.StartsWith('[') && !Name.EndsWith(']') ? $"[{Name}]" : Name;

    /// <summary>
    /// Gets or sets the plural form of <see cref="Name"/>, which is used by some command interfaces that modify entities.
    /// </summary>
    public string? Plural { get; set; }

    /// <summary>
    /// Gets or sets the description of the schema.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets the list of fields, keyed by name, defined for this schema.
    /// </summary>
    /// <remarks>
    /// Do not use this outside of this class.  Left public for serialization only.
    /// </remarks>
    public Dictionary<string, SchemaFieldBase> Properties { get; init; } = [];

    /// <summary>
    /// Gets or sets the versioning plan for this schema, if the schema is versioned.
    /// </summary>
    public string? VersionGuid { get; set; }

    /// <summary>
    /// Gets the list of import maps that define how to import external records into new things of this schema type.
    /// </summary>
    public List<SchemaImportMap> ImportMaps { get; init; } = [];

    /// <summary>
    /// Gets the date the schema was created.
    /// </summary>
    public DateTime CreatedOn { get; init; }

    /// <summary>
    /// Gets or sets the date the schema was last modified.
    /// </summary>
    /// <remarks>Use <see cref="MarkModified"/> to programmatically update this value.</remarks>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the date the schema was last accessed.
    /// </summary>
    /// <remarks>Use <see cref="MarkAccessed"/> to programmatically update this value.</remarks>
    public DateTime LastAccessed { get; set; }

    /// <summary>
    /// Creates a new <see cref="Schema"/>.
    /// </summary>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newly created schema, if the operation was successful.</returns>
    public static async Task<Schema?> Create(string schemaName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
        {
            return null;
        }

        var result = await provider.CreateAsync(schemaName, cancellationToken);
        if (!result.Success || result.NewGuid == null)
        {
            return null;
        }

        return await provider.LoadAsync(result.NewGuid, cancellationToken);
    }

    /// <summary>
    /// Attempts to delete this schema from the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True, if the operation was successful.  Otherwise, false.</returns>
    public async Task<bool> DeleteAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
        {
            return false;
        }

        var success = await provider.DeleteAsync(Guid, cancellationToken);
        MarkModified();
        return success;
    }

    /// <summary>
    /// Attempts to save this schema to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the save attempt was successful.</returns>
    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (provider == null)
        {
            return false;
        }

        var saved = await provider.SaveAsync(this, cancellationToken);
        MarkModified();
        return saved;
    }

    /// <summary>
    /// Creates a new <see cref="SchemaTextField"/> and adds it as a field of this schema.
    /// </summary>
    /// <param name="name">The name of the new text field.</param>
    /// <param name="minLength">The minimum length of text values for this field, if any.</param>
    /// <param name="maxLength">The maximum length of text values for this field, if any.</param>
    /// <param name="pattern">The regular expression that text values for this field must match, if any.</param>
    /// <returns>The new text field.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="name"/> is null or if a field of this name already exists on the schema.</exception>
    public SchemaTextField AddTextField(string name, ushort? minLength = null, ushort? maxLength = null, string? pattern = null)
    {
        MarkAccessed();

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (minLength.HasValue && maxLength.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(minLength.Value, maxLength.Value, nameof(minLength));
        }

        if (Properties.ContainsKey(name))
        {
            throw new ArgumentException($"A field named '{name}' already exists on this schema", nameof(name));
        }

        var stf = new SchemaTextField(name)
        {
            MinLength = minLength,
            MaxLength = maxLength,
            Pattern = pattern,
        };
        Properties.Add(name, stf);
        return stf;
    }

    /// <summary>
    /// Attempts to find a schema by <paramref name="guidOrNamePart"/>.
    /// </summary>
    /// <param name="guidOrNamePart">The <see cref="Guid"/> or <see cref="Name"/> of schemas to match and return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerator for each <see cref="Schema"/>.</returns>
    public static async IAsyncEnumerable<PossibleNameMatch> ResolveAsync(
        string guidOrNamePart,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var ssp = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (ssp == null)
        {
            yield break;
        }

        // Shortcut - See if it's a guid first of all.
        if (await ssp.GuidExists(guidOrNamePart, cancellationToken))
        {
            var schema = await ssp.LoadAsync(guidOrNamePart, cancellationToken);
            if (schema != null)
            {
                yield return new PossibleNameMatch
                {
                    Name = schema!.Name,
                    Reference = new Reference
                    {
                        Guid = guidOrNamePart,
                        Type = Reference.ReferenceType.Schema,
                    },
                };
            }

            yield break;
        }

        // Nope, so name searching...
        await foreach (var possible in ssp.FindByPartialNameAsync(guidOrNamePart, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return possible;
        }
    }

    /// <summary>
    /// Marks the schema as changed, updating both the <see cref="LastModified"/> and <see cref="LastAccessed"/> dates.
    /// </summary>
    public void MarkModified()
    {
        LastModified = DateTime.UtcNow;
        LastAccessed = LastModified;
    }

    /// <summary>
    /// Marks the schema as accessed, updating the <see cref="LastAccessed"/> date.
    /// </summary>
    public void MarkAccessed() => LastAccessed = DateTime.UtcNow;

    /// <summary>
    /// Returns the <see cref="Name"/> of this schema.
    /// </summary>
    /// <returns>The <see cref="Name"/> of this schema.</returns>
    public override string ToString() => Name;
}