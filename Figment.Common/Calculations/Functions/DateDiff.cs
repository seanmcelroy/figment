namespace Figment.Common.Calculations.Functions;

public class DateDiff : IFunction
{
    public CalculationResult Evaluate(CalculationResult[] parameters)
    {
        if (parameters.Length != 3)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "DATEDIFF() takes three parameters");

        var interval = parameters[0].Result;
        if (interval == null)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "DATEDIFF() requires the first (interval) parameter");

        var startDateParam = parameters[1].Result;
        if (startDateParam == null)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "DATEDIFF() requires the second (start date) parameter");

        if (!DateUtility.TryParseFunctionalDateValue(startDateParam.ToString(), out double? startFunctionalDate))
        {
            return CalculationResult.Error(CalculationErrorType.BadValue, "Start date parameter could not be interpreted as a date");
        }

        if (!DateUtility.TryParseDate(startFunctionalDate!.Value, out DateTime? startDate))
        {
            return CalculationResult.Error(CalculationErrorType.BadValue, "Start date parameter could not be interpreted as a date");
        }

        var endDateParam = parameters[2].Result;
        if (endDateParam == null)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, "DATEDIFF() requires the third (end date) parameter");

        if (!DateUtility.TryParseFunctionalDateValue(endDateParam.ToString(), out double? endFunctionalDate))
        {
            return CalculationResult.Error(CalculationErrorType.BadValue, "End date parameter could not be interpreted as a date");
        }

        if (!DateUtility.TryParseDate(endFunctionalDate!.Value, out DateTime? endDate))
        {
            return CalculationResult.Error(CalculationErrorType.BadValue, "End date parameter could not be interpreted as a date");
        }

        if (interval.ToString() == "yyyy")
        {
            var diff = (endDate!.Value - startDate!.Value).TotalDays / 365.25;
            return CalculationResult.Success(diff);
        }

        return CalculationResult.Error(CalculationErrorType.BadValue, $"Unknown interval type {interval}");
    }
}