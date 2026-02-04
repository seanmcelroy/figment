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

using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Figment.Common.Data;

namespace Figment.Common;

/// <summary>
/// A set of field definitions a thing can optionally implement.
/// </summary>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class Schema
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Schema"/> class.
    /// </summary>
    /// <param name="guid">Globally unique identifier for the schema.</param>
    /// <param name="name">Name of the schema.</param>
    public Schema(string guid, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guid, nameof(guid));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (!IsSchemaNameValid(name))
        {
            throw new ArgumentException($"Name '{name}' is not valid for schemas.", nameof(name));
        }

        Guid = guid;
        Name = name;
    }

    /// <summary>
    /// Gets the globally unique identifier for the schema.
    /// </summary>
    public string Guid { get; init; }

    /// <summary>
    /// Gets or sets the name of the schema.
    /// </summary>
    public string Name { get; set; }

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
    /// Gets or sets the list of fields, keyed by name, defined for this schema.
    /// </summary>
    /// <remarks>
    /// After loading, this is a <see cref="FrozenDictionary{TKey, TValue}"/> for optimized lookups.
    /// Use <see cref="SetProperty"/> and <see cref="RemoveProperty"/> for mutations.
    /// </remarks>
    public IReadOnlyDictionary<string, SchemaFieldBase> Properties { get; set; } = new Dictionary<string, SchemaFieldBase>();

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

        var provider = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
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
        var provider = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
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
    public async Task<(bool success, string? message)> SaveAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (provider == null)
        {
            return (false, AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
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
            Name = name,
            MinLength = minLength,
            MaxLength = maxLength,
            Pattern = pattern,
        };
        SetProperty(name, stf);
        return stf;
    }

    /// <summary>
    /// Creates a new <see cref="SchemaDateField"/> and adds it as a field of this schema.
    /// </summary>
    /// <param name="name">The name of the new date field.</param>
    /// <returns>The new date field.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="name"/> is null or if a field of this name already exists on the schema.</exception>
    public SchemaDateField AddDateField(string name)
    {
        MarkAccessed();

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (Properties.ContainsKey(name))
        {
            throw new ArgumentException($"A field named '{name}' already exists on this schema", nameof(name));
        }

        var sdf = new SchemaDateField(name);
        SetProperty(name, sdf);
        return sdf;
    }

    /// <summary>
    /// Creates a new <see cref="SchemaIncrementField"/> and adds it as a field of this schema.
    /// </summary>
    /// <param name="name">The name of the new increment field.</param>
    /// <returns>The new increment field.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="name"/> is null or if a field of this name already exists on the schema.</exception>
    public SchemaIncrementField AddIncrementField(string name)
    {
        MarkAccessed();

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (Properties.ContainsKey(name))
        {
            throw new ArgumentException($"A field named '{name}' already exists on this schema", nameof(name));
        }

        var sif = new SchemaIncrementField(name);
        SetProperty(name, sif);
        return sif;
    }

    /// <summary>
    /// Creates a new <see cref="SchemaMonthDayField"/> and adds it as a field of this schema.
    /// </summary>
    /// <param name="name">The name of the new month+day field.</param>
    /// <returns>The new month+day field.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="name"/> is null or if a field of this name already exists on the schema.</exception>
    public SchemaMonthDayField AddMonthDayField(string name)
    {
        MarkAccessed();

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (Properties.ContainsKey(name))
        {
            throw new ArgumentException($"A field named '{name}' already exists on this schema", nameof(name));
        }

        var smdf = new SchemaMonthDayField(name);
        SetProperty(name, smdf);
        return smdf;
    }

    /// <summary>
    /// Adds or replaces a property field on this schema.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="field">The schema field definition.</param>
    public void SetProperty(string name, SchemaFieldBase field)
    {
        var mutable = Properties as Dictionary<string, SchemaFieldBase>
            ?? new Dictionary<string, SchemaFieldBase>(Properties, StringComparer.Ordinal);
        mutable[name] = field;
        Properties = mutable;
    }

    /// <summary>
    /// Removes a property field from this schema.
    /// </summary>
    /// <param name="name">The name of the property to remove.</param>
    /// <returns>True if the property was found and removed; otherwise, false.</returns>
    public bool RemoveProperty(string name)
    {
        var mutable = Properties as Dictionary<string, SchemaFieldBase>
            ?? new Dictionary<string, SchemaFieldBase>(Properties, StringComparer.Ordinal);
        var removed = mutable.Remove(name);
        Properties = mutable;
        return removed;
    }

    /// <summary>
    /// Freezes the <see cref="Properties"/> dictionary for optimized read performance.
    /// </summary>
    public void FreezeProperties()
    {
        if (Properties is not FrozenDictionary<string, SchemaFieldBase>)
        {
            Properties = Properties.ToFrozenDictionary(StringComparer.Ordinal);
        }
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
        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
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

    /// <summary>
    /// Determines whether a <see cref="Name"/> is considered valid when specified by a user.
    /// </summary>
    /// <param name="schemaName">The proposed schema name to analyze.</param>
    /// <returns>A value indicating whether the schema name is valid when specified by a user.</returns>
    public static bool IsSchemaNameValid(string schemaName)
    {
        // Cannot be null or empty.
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            return false;
        }

        // Cannot start with digit.
        if (char.IsDigit(schemaName, 0))
        {
            return false;
        }

        // Cannot start with a symbol.
        if (char.IsSymbol(schemaName, 0))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Retrieves the 'increment' field on this schema, if it has one.
    /// </summary>
    /// <returns>The incrementing field on this schema, if one is defined on it.</returns>
    public SchemaIncrementField? GetIncrementField() => Properties
        .OrderBy(p => p.Key)
        .Select(p => p.Value)
        .OfType<SchemaIncrementField>()
        .FirstOrDefault();
}