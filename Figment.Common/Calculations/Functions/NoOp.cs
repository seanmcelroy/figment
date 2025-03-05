namespace Figment.Common.Calculations.Functions;

public class NoOp : FunctionBase
{
    public override CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> _)
    {
        switch (parameters.Length)
        {
            case 0:
                return CalculationResult.Success(null, CalculationResultType.FunctionResult);
            case 1:
                return CalculationResult.Success(parameters[0], CalculationResultType.FunctionResult);
            default:
                throw new InvalidOperationException("Cannot pass-thru multiple arguments");
        }
    }
}