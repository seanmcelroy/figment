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

namespace Figment.Test.Common.Calculations;

[TestClass]
public sealed class If
{
    /// <summary>
    /// Tests whether If() can parse and execute correctly
    /// </summary>
    [TestMethod]
    public void IfTrueAsInteger()
    {
        var (success, message, root) = Figment.Common.Calculations.Parser.ParseFormula("=IF(1, '1981-01-26', '2025-01-26')");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke([]);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<string>(result);
        Assert.AreEqual("1981-01-26", result);
        Console.Out.WriteLine(calcResult.Result);
    }

    /// <summary>
    /// Tests whether If() can parse and execute correctly
    /// </summary>
    [TestMethod]
    public void IfTrueAsEquation()
    {
        var (success, message, root) = Figment.Common.Calculations.Parser.ParseFormula("=IF(1=1, '1981-01-26', '2025-01-26')");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke([]);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<string>(result);
        Assert.AreEqual("1981-01-26", result);
        Console.Out.WriteLine(calcResult.Result);
    }

    /// <summary>
    /// Tests whether If() can parse and execute correctly
    /// </summary>
    [TestMethod]
    public void IfTrueAsFunction()
    {
        var (success, message, root) = Figment.Common.Calculations.Parser.ParseFormula("=IF(TRUE(), '1981-01-26', '2025-01-26')");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke([]);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<string>(result);
        Assert.AreEqual("1981-01-26", result);
        Console.Out.WriteLine(calcResult.Result);
    }
}