namespace Figment.Common.Calculations.Functions;

public class Now : IFunction
{
    public CalculationResult Evaluate(CalculationResult[] parameters)
    {
        if (parameters.Length != 0)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "NOW() takes no parameters");

        TimeSpan ts = DateTime.UtcNow - DateUtility.TwentiethCentry;
        return CalculationResult.Success(ts.TotalDays + 1);
    }
}