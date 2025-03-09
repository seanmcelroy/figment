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
public sealed class Floor
{
    /// <summary>
    /// Not enough parameters
    /// </summary>
    [TestMethod]
    public void FloorWithoutParameters()
    {
        var sampleThing = new Thing(nameof(FloorWithoutParameters), nameof(FloorWithoutParameters));
        var calcResult = Parser.Calculate("=FLOOR()", sampleThing);
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }

    /// <summary>
    /// Not enough parameters
    /// </summary>
    [TestMethod]
    public void FloorWithBadParameterType()
    {
        var sampleThing = new Thing(nameof(FloorWithBadParameterType), nameof(FloorWithBadParameterType));
        var calcResult = Parser.Calculate("=FLOOR('what')", sampleThing);
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }

    [TestMethod]
    public void FloorStaticFloatBelowHalf()
    {
        var sampleThing = new Thing(nameof(FloorStaticFloatBelowHalf), nameof(FloorStaticFloatBelowHalf));
        var calcResult = Parser.Calculate("=Floor(1.4)", sampleThing);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsInstanceOfType<double>(result);
        Assert.AreEqual(1D, result);
    }

    [TestMethod]
    public void FloorStaticFloatAboveHalf()
    {
        var sampleThing = new Thing(nameof(FloorStaticFloatAboveHalf), nameof(FloorStaticFloatAboveHalf));
        var calcResult = Parser.Calculate("=Floor(1.755)", sampleThing);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsInstanceOfType<double>(result);
        Assert.AreEqual(1D, result);
    }

    [TestMethod]
    public void FloorStaticThousandsSeparator()
    {
        var sampleThing = new Thing(nameof(FloorStaticThousandsSeparator), nameof(FloorStaticThousandsSeparator));
        var calcResult = Parser.Calculate("=Floor(1,234,567.89)", sampleThing);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsInstanceOfType<double>(result);
        Assert.AreEqual(1234567D, result);
    }

    [TestMethod]
    public void FloorStaticBigSeparator()
    {
        var sampleThing = new Thing(nameof(FloorStaticBigSeparator), nameof(FloorStaticBigSeparator));
        var calcResult = Parser.Calculate("=Floor(12345671234567.89)", sampleThing);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsInstanceOfType<double>(result);
        Assert.AreEqual(12345671234567D, result);
    }

    [TestMethod]
    public void FloorStaticNegative()
    {
        var sampleThing = new Thing(nameof(FloorStaticNegative), nameof(FloorStaticNegative));
        var calcResult = Parser.Calculate("=Floor(-567.89)", sampleThing);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsInstanceOfType<double>(result);
        Assert.AreEqual(-568D, result);
    }

    /// <summary>
    /// Tests on null, which is not valid
    /// </summary>
    [TestMethod]
    public void CalculateFloorNull()
    {
        var calcResult = Parser.Calculate("=FLOOR(NULL())");
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }

    /// <summary>
    /// Tests on invalid number 1.2.3, which is not valid
    /// </summary>
    [TestMethod]
    public void CalculateFloorSemanticVersion()
    {
        var calcResult = Parser.Calculate("=FLOOR(1.2.3)");
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }

    /// <summary>
    /// Tests on invalid number 1.2., which is not valid
    /// </summary>
    [TestMethod]
    public void CalculateFloorExtraEndingDecimal()
    {
        var calcResult = Parser.Calculate("=FLOOR(1.2.)");
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }

    /// <summary>
    /// Tests on invalid number 1..2, which is not valid
    /// </summary>
    [TestMethod]
    public void CalculateFloorDoubleDecimal()
    {
        var calcResult = Parser.Calculate("=FLOOR(1..2)");
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }
}