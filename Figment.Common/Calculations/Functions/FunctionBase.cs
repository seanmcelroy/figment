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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Figment.Common.Calculations.Parsing;

namespace Figment.Common.Calculations.Functions;

/// <summary>
/// The abstract base class from which all functions usable in formulas by the <see cref="Parser"/> derive.
/// </summary>
public abstract class FunctionBase
{
    /// <summary>
    /// Gets the identifier of this function.
    /// </summary>
    public abstract string Identifier { get; }

    /// <summary>
    /// Evaluates the function using the given input <paramref name="arguments"/> over the supplied <paramref name="targets"/>.
    /// </summary>
    /// <param name="context">The context for the evaluation.</param>
    /// <param name="arguments">The arguments to provide for the funciton to perform its calculation.</param>
    /// <returns>The outcome calculation result, whether a success or failure.</returns>
    public abstract ExpressionResult Evaluate(EvaluationContext context, NodeBase[] arguments);

    /// <summary>
    /// Checks the count of arguments is an expected number.
    /// </summary>
    /// <param name="arguments">The actual arguments.</param>
    /// <param name="expectedCount">The expected number of arguments.</param>
    /// <param name="errorResult">The error result, if the expected number of arguments is not found in <see cref="arguments"/>.</param>
    /// <returns>False if the count is unexpected, otherwise true.</returns>
    protected bool ExpectArgumentCount(NodeBase[] arguments, int expectedCount, [NotNullWhen(false)] out ExpressionResult? errorResult)
    {
        if (arguments.Length != expectedCount)
        {
            errorResult = ExpressionResult.Error(CalculationErrorType.FormulaParse, $"{Identifier}() takes {expectedCount} parameter");
            return false;
        }

        errorResult = default;
        return true;
    }
}
