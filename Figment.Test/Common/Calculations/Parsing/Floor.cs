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
public sealed class Floor
{
    /// <summary>
    /// Not enough parameters
    /// </summary>
    [TestMethod]
    public void FloorWithoutParameters()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=FLOOR()");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.FormulaParse, result.ErrorType);
    }

    [TestMethod]
    public void FloorWitEmptyParameter()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=FLOOR(\"\")");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess, "Floor should not throw a parse error if a string is provided, but should return an error if that string is not parsable as a number.");
        Assert.AreEqual(CalculationErrorType.BadValue, result.ErrorType);
    }

    [TestMethod]
    public void FloorWithBadParameterType()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=FLOOR(\"Sean\")");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess, "Floor should not throw a parse error if a string is provided, but should return an error if that string is not parsable as a number.");
        Assert.AreEqual(CalculationErrorType.BadValue, result.ErrorType);
    }

    [TestMethod]
    public void FloorStaticFloatBelowHalf()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=Floor(1.4)");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<double>(result.Result);
        Assert.AreEqual(1D, result.Result);
    }

    [TestMethod]
    public void FloorStaticFloatBelowHalfAsString()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=Floor(\"1.4\")");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<double>(result.Result);
        Assert.AreEqual(1D, result.Result);
    }

    [TestMethod]
    public void FloorStaticFloatAboveHalf()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=Floor(1.755)");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<double>(result.Result);
        Assert.AreEqual(1D, result.Result);
    }

    [TestMethod]
    public void FloorStaticThousandsSeparator()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=Floor(1,234,567.89)");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<double>(result.Result);
        Assert.AreEqual(1234567D, result.Result);
    }

    [TestMethod]
    public void FloorStaticBigSeparator()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=Floor(12345671234567.89)");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<double>(result.Result);
        Assert.AreEqual(12345671234567D, result.Result);
    }

    [TestMethod]
    public void FloorStaticNegative()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=Floor(-567.89)");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<double>(result.Result);
        Assert.AreEqual(-568D, result.Result);
    }

    /// <summary>
    /// Tests on null, which is not valid
    /// </summary>
    [TestMethod]
    public void CalculateFloorNull()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=FLOOR(NULL())");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.BadValue, result.ErrorType);
    }

    /// <summary>
    /// Tests on invalid number 1.2.3, which is not valid
    /// </summary>
    [TestMethod]
    public void CalculateFloorSemanticVersion()
    {
        var xp = new ExpressionParser();
        Assert.ThrowsExactly<ParseException>(() => xp.Parse("=FLOOR(1.2.3)"));
    }

    /// <summary>
    /// Tests on invalid number 1.2., which is not valid
    /// </summary>
    [TestMethod]
    public void CalculateFloorExtraEndingDecimal()
    {
        var xp = new ExpressionParser();
        Assert.ThrowsExactly<ParseException>(() => xp.Parse("=FLOOR(1.2.)"));
    }

    /// <summary>
    /// Tests on invalid number 1..2, which is not valid
    /// </summary>
    [TestMethod]
    public void CalculateFloorDoubleDecimal()
    {
        var xp = new ExpressionParser();
        Assert.ThrowsExactly<ParseException>(() => xp.Parse("=FLOOR(1..2)"));
    }
}