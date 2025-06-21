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
using Figment.Common.Calculations.Parsing;

namespace Figment.Test.Common.Calculations.Parsing;

[TestClass]
public sealed class Now
{
    /// <summary>
    /// Tests one function with no parameters
    /// </summary>
    [TestMethod]
    public void ParseNow()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=NOW()");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);

        Assert.IsInstanceOfType<double>(result.Result);
        var dr = (double)result.Result;
        Assert.IsTrue(dr >= 45718);
        Assert.AreNotEqual(Math.Truncate(dr), dr);
    }

    [TestMethod]
    public void NowWithParameters()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=Now(12345)");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.FormulaParse, result.ErrorType);
    }

    [TestMethod]
    public void ParseNowExtraParenthesis()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=(NOW())");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void ParseLowerNow()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LOWER(NOW())");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void ParseLowerNowExtraParenthesis()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LOWER((NOW()))");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
    }

    /// <summary>
    /// Tests two different functions with nesting and no parameters
    /// </summary>
    [TestMethod]
    public void ParseLowerNowExtraParenthesis2()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LOWER((LOWER((NOW()))))");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
    }

}