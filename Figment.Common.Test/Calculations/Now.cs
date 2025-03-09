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
public sealed class Now
{
    /// <summary>
    /// Tests one function with no parameters
    /// </summary>
    [TestMethod]
    public void ParseNow()
    {
        var (success, message, root) = Parser.ParseFormula("=NOW()");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke([]);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<double>(result);

        var dr = (double)result;
        Assert.IsTrue(dr >= 45718);
        Assert.AreNotEqual(Math.Truncate(dr), dr);

        Console.Out.WriteLine(calcResult.Result);
    }

        /// <summary>
    /// Not enough parameters
    /// </summary>
    [TestMethod]
    public void NowWithParameters()
    {
        var sampleThing = new Thing(nameof(NowWithParameters), nameof(NowWithParameters));
        var calcResult = Parser.Calculate("=Now([Name])", sampleThing);
        Assert.IsTrue(calcResult.IsError);
        Assert.AreEqual(CalculationErrorType.FormulaParse, calcResult.ErrorType);
    }


    /// <summary>
    /// Tests one function with no parameters
    /// </summary>
    [TestMethod]
    public void ParseTodayExtraParenthesis()
    {
        var (success, message, root) = Parser.ParseFormula("=(NOW())");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke([]);
        Assert.IsFalse(calcResult.IsError);
        Console.Out.WriteLine(calcResult.Result);
    }

    /// <summary>
    /// Tests two different functions with nesting and no parameters
    /// </summary>
    [TestMethod]
    public void ParseLowerToday()
    {
        var (success, message, root) = Parser.ParseFormula("=LOWER(NOW())");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke([]);
        Assert.IsFalse(calcResult.IsError);
        Console.Out.WriteLine(calcResult.Result);
    }

    /// <summary>
    /// Tests two different functions with nesting and no parameters
    /// </summary>
    [TestMethod]
    public void ParseLowerTodayExtraParenthesis()
    {
        var (success, message, root) = Parser.ParseFormula("=LOWER((NOW()))");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke([]);
        Assert.IsFalse(calcResult.IsError);
        Console.Out.WriteLine(calcResult.Result);
    }

        /// <summary>
    /// Tests two different functions with nesting and no parameters
    /// </summary>
    [TestMethod]
    public void ParseLowerTodayExtraParenthesis2()
    {
        var (success, message, root) = Parser.ParseFormula("=LOWER((LOWER((NOW()))))");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke([]);
        Assert.IsFalse(calcResult.IsError);
        Console.Out.WriteLine(calcResult.Result);
    }

}