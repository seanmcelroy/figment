namespace Figment.Common.Calculations.Functions;

public class Today : IFunction
{
    public CalculationResult Evaluate(CalculationResult[] parameters)
    {
        if (parameters.Length != 0)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "TODAY() takes no parameters");

        TimeSpan ts = DateTime.UtcNow.Date - DateUtility.TwentiethCentry;
        return CalculationResult.Success(ts.TotalDays + 1);
    }
}