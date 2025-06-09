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
using Figment.Common.Calculations.Parsing;

namespace Figment.Test.Common.Calculations.Parsing;

[TestClass]
public sealed class Today
{
    /// <summary>
    /// Tests one function with no parameters
    /// </summary>
    [TestMethod]
    public void ParseToday()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=TODAY()");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);

        Assert.IsInstanceOfType<double>(result.Result);
        var dr = (double)result.Result;
        Assert.IsTrue(dr >= 45718);
        Assert.AreEqual(Math.Truncate(dr), dr);
    }

    [TestMethod]
    public void TodayWithParameters()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=TODAY(1234)");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.FormulaParse, result.ErrorType);
    }

    [TestMethod]
    public void ParseTodayExtraParenthesis()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=(TODAY())");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void ParseLowerToday()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LOWER(TODAY())");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void ParseLowerTodayExtraParenthesis()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LOWER((TODAY()))");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
    }

    /// <summary>
    /// Tests two different functions with nesting and no parameters
    /// </summary>
    [TestMethod]
    public void ParseLowerTodayExtraParenthesis2()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LOWER((LOWER((TODAY()))))");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
    }

}