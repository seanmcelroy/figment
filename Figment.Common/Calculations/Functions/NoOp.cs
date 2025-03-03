namespace Figment.Common.Calculations.Functions;

public class NoOp : IFunction
{
    public CalculationResult Evaluate(CalculationResult[] parameters)
    {
        switch (parameters.Length)
        {
            case 0:
                return CalculationResult.Success(null);
            case 1:
                return CalculationResult.Success(parameters[0]);
            default:
                throw new InvalidOperationException("Cannot pass-thru multiple arguments");
        }
    }
}