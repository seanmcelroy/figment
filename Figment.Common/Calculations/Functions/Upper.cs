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

using Figment.Common.Calculations.Parsing;

namespace Figment.Common.Calculations.Functions;

/// <summary>
/// The UPPER() function capitalizes strings or values coerceable into strings.
/// </summary>
public class Upper : FunctionBase
{
    /// <summary>
    /// The identifier of this function.
    /// </summary>
    public const string IDENTIFIER = "UPPER";

    /// <inheritdoc/>
    public override string Identifier => IDENTIFIER;

    /// <inheritdoc/>
    public override ExpressionResult Evaluate(EvaluationContext context, NodeBase[] arguments)
    {
        if (arguments.Length != 1)
        {
            return ExpressionResult.Error(CalculationErrorType.FormulaParse, "UPPER() takes one parameter");
        }

        var argumentResult = arguments[0].Evaluate(context);
        if (!argumentResult.IsSuccess)
        {
            return argumentResult;
        }

        if (argumentResult.Result is int || argumentResult.Result is double)
        {
            return ExpressionResult.Error(CalculationErrorType.FormulaParse, "UPPER() takes one string parameter, but a numeric was provided.");
        }

        if (!argumentResult.TryConvertString(out string? stringResult))
        {
            return ExpressionResult.Error(CalculationErrorType.BadValue, $"Unable to convert '{argumentResult.Result}' to a string");
        }

        return ExpressionResult.Success(stringResult?.ToUpperInvariant());
    }
}