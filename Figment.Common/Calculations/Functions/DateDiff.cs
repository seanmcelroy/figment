namespace Figment.Common.Calculations.Functions;

public class DateDiff : FunctionBase
{
    public override CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> targets)
    {
        if (parameters.Length != 3)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "DATEDIFF() takes three parameters");

        if (!TryGetStringParameter(1, true, parameters, targets, out CalculationResult _, out string? interval))
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "DATEDIFF() requires the first (interval) parameter");

        if (!TryGetDateParameter(2, true, parameters, targets, out CalculationResult _, out DateTime? startDateParam))
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "DATEDIFF() requires the second (start date) parameter");

        if (!DateUtility.TryParseFunctionalDateValue(startDateParam!.Value, out double startFunctionalDate))
            return CalculationResult.Error(CalculationErrorType.BadValue, "Start date parameter could not be interpreted as a date");

        if (!DateUtility.TryParseDate(startFunctionalDate, out DateTime startDate))
            return CalculationResult.Error(CalculationErrorType.BadValue, "Start date parameter could not be interpreted as a date");

        if (!TryGetDateParameter(3, true, parameters, targets, out CalculationResult _, out DateTime? endDateParam))
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "DATEDIFF() requires the third (end date) parameter");

        if (!DateUtility.TryParseFunctionalDateValue(endDateParam!.Value, out double endFunctionalDate))
            return CalculationResult.Error(CalculationErrorType.BadValue, "End date parameter could not be interpreted as a date");

        if (!DateUtility.TryParseDate(endFunctionalDate, out DateTime endDate))
            return CalculationResult.Error(CalculationErrorType.BadValue, "End date parameter could not be interpreted as a date");

        if (string.Compare(interval, "yyyy", StringComparison.InvariantCultureIgnoreCase) == 0)
        {
            var diff = (endDate - startDate).TotalDays / 365.25;
            return CalculationResult.Success(diff, CalculationResultType.FunctionResult);
        }

        return CalculationResult.Error(CalculationErrorType.BadValue, $"Unknown interval type {interval}");
    }
}