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

using System.Diagnostics.CodeAnalysis;

namespace Figment.Common;

/// <summary>
/// A property of a <see cref="Thing"/> that is actually set but is defined in a <see cref="Schema"/> schema associated
/// with it.
/// </summary>
/// <seealso cref="ThingUnsetProperty"/>
public readonly record struct ThingProperty
{
    /// <summary>
    /// Gets the property name serialized in a JSON file on a Thing.
    /// </summary>
    /// <remarks>
    /// Such as <c>b0c1592e-5d79-4fe4-8814-aa6e534d2b7f.email</c>.
    /// </remarks>
    required public readonly string TruePropertyName { get; init; }

    /// <summary>
    /// Gets the property name rendered for display with any accompanying Schema.
    /// </summary>
    /// <remarks>
    /// Such as <c>Person.email</c>.
    /// </remarks>
    required public readonly string FullDisplayName { get; init; }

    /// <summary>
    /// Gets the property name rendered for display.
    /// </summary>
    /// <remarks>
    /// Such as <c>email</c>.
    /// </remarks>
    required public readonly string SimpleDisplayName { get; init; }

    /// <summary>
    /// Gets the unique identifier of the Schema associated with this property.
    /// </summary>
    required public readonly string? SchemaGuid { get; init; }

    /// <summary>
    /// Gets the value of the property.
    /// </summary>
    required public readonly object? Value { get; init; }

    /// <summary>
    /// Gets a value indicating whether the value of this property is valid, as
    /// according to the associated <see cref="Schema"/>. If no <see cref="Schema"/>
    /// is associated, this is always true.
    /// </summary>
    required public readonly bool Valid { get; init; }

    /// <summary>
    /// Gets a value indicating whether this property is required by its associated schema.
    /// </summary>
    /// <remarks>
    /// This is a derived property if the field is provided by an associated schema.
    /// </remarks>
    required public readonly bool Required { get; init; }

    /// <summary>
    /// Gets a value indicating the type of the field as specified by its associated schema.
    /// </summary>
    /// <remarks>
    /// This is a derived property if the field is provided by an associated schema.
    /// </remarks>
    required public readonly string? SchemaFieldType { get; init; }

    /// <summary>
    /// Gets a value indicating the name of the associated schema, if applicable.
    /// </summary>
    /// <remarks>
    /// This is a derived property if the field is provided by an associated schema.
    /// </remarks>
    required public readonly string? SchemaName { get; init; }

    /// <summary>
    /// Determines whether a property name is considered valid when specified by a user.
    /// </summary>
    /// <param name="propertyName">The proposed property name to analyze.</param>
    /// <param name="message">A validation error message that can be displayed to the end user why the name is invalid.</param>
    /// <returns>A value indicating whether the property is valid when specified by a user.</returns>
    public static bool IsPropertyNameValid(string? propertyName, [NotNullWhen(false)] out string? message)
    {
        // Cannot be null or empty.
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            message = "Property name cannot be null or empty.";
            return false;
        }

        // Cannot start with digit.
        if (char.IsDigit(propertyName, 0))
        {
            message = "Property name cannot start with a digit.";
            return false;
        }

        // Cannot start with a symbol.
        if (char.IsSymbol(propertyName, 0))
        {
            message = "Property name cannot start with a symbol.";
            return false;
        }

        // Cannot contain a space.
        if (propertyName.StartsWith(' ') || propertyName.EndsWith(' ') || propertyName.Contains(' '))
        {
            message = "Property name cannot contain a space.";
            return false;
        }

        // Cannot be a system property
        if (propertyName.Equals("createdon", StringComparison.InvariantCultureIgnoreCase)
            || propertyName.Equals("lastaccessed", StringComparison.InvariantCultureIgnoreCase)
            || propertyName.Equals("lastmodified", StringComparison.InvariantCultureIgnoreCase)
            )
        {
            message = "Property name cannot be a reserved word.";
            return false;
        }

        message = null;
        return true;
    }

    /// <summary>
    /// Returns the <see cref="Value"/> as a <see cref="ulong"/>.
    /// </summary>
    /// <returns>The value of the property, if it is a <see cref="ulong"/>.</returns>
    public ulong? AsUInt64()
    {
        if (Value is ulong ul)
        {
            return ul;
        }

        return null;
    }

    /// <summary>
    /// Returns the <see cref="Value"/> as a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <returns>The value of the property, if it is a <see cref="DateTimeOffset"/>.</returns>
    public DateTimeOffset? AsDateTimeOffset()
    {
        if (Value is DateTimeOffset dto)
        {
            return dto;
        }

        if (Value is string s && SchemaDateField.TryParseDate(s, out dto))
        {
            return dto;
        }

        return null;
    }

    /// <summary>
    /// Returns the <see cref="Value"/> as a <see cref="bool"/>.
    /// </summary>
    /// <returns>The value of the property, if it is a <see cref="bool"/>.</returns>
    public bool? AsBoolean()
    {
        if (Value is bool b)
        {
            return b;
        }

        if (Value is string s && SchemaBooleanField.TryParseBoolean(s, out b))
        {
            return b;
        }

        return null;
    }
}