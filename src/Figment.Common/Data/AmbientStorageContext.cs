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

namespace Figment.Common.Data;

/// <summary>
/// A thread-safe and async-safe reference to the current <see cref="IStorageProvider"/>.
/// </summary>
public static class AmbientStorageContext
{
    /// <summary>
    /// A text resource for displaying an error to the user when the <see cref="ISchemaStorageProvider"/> cannot be loaded from the <see cref="IStorageProvider"/>.
    /// </summary>
    public const string RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER = "Unable to load schema storage provider.";

    /// <summary>
    /// A text resource for displaying an error to the user when the <see cref="IThingStorageProvider"/> cannot be loaded from the <see cref="IStorageProvider"/>.
    /// </summary>
    public const string RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER = "Unable to load thing storage provider.";

    /// <summary>
    /// A text resource for displaying an error to the user when the <see cref="ISchemaStorageProvider"/> cannot find a well-known, built-in schema.
    /// </summary>
    public const string RESOURCE_ERR_UNABLE_TO_LOAD_BUILT_IN_SCHEMA = "Built-in schema cannot be loaded.";

    private static readonly AsyncLocal<IStorageProvider> _StorageProvider = new();

    /// <summary>
    /// Gets or sets the ambient <see cref="IStorageProvider"/> instance that clients can use to interact with entities.
    /// </summary>
    public static IStorageProvider? StorageProvider
    {
        get => _StorageProvider.Value;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _StorageProvider.Value = value;
        }
    }
}