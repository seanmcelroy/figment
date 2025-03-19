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

namespace Figment.Common.Calculations;

/// <summary>
/// The type of value stored in a <see cref="CalculationResult"/>.
/// </summary>
public enum CalculationResultType
{
    /// <summary>
    /// The value represents an error.
    /// </summary>
    Error = 0,

    /// <summary>
    /// The value represents a static value either input directly into a
    /// formula definition.
    /// </summary>
    StaticValue = 1,

    /// <summary>
    /// The value represents an indirect value dynamically dispatched
    /// to a property on a <see cref="Thing"/>.
    /// </summary>
    PropertyValue = 2,

    /// <summary>
    /// The value represents a static value from another inner calculation
    /// returned from a <see cref="FunctionBase"/> subclass implementation.
    /// </summary>
    FunctionResult = 3,
}