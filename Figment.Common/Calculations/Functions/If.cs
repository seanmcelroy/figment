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

namespace Figment.Common.Calculations.Functions;

/// <summary>
/// The IF function allows you to make a logical comparison between a value and
/// what you expect by testing for a condition and returning a result if that
/// condition is true or false.
/// </summary>
public class If : FunctionBase
{
    /// <inheritdoc/>
    public override CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> targets)
    {
        if (parameters.Length != 3)
        {
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "IF() takes three parameters");
        }

        if (!TryGetBooleanParameter(1, true, parameters, targets, out CalculationResult _, out bool? condition))
        {
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "IF() requires the first (boolean) parameter");
        }

        if (condition ?? false)
        {
            return CalculationResult.Success(parameters[1].Result, CalculationResultType.FunctionResult);
        }
        else
        {
            return CalculationResult.Success(parameters[2].Result, CalculationResultType.FunctionResult);
        }
    }
}