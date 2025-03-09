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

using Figment.Common.Calculations;

namespace Figment.Common.Test.Calculations;

[TestClass]
public sealed class Lower
{
    /// <summary>
    /// Not enough parameters
    /// </summary>
    [TestMethod]
    public void LowerWithoutParameters()
    {
        var sampleThing = new Thing(nameof(LowerWithoutParameters), nameof(LowerWithoutParameters));
        var calcResult = Parser.Calculate("=LOWER()", sampleThing);
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }

    /// <summary>
    /// Not enough parameters
    /// </summary>
    [TestMethod]
    public void LowerWithBadParameterTypeRecoverable()
    {
        var sampleThing = new Thing(nameof(LowerWithBadParameterTypeRecoverable), nameof(LowerWithBadParameterTypeRecoverable));
        var calcResult = Parser.Calculate("=LOWER(1.4)", sampleThing);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsInstanceOfType<string>(result);
        Assert.AreEqual("1.4", result);
    }

    /// <summary>
    /// Tests one function with no parameters
    /// </summary>
    [TestMethod]
    public void CalculateLowerThingProperty()
    {
        var sampleThing = new Thing(nameof(CalculateLowerThingProperty), nameof(CalculateLowerThingProperty));
        var calcResult = Parser.Calculate("=LOWER([Name])", sampleThing);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsInstanceOfType<string>(result);
        Assert.AreEqual(nameof(CalculateLowerThingProperty).ToLowerInvariant(), result);
    }
}