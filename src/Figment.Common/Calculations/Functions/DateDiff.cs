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
/// Calculates the number of days, months, or years between two dates.
/// This function is useful in formulas where you need to calculate an age.
/// </summary>
public class DateDiff : FunctionBase
{
    /// <summary>
    /// The identifier of this function.
    /// </summary>
    public const string IDENTIFIER = "DATEDIFF";

    /// <inheritdoc/>
    public override string Identifier => IDENTIFIER;

    /// <inheritdoc/>
    public override ExpressionResult Evaluate(EvaluationContext context, NodeBase[] arguments)
    {
        if (!ExpectArgumentCount(arguments, 3, out ExpressionResult? error))
        {
            return error.Value;
        }

        // Get argument 1
        if (!arguments[0].Evaluate(context).TryConvertString(out string? interval))
        {
            return ExpressionResult.Error(CalculationErrorType.BadValue, "DATEDIFF() requires the first (interval) string argument");
        }

        if (!arguments[1].Evaluate(context).TryConvertDateTime(out DateTimeOffset? startDate))
        {
            return ExpressionResult.Error(CalculationErrorType.BadValue, "DATEDIFF() requires the second (start date) date argument");
        }

        if (!arguments[2].Evaluate(context).TryConvertDateTime(out DateTimeOffset? endDate))
        {
            return ExpressionResult.Error(CalculationErrorType.BadValue, "DATEDIFF() requires the third (end date) date argument");
        }

        if (string.Equals(interval, "yyyy", StringComparison.InvariantCultureIgnoreCase))
        {
            var diff = (endDate.Value - startDate.Value).TotalDays / 365.25;
            return ExpressionResult.Success(diff);
        }

        return ExpressionResult.Error(CalculationErrorType.BadValue, $"Unknown interval type {interval}");
    }
}