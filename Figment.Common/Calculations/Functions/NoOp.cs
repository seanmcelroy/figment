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
/// This function is used to encapsulate another inner value or function.  Parenthesis
/// are represnetd in function parse trees as NoOp functions.
/// </summary>
public class NoOp : FunctionBase
{
    /// <inheritdoc/>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public override CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> _)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    {
        return parameters.Length switch
        {
            0 => CalculationResult.Success(null, CalculationResultType.FunctionResult),
            1 => CalculationResult.Success(parameters[0], CalculationResultType.FunctionResult),
            _ => throw new InvalidOperationException("Cannot pass-thru multiple arguments"),
        };
    }
}