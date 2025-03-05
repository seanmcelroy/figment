namespace Figment.Common.Calculations.Functions;

public class Now : FunctionBase
{
    public override CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> _)
    {
        if (parameters.Length != 0)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "NOW() takes no parameters");

        TimeSpan ts = DateTime.UtcNow - DateUtility.TwentiethCentry;
        return CalculationResult.Success(ts.TotalDays + 1, CalculationResultType.FunctionResult);
    }
}