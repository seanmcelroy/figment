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
public sealed class Lower
{
    [TestMethod]
    public void LowerWithoutParameters()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LOWER()");
        Assert.IsNotNull(ast);
        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public void LowerWitEmptyParameter()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LOWER(\"\")");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual(string.Empty, (string)result.Result, StringComparer.InvariantCultureIgnoreCase);
    }

    [TestMethod]
    public void LowerWithBadParameterType()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LOWER(1.4)");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual("1.4", (string)result.Result, StringComparer.InvariantCultureIgnoreCase);
    }

    [TestMethod]
    public void LowerLiteral()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LOWER(\"Sean\")");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual("sean", (string)result.Result, StringComparer.InvariantCultureIgnoreCase);
    }

    [TestMethod]
    public void CalculateLowerThingProperty()
    {
        var sampleThing = new Thing(nameof(CalculateLowerThingProperty), nameof(CalculateLowerThingProperty));
        var xp = new ExpressionParser();
        var ast = xp.Parse("=LOWER([Name])");
        Assert.IsNotNull(ast);

        var ctx = new EvaluationContext(sampleThing);
        var result = ast.Evaluate(ctx);
        Assert.IsTrue(result.IsSuccess);

        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual(nameof(CalculateLowerThingProperty).ToLowerInvariant(), (string)result.Result, StringComparer.InvariantCultureIgnoreCase);
    }
}