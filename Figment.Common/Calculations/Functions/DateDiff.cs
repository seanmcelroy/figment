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

public class DateDiff : FunctionBase
{
    public override CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> targets)
    {
        if (parameters.Length != 3)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "DATEDIFF() takes three parameters");

        if (!TryGetStringParameter(1, true, parameters, targets, out CalculationResult _, out string? interval))
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "DATEDIFF() requires the first (interval) parameter");

        if (!TryGetDateParameter(2, true, parameters, targets, out CalculationResult _, out DateTime? startDateParam))
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "DATEDIFF() requires the second (start date) parameter");

        if (!DateUtility.TryParseFunctionalDateValue(startDateParam!.Value, out double startFunctionalDate))
            return CalculationResult.Error(CalculationErrorType.BadValue, "Start date parameter could not be interpreted as a date");

        if (!DateUtility.TryParseDate(startFunctionalDate, out DateTime startDate))
            return CalculationResult.Error(CalculationErrorType.BadValue, "Start date parameter could not be interpreted as a date");

        if (!TryGetDateParameter(3, true, parameters, targets, out CalculationResult _, out DateTime? endDateParam))
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "DATEDIFF() requires the third (end date) parameter");

        if (!DateUtility.TryParseFunctionalDateValue(endDateParam!.Value, out double endFunctionalDate))
            return CalculationResult.Error(CalculationErrorType.BadValue, "End date parameter could not be interpreted as a date");

        if (!DateUtility.TryParseDate(endFunctionalDate, out DateTime endDate))
            return CalculationResult.Error(CalculationErrorType.BadValue, "End date parameter could not be interpreted as a date");

        if (string.Compare(interval, "yyyy", StringComparison.InvariantCultureIgnoreCase) == 0)
        {
            var diff = (endDate - startDate).TotalDays / 365.25;
            return CalculationResult.Success(diff, CalculationResultType.FunctionResult);
        }

        return CalculationResult.Error(CalculationErrorType.BadValue, $"Unknown interval type {interval}");
    }
}