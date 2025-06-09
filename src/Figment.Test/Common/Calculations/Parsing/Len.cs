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
public sealed class Len
{
    /// <summary>
    /// Not enough parameters
    /// </summary>
    [TestMethod]
    public void LenWithoutParameters()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LEN()");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.FormulaParse, result.ErrorType);
    }

    /// <summary>
    /// Not enough parameters
    /// </summary>
    [TestMethod]
    public void LenWithBadParameterTypeRecoverable()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LEN(1.4)");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.FormulaParse, result.ErrorType);
    }

    [TestMethod]
    public void LenLiteral()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LEN(\"Sean\")");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<int>(result.Result);
        Assert.AreEqual(4, (int)result.Result);
    }

    [TestMethod]
    public void CalculateLenThingProperty()
    {
        var sampleThing = new Thing(nameof(CalculateLenThingProperty), nameof(CalculateLenThingProperty));
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LEN([Name])");
        Assert.IsNotNull(ast);

        var ctx = new EvaluationContext(sampleThing);
        var result = ast.Evaluate(ctx);
        Assert.IsTrue(result.IsSuccess);

        Assert.IsInstanceOfType<int>(result.Result);
        Assert.AreEqual(nameof(CalculateLenThingProperty).Length, result.Result);
    }

    /// <summary>
    /// Tests on null, which is not valid
    /// </summary>
    [TestMethod]
    public void CalculateLenNull()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LEN(NULL())");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.BadValue, result.ErrorType);
    }
}