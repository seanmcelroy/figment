using System.Net.Sockets;

namespace Figment.Common.Test.Calculations;

[TestClass]
public sealed class Lower
{
    /// <summary>
    /// Tests one function with no parameters
    /// </summary>
    [TestMethod]
    public async Task CalculateLowerThingProperty()
    {
        var sampleThing = new Thing(nameof(CalculateLowerThingProperty), nameof(CalculateLowerThingProperty));
        var calcResult = await Common.Calculations.Parser.CalculateAsync("=LOWER([Name])", sampleThing);
        Assert.IsFalse(calcResult.IsError);

        var result = calcResult.Result;
        Assert.IsInstanceOfType<string>(result);
        Assert.AreEqual(result, nameof(CalculateLowerThingProperty).ToLowerInvariant());
        Console.Out.WriteLine(result);
    }
}