namespace Figment.Common.Calculations.Functions;

public class Lower : FunctionBase
{
    public override CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> targets)
    {
        if (parameters.Length != 1)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "LOWER() takes one parameter");

        if (!TryGetStringParameter(1, true, parameters, targets, out CalculationResult? _, out string? text))
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "LOWER() takes one non-null parameter");

        return CalculationResult.Success(text?.ToLowerInvariant() ?? string.Empty, CalculationResultType.FunctionResult);
    }
}