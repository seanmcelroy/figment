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

namespace Figment.Test.Common.Calculations;

[TestClass]
public sealed class False
{
    [TestMethod]
    public void FalseWithoutParameters()
    {
        var calcResult = Parser.Calculate("=FALSE()");
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsFalse(result as bool?);
    }

    [TestMethod]
    public void FalseWithNonsenseParameters()
    {
        var calcResult = Parser.Calculate("=FALSE(nope)");
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }

    [TestMethod]
    public void FalseWithActualParameters()
    {
        var calcResult = Parser.Calculate("=FALSE(1)");
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }
}