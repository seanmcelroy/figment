namespace Figment.Common.Calculations.Functions;

public class Today : IFunction
{
    public CalculationResult Evaluate(CalculationResult[] parameters)
    {
        if (parameters.Length != 0)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "TODAY() takes no parameters");

        TimeSpan ts = DateTime.UtcNow - new DateTime(1900, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        return CalculationResult.Success(ts.TotalDays + 1);
    }
}