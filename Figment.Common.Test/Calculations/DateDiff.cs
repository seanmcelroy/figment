namespace Figment.Common.Test.Calculations;

[TestClass]
public sealed class DateDiff
{
    /// <summary>
    /// Tests one function with no parameters
    /// </summary>
    [TestMethod]
    public void DateDiffYYYY()
    {
        var (success, message, root) = Common.Calculations.Parser.ParseFormula("=DATEDIFF(\"yyyy\", '1981-01-26', '2025-01-26')");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke();
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<double>(result);
        Assert.IsTrue((double)result >= 44);
        Console.Out.WriteLine(calcResult.Result);
    }
}