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

namespace jot.Commands.Tasks;

/// <summary>
/// Represents a type that can be assigned null, or may not be assigned at all.
/// </summary>
/// <typeparam name="T">The underlying value type of the <see cref="Nullable{T}"/> generic type.</typeparam>
public readonly record struct NullableOrMissing<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullableOrMissing{T}"/> class.
    /// </summary>
    public NullableOrMissing()
    {
        Specified = false;
        Value = default;
    }

    /// <summary>
    /// Gets a value indicating whether any value was explicitly specified at all.
    /// </summary>
    required public readonly bool Specified { get; init; } = false;

    /// <summary>
    /// Gets the value of the current <see cref="Nullable{T}"/> object if it has been assigned a valid underlying value.
    /// If this value is null, that does not mean it was specified as null.  That is true only if <see cref="Specified"/> is also <c>true</c>.
    /// </summary>
    required public readonly T? Value { get; init; } = default;

    /// <summary>
    /// Creates the value as specified with the given value.
    /// </summary>
    /// <typeparam name="TStruct">The type of the value, same as <typeparamref name="T"/>.</typeparam>
    /// <param name="value">A value type.</param>
    /// <returns>The newly constructed object.</returns>
    public static NullableOrMissing<T> CreateWithStruct<TStruct>(TStruct value)
        where TStruct : struct, T
    {
        return new NullableOrMissing<T>()
        {
            Specified = true,
            Value = value,
        };
    }

    /// <summary>
    /// Creates the value as specified with the given value.
    /// </summary>
    /// <typeparam name="TStruct">The type of the value, same as <typeparamref name="T"/>.</typeparam>
    /// <param name="value">A value type.</param>
    /// <returns>The newly constructed object.</returns>
    public static NullableOrMissing<T> CreateWithStruct<TStruct>(TStruct? value)
        where TStruct : struct, T
    {
        return new NullableOrMissing<T>()
        {
            Specified = true,
            Value = value ?? default(T),
        };
    }

    /// <summary>
    /// Creates the value as specified with the given value.
    /// </summary>
    /// <typeparam name="TClass">The type of the value, same as <typeparamref name="T"/>.</typeparam>
    /// <param name="value">A value type.</param>
    /// <returns>The newly constructed object.</returns>
    public static NullableOrMissing<T> CreateWithClass<TClass>(TClass? value)
        where TClass : class, T
    {
        return new NullableOrMissing<T>()
        {
            Specified = true,
            Value = value,
        };
    }
}
