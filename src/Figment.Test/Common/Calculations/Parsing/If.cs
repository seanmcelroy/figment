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
public sealed class If
{
    [TestMethod]
    public void IfTooFewArguments()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=IF(TRUE())");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.FormulaParse, result.ErrorType);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public void IfUndefinedFails()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=IF(WHATEVER(), '1981-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.FormulaParse, result.ErrorType);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public void IfUnparsableFails()
    {
        var xp = new ExpressionParser();
        Assert.ThrowsExactly<ParseException>(() => xp.Parse("=IF(WHATEVER(), '1981-01-26', '2025-01-26'")); // Missing parenthesis fails.
    }

    [TestMethod]
    public void IfNonBooleanFails()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=IF('whatever', '1981-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.BadValue, result.ErrorType);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public void IfTrueAsIBoolean()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=IF(TRUE(), '1981-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual("1981-01-26", result.Result);
    }

    [TestMethod]
    public void IfFalseAsBoolean()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=IF(FALSE(), '1981-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual("2025-01-26", result.Result);
    }

    [TestMethod]
    public void IfTrueAsInteger()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=IF(1, '1981-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual("1981-01-26", result.Result);
    }

    [TestMethod]
    public void IfFalseAsInteger()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=IF(0, '1981-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual("2025-01-26", result.Result);
    }

    [TestMethod]
    public void IfTrueAsEquation()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=IF(1=1, '1981-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual("1981-01-26", result.Result);
    }

    [TestMethod]
    public void IfFalseAsEquation()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=IF(1=2, '1981-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual("2025-01-26", result.Result);
    }

    [TestMethod]
    public void IfTrueAsFunction()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=IF(TRUE(), '1981-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual("1981-01-26", result.Result);
    }

    [TestMethod]
    public void IfFalseAsFunction()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=IF(FALSE(), '1981-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual("2025-01-26", result.Result);
    }
}