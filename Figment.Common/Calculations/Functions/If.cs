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
/// The IF function allows you to make a logical comparison between a value and
/// what you expect by testing for a condition and returning a result if that
/// condition is true or false.
/// </summary>
public class If : FunctionBase
{
    /// <summary>
    /// The identifier of this function.
    /// </summary>
    public const string IDENTIFIER = "IF";

    /// <inheritdoc/>
    public override string Identifier => IDENTIFIER;

    /// <inheritdoc/>
    public override ExpressionResult Evaluate(EvaluationContext context, NodeBase[] arguments)
    {
        if (arguments.Length != 3)
        {
            return ExpressionResult.Error(CalculationErrorType.FormulaParse, "IF() takes three arguments");
        }

        var conditionResult = arguments[0].Evaluate(context);
        if (!conditionResult.IsSuccess)
        {
            return conditionResult;
        }

        if (!conditionResult.TryConvertBoolean(out bool condition))
        {
            return ExpressionResult.Error(CalculationErrorType.BadValue, $"Unable to convert '{conditionResult.Result}' into a boolean");
        }

        return condition
            ? arguments[1].Evaluate(context)
            : arguments[2].Evaluate(context);
    }
}