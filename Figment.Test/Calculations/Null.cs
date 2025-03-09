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

namespace Figment.Test.Calculations;

[TestClass]
public sealed class Null
{
    [TestMethod]
    public void NullWithoutParameters()
    {
        var calcResult = Parser.Calculate("=NULL()");
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.AreEqual(null, result);
    }

    [TestMethod]
    public void NullWithNonsenseParameters()
    {
        var calcResult = Parser.Calculate("=NULL(nope)");
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }

    [TestMethod]
    public void NullWithActualParameters()
    {
        var calcResult = Parser.Calculate("=NULL(1)");
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }
}