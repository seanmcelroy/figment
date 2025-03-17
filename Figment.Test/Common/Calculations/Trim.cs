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

namespace Figment.Test.Common.Calculations;

[TestClass]
public sealed class Trim
{
    /// <summary>
    /// Not enough parameters
    /// </summary>
    [TestMethod]
    public void TrimWithoutParameters()
    {
        var sampleThing = new Thing(nameof(TrimWithoutParameters), nameof(TrimWithoutParameters));
        var calcResult = Parser.Calculate("=TRIM()", sampleThing);
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }

    /// <summary>
    /// Too many parameters
    /// </summary>
    [TestMethod]
    public void TrimWithTwoParameters()
    {
        var sampleThing = new Thing(nameof(TrimWithTwoParameters), nameof(TrimWithTwoParameters));
        var calcResult = Parser.Calculate("=Trim([Name],[Name])", sampleThing);
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }

    [TestMethod]
    public void CalculateTrimThingPropertyBeginningSpace()
    {
        var sampleThing = new Thing(nameof(CalculateTrimThingPropertyBeginningSpace), $" {nameof(CalculateTrimThingPropertyBeginningSpace)}");
        var calcResult = Parser.Calculate("=Trim([Name])", sampleThing);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsInstanceOfType<string>(result);
        Assert.AreEqual(nameof(CalculateTrimThingPropertyBeginningSpace), result);
    }

    [TestMethod]
    public void CalculateTrimThingPropertyEndingSpace()
    {
        var sampleThing = new Thing(nameof(CalculateTrimThingPropertyEndingSpace), $"{nameof(CalculateTrimThingPropertyEndingSpace)} ");
        var calcResult = Parser.Calculate("=Trim([Name])", sampleThing);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsInstanceOfType<string>(result);
        Assert.AreEqual(nameof(CalculateTrimThingPropertyEndingSpace), result);
    }


    [TestMethod]
    public void CalculateTrimThingPropertyBothSpaces()
    {
        var sampleThing = new Thing(nameof(CalculateTrimThingPropertyBothSpaces), $" {nameof(CalculateTrimThingPropertyBothSpaces)} ");
        var calcResult = Parser.Calculate("=Trim([Name])", sampleThing);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsInstanceOfType<string>(result);
        Assert.AreEqual(nameof(CalculateTrimThingPropertyBothSpaces), result);
    }

    /// <summary>
    /// Tests on null, which is not valid
    /// </summary>
    [TestMethod]
    public void CalculateTrimNull()
    {
        var calcResult = Parser.Calculate("=Trim(NULL())");
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }
}