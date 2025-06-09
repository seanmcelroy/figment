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

namespace Figment.Common.Calculations.Parsing;

/// <summary>
/// An exception thrown if the <see cref="ExpressionParser"/> cannot parse an expression.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ParseException"/> class.
/// </remarks>
/// <param name="message">The error message that explains the reason for the exception.</param>
/// <param name="position">The index of the expression where the exception occurred during parsing.</param>
public class ParseException(string message, int position) : Exception(message)
{
    /// <summary>
    /// Gets the index of the expression where the exception occurred during parsing.
    /// </summary>
    public int Position { get; init; } = position;
}