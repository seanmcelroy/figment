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

using Figment.Common;
using Figment.Common.Calculations;

namespace Figment.Test.Calculations;

[TestClass]
public sealed class Len
{
    /// <summary>
    /// Not enough parameters
    /// </summary>
    [TestMethod]
    public void LenWithoutParameters()
    {
        var sampleThing = new Thing(nameof(LenWithoutParameters), nameof(LenWithoutParameters));
        var calcResult = Parser.Calculate("=LEN()", sampleThing);
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }

    /// <summary>
    /// Not enough parameters
    /// </summary>
    [TestMethod]
    public void LenWithBadParameterTypeRecoverable()
    {
        var sampleThing = new Thing(nameof(LenWithBadParameterTypeRecoverable), nameof(LenWithBadParameterTypeRecoverable));
        var calcResult = Parser.Calculate("=LEN(1.4)", sampleThing);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsInstanceOfType<int>(result);
        Assert.AreEqual(3, result);
    }

    /// <summary>
    /// Tests one function with no parameters
    /// </summary>
    [TestMethod]
    public void CalculateLenThingProperty()
    {
        var sampleThing = new Thing(nameof(CalculateLenThingProperty), nameof(CalculateLenThingProperty));
        var calcResult = Parser.Calculate("=LEN([Name])", sampleThing);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsInstanceOfType<int>(result);
        Assert.AreEqual(nameof(CalculateLenThingProperty).Length, result);
    }

    /// <summary>
    /// Tests on null, which is not valid
    /// </summary>
    [TestMethod]
    public void CalculateLenNull()
    {
        var calcResult = Parser.Calculate("=LEN(NULL())");
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }
}