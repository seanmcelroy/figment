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
using Figment.Common.Calculations.Parsing;

namespace Figment.Test.Common.Calculations.Parsing;

[TestClass]
public sealed class Trim
{
    [TestMethod]
    public void TrimWithoutParameters()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=TRIM()");
        Assert.IsNotNull(ast);
        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public void TrimWithExtraParameters()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=TRIM(\"a\",\"b\")");
        Assert.IsNotNull(ast);
        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public void TrimWithEmptyParameter()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=TRIM(\"\")");
        Assert.IsNotNull(ast);
        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual(string.Empty, (string)result.Result, StringComparer.InvariantCultureIgnoreCase);
    }

    [TestMethod]
    public void TrimWithBadParameterType()
    {
        var xp = new ExpressionParser();
        Assert.ThrowsExactly<ParseException>(() => xp.Parse("=TRIM(1.4)"));
    }

    [TestMethod]
    public void TrimLiteral()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=TRIM(\" Sean \")");
        Assert.IsNotNull(ast);
        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual("Sean", (string)result.Result, StringComparer.InvariantCultureIgnoreCase);
    }

    [TestMethod]
    public void CalculateLowerThingProperty()
    {
        var sampleThing = new Thing(nameof(CalculateLowerThingProperty), " Asdf ");
        var xp = new ExpressionParser();
        var ast = xp.Parse("=TRIM([Name])");
        Assert.IsNotNull(ast);

        var ctx = new EvaluationContext(sampleThing);
        var result = ast.Evaluate(ctx);
        Assert.IsTrue(result.IsSuccess);

        Assert.IsInstanceOfType<string>(result.Result);
        Assert.AreEqual("Asdf", (string)result.Result, StringComparer.InvariantCultureIgnoreCase);
    }
}