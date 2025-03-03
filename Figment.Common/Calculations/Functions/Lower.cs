namespace Figment.Common.Calculations.Functions;

public class Lower : IFunction
{
    public CalculationResult Evaluate(CalculationResult[] parameters)
    {
        if (parameters.Length != 1)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "LOWER() takes one parameter");

        var v = parameters[0].Result;
        if (v == null)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "LOWER() takes one non-null parameter");

        var x = v.ToString();
        return CalculationResult.Success(v.ToString()?.ToLowerInvariant() ?? string.Empty);
    }
}