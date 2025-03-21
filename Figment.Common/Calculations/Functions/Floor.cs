namespace Figment.Common.Calculations.Functions;

/// <summary>
/// The FLOOR function returns the largest integral value less than or equal to the
/// specified double-precision floating-point number.
/// </summary>
public class Floor : FunctionBase
{
    /// <inheritdoc/>
    public override CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> targets)
    {
        if (parameters.Length != 1)
        {
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "FLOOR() takes one parameter");
        }

        if (!TryGetDoubleParameter(1, true, parameters, targets, out CalculationResult _, out double dbl))
        {
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "FLOOR() takes one non-null parameter");
        }

        return CalculationResult.Success(Math.Floor(dbl), CalculationResultType.FunctionResult);
    }
}