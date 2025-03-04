namespace Figment.Common.Test.Calculations;

[TestClass]
public sealed class Today
{
    /// <summary>
    /// Tests one function with no parameters
    /// </summary>
    [TestMethod]
    public void ParseToday()
    {
        var (success, message, root) = Common.Calculations.Parser.ParseFormula("=TODAY()");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke();
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<double>(result);
        Assert.IsTrue((double)result >= 45718);
        Console.Out.WriteLine(calcResult.Result);
    }

    /// <summary>
    /// Tests one function with no parameters
    /// </summary>
    [TestMethod]
    public void ParseTodayExtraParenthesis()
    {
        var (success, message, root) = Common.Calculations.Parser.ParseFormula("=(TODAY())");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke();
        Assert.IsFalse(calcResult.IsError);
        Console.Out.WriteLine(calcResult.Result);
    }

    /// <summary>
    /// Tests two different functions with nesting and no parameters
    /// </summary>
    [TestMethod]
    public void ParseLowerToday()
    {
        var (success, message, root) = Common.Calculations.Parser.ParseFormula("=LOWER(TODAY())");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke();
        Assert.IsFalse(calcResult.IsError);
        Console.Out.WriteLine(calcResult.Result);
    }

    /// <summary>
    /// Tests two different functions with nesting and no parameters
    /// </summary>
    [TestMethod]
    public void ParseLowerTodayExtraParenthesis()
    {
        var (success, message, root) = Common.Calculations.Parser.ParseFormula("=LOWER((TODAY()))");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke();
        Assert.IsFalse(calcResult.IsError);
        Console.Out.WriteLine(calcResult.Result);
    }

        /// <summary>
    /// Tests two different functions with nesting and no parameters
    /// </summary>
    [TestMethod]
    public void ParseLowerTodayExtraParenthesis2()
    {
        var (success, message, root) = Common.Calculations.Parser.ParseFormula("=LOWER((LOWER((TODAY()))))");
        Assert.IsTrue(success);
        Assert.IsNotNull(root);
        var calcResult = root.Invoke();
        Assert.IsFalse(calcResult.IsError);
        Console.Out.WriteLine(calcResult.Result);
    }

}