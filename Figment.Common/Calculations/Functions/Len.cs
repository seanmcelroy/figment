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
/// Returns the number of characters in a text string.
/// </summary>
public class Len : FunctionBase
{
    /// <inheritdoc/>
    public override CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> targets)
    {
        if (parameters.Length != 1)
        {
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "LEN() takes one parameter");
        }

        if (!TryGetStringParameter(1, true, parameters, targets, out CalculationResult _, out string? text))
        {
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "LEN() takes one non-null parameter");
        }

        return CalculationResult.Success(text?.Length ?? 0, CalculationResultType.FunctionResult);
    }
}