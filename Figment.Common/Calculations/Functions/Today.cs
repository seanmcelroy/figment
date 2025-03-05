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

public class Today : FunctionBase
{
    public override CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> _)
    {
        if (parameters.Length != 0)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "TODAY() takes no parameters");

        TimeSpan ts = DateTime.UtcNow.Date - DateUtility.TwentiethCentry;
        return CalculationResult.Success(ts.TotalDays + 1, CalculationResultType.FunctionResult);
    }
}