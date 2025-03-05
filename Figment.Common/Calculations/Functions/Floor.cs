namespace Figment.Common.Calculations.Functions;

public class Floor : FunctionBase
{
    public override CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> targets)
    {
        if (parameters.Length != 1)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "FLOOR() takes one parameter");

        if (!TryGetDoubleParameter(1, true, parameters, targets, out CalculationResult _, out double dbl))
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "FLOOR() takes one non-null parameter");

        return CalculationResult.Success(Math.Floor(dbl), CalculationResultType.FunctionResult);
    }
}