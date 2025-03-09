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

namespace Figment.Test.Calculations;

[TestClass]
public sealed class DateDiff
{
    /// <summary>
    /// Tests valid yyyy
    /// </summary>
    [TestMethod]
    public void DateDiffYYYY()
    {
        var (success, message, root) = Common.Calculations.Parser.ParseFormula("=DATEDIFF(\"yyyy\", '1981-01-26', '2025-01-26')");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke([]);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<double>(result);
        Assert.IsTrue((double)result >= 44);
        Console.Out.WriteLine(calcResult.Result);
    }

    /// <summary>
    /// Tests with missing parameters
    /// </summary>
    [TestMethod]
    public void DateDiffNoParameters()
    {
        var calcResult = Common.Calculations.Parser.Calculate("=DATEDIFF()");
        Assert.IsTrue(calcResult.IsError);
    }

     /// <summary>
    /// Tests with wrong parameters
    /// </summary>
    [TestMethod]
    public void DateDiffWrongParameters()
    {
        var calcResult = Common.Calculations.Parser.Calculate("=DATEDIFF(\"aaa\", '1981-01-26', '2025-01-26')");
        Assert.IsTrue(calcResult.IsError);

        calcResult = Common.Calculations.Parser.Calculate("=DATEDIFF(1.2, '1981-01-26', '2025-01-26')");
        Assert.IsTrue(calcResult.IsError);

        calcResult = Common.Calculations.Parser.Calculate("=DATEDIFF(\"yyyy\", 'aaaa-01-26', '2025-01-26')");
        Assert.IsTrue(calcResult.IsError);

        calcResult = Common.Calculations.Parser.Calculate("=DATEDIFF(\"yyyy\", '1981-01-26', 'aaaa-01-26')");
        Assert.IsTrue(calcResult.IsError);

        calcResult = Common.Calculations.Parser.Calculate("=DATEDIFF(\"yyyy\", 'aaaa-01-26', 1)");
        Assert.IsTrue(calcResult.IsError);

        calcResult = Common.Calculations.Parser.Calculate("=DATEDIFF(\"yyyy\", 1, 'aaaa-01-26')");
        Assert.IsTrue(calcResult.IsError);

    }
}