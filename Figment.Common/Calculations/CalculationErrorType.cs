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
/// Reasons that a <see cref="Parsing.ExpressionResult"/> terminated.
/// </summary>
public enum CalculationErrorType
{
    /// <summary>
    /// The calculation was successful; there was no error.
    /// </summary>
    Success = 0,

    /// <summary>
    /// The formula could not be parsed.
    /// </summary>
    /// <remarks>#ERR</remarks>
    FormulaParse = 1,

    /// <summary>
    /// The calculation resulted in a NaN result.
    /// </summary>
    /// <remarks>#NAN</remarks>
    NotANumber = 2,

    /// <summary>
    /// The calculation resulted in a divide-by-zero error.
    /// </summary>
    /// <remarks>#DIV</remarks>
    DivisionByZero = 3,

    /// <summary>
    /// The calculation resulted in recursion that went beyond the allowed recursion limit.
    /// </summary>
    Recursion = 4,

    /// <summary>
    /// The calculation resulted in an invalid value or received input of the wrong type.
    /// </summary>
    /// <remarks>#VALUE</remarks>
    BadValue = 5,

    /// <summary>
    /// The calculation failed due to an unexpected internal program error.
    /// </summary>
    InternalError = 6,
}