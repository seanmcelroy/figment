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
using Figment.Common.Data;
using Figment.Data.Memory;

namespace Figment.Test.Common.Calculations.Parsing;

[TestClass]
public sealed class DateDiff
{
    [TestInitialize]
    public void Initialize()
    {
        AmbientStorageContext.StorageProvider = new MemoryStorageProvider();
        _ = AmbientStorageContext.StorageProvider.InitializeAsync(CancellationToken.None).Result;
    }

    /// <summary>
    /// Tests valid yyyy
    /// </summary>
    [TestMethod]
    public void DateDiffYYYY()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=DATEDIFF(\"yyyy\", '1981-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsTrue(result.IsSuccess);

        Assert.IsInstanceOfType<double>(result.Result);
        Assert.AreEqual(44, (double)result.Result);
    }

    /// <summary>
    /// Tests with missing parameters
    /// </summary>
    [TestMethod]
    public void DateDiffNoParameters()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=DATEDIFF()");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.FormulaParse, result.ErrorType);
    }

    [TestMethod]
    public void DateDiffUnsupportedInterval()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=DATEDIFF(\"aaa\", '1981-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.BadValue, result.ErrorType);
    }

    [TestMethod]
    public void DateDiffInvalidIntervalFormat()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=DATEDIFF(1234, '1981-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.BadValue, result.ErrorType);
    }

    [TestMethod]
    public void DateDiffInvalidStartDate()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=DATEDIFF(\"yyyy\", 'aaaa-01-26', '2025-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.BadValue, result.ErrorType);
    }

    [TestMethod]
    public void DateDiffInvalidEndDate()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=DATEDIFF(\"yyyy\", '1981-01-26', 'aaaa-01-26')");
        Assert.IsNotNull(ast);

        var result = ast.Evaluate(EvaluationContext.EMPTY);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.BadValue, result.ErrorType);
    }

    [TestMethod]
    public async Task DateDiffPropertyExistsAndSet()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=DATEDIFF(\"yyyy\", [birthdate], NOW())");
        Assert.IsNotNull(ast);

        var sampleSchema = new Schema($"schema-{nameof(DateDiffPropertyExistsButNotSet)}", $"schema-{nameof(DateDiffPropertyExistsButNotSet)}");
        var stf = sampleSchema.AddDateField("birthdate");
        var (schemaSaved, _) = await sampleSchema.SaveAsync(CancellationToken.None);
        Assert.IsTrue(schemaSaved);

        var sampleThing = new Thing(nameof(DateDiffPropertyExistsButNotSet), nameof(DateDiffPropertyExistsButNotSet));
        var (thingSaved, _) = await sampleThing.SaveAsync(CancellationToken.None);
        Assert.IsTrue(thingSaved);
        var assocResult = await sampleThing.AssociateWithSchemaAsync(sampleSchema.Name, CancellationToken.None);
        Assert.IsTrue(assocResult.Item1);
        sampleThing = assocResult.Item2;
        Assert.IsNotNull(sampleThing);

        var setResult = await sampleThing.Set("birthdate", "January 26, 1981", CancellationToken.None);
        Assert.IsTrue(setResult.Success);

        // Birthdate set to null.
        var context = new EvaluationContext(sampleThing);
        var result = ast.Evaluate(context);
        Assert.IsTrue(result.IsSuccess);
    }


    [TestMethod]
    public async Task DateDiffPropertyExistsButNotSet()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=DATEDIFF(\"yyyy\", [birthdate], NOW())");
        Assert.IsNotNull(ast);

        var sampleSchema = new Schema($"schema-{nameof(DateDiffPropertyExistsButNotSet)}", $"schema-{nameof(DateDiffPropertyExistsButNotSet)}");
        var stf = sampleSchema.AddMonthDayField("birthdate");
        var (schemaSaved, _) = await sampleSchema.SaveAsync(CancellationToken.None);
        Assert.IsTrue(schemaSaved);

        var sampleThing = new Thing(nameof(DateDiffPropertyExistsButNotSet), nameof(DateDiffPropertyExistsButNotSet));
        var (thingSaved, _) = await sampleThing.SaveAsync(CancellationToken.None);
        Assert.IsTrue(thingSaved);
        var assocResult = await sampleThing.AssociateWithSchemaAsync($"schema-{nameof(DateDiffPropertyExistsButNotSet)}", CancellationToken.None);
        Assert.IsTrue(assocResult.Item1);
        sampleThing = assocResult.Item2;
        Assert.IsNotNull(sampleThing);

        // Birthdate set to null.
        var context = new EvaluationContext(sampleThing);
        var result = ast.Evaluate(context);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.BadValue, result.ErrorType);
    }

    [TestMethod]
    public void DateDiffPropertyDoesNotExist()
    {
        var xp = new ExpressionParser();
        var ast = xp.Parse("=DATEDIFF(\"yyyy\", [birthdate], NOW())");
        Assert.IsNotNull(ast);

        var sampleThing = new Thing(nameof(DateDiffPropertyExistsButNotSet), nameof(DateDiffPropertyExistsButNotSet));
        // No birthdate set.
        var context = new EvaluationContext(sampleThing);
        var result = ast.Evaluate(context);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(CalculationErrorType.FormulaParse, result.ErrorType);
    }

}