namespace Figment.Common.Calculations.Functions;

public class Upper : FunctionBase
{
    public override CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> targets)
    {
        if (parameters.Length != 1)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "UPPER() takes one parameter");

        var v = parameters[0].Result;
        if (v == null)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "UPPER() takes one non-null parameter");

        var x = v.ToString();
        return CalculationResult.Success(v.ToString()?.ToUpperInvariant() ?? string.Empty, CalculationResultType.FunctionResult);
    }
}