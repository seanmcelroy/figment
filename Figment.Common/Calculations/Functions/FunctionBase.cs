using System.Diagnostics.CodeAnalysis;

namespace Figment.Common.Calculations.Functions;

public abstract class FunctionBase
{
    public abstract CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> targets);

    public bool TryGetDateParameter(
        int ordinal,
        bool required,
        CalculationResult[] parameters,
        IEnumerable<Thing> targets,
        [NotNullWhen(true)] out CalculationResult? calculationResult,
        out DateTime? dateResult)
    {
        var res = TryGetStringParameter(ordinal, required, parameters, targets, out calculationResult, out string? stringResult);
        if (!res || calculationResult == null)
        {
            dateResult = null;
            return false;
        }

        // Not supplied and not required, technically success.
        if (string.IsNullOrEmpty(stringResult) && !required)
        {
            calculationResult = CalculationResult.Success(null, parameters[ordinal - 1].ResultType);
            dateResult = null;
            return true;
        }

        // Unparsable and required
        if (!DateTime.TryParse(stringResult, out DateTime resultAsDate) && required)
        {
            calculationResult = CalculationResult.Error(CalculationErrorType.BadValue, $"Cannot parse {stringResult} as a date.");
            dateResult = null;
            return false;
        }

        calculationResult = CalculationResult.Success(resultAsDate, parameters[ordinal - 1].ResultType);
        dateResult = resultAsDate;
        return true;
    }

    public bool TryGetStringParameter(
        int ordinal,
        bool required,
        CalculationResult[] parameters,
        IEnumerable<Thing> targets,
        [NotNullWhen(true)] out CalculationResult? calculationResult,
        out string? stringResult)
    {
        var res = TryGetParameter(ordinal, required, parameters, targets, out calculationResult);
        if (!res || calculationResult == null)
        {
            stringResult = null;
            return false;
        }

        stringResult = calculationResult.Value.Result as string;
        if (string.IsNullOrWhiteSpace(stringResult) && required)
        {
            calculationResult = CalculationResult.Error(CalculationErrorType.FormulaParse, $"Parameter {ordinal} is required.");
            return false;
        }

        calculationResult = CalculationResult.Success(stringResult, parameters[ordinal - 1].ResultType);
        return true;
    }

    public bool TryGetParameter(
        int ordinal,
        bool required,
        CalculationResult[] parameters,
        IEnumerable<Thing> targets,
        [NotNullWhen(true)] out CalculationResult? result)
    {
        if (ordinal > parameters.Length)
        {
            if (required)
            {
                result = CalculationResult.Error(CalculationErrorType.FormulaParse, $"Parameter {ordinal} not found, only {parameters.Length} were provided.");
                return false;
            }
            else
            {
                result = CalculationResult.Success(null, CalculationResultType.StaticValue);
                return false;
            }
        }

        var p = parameters[ordinal - 1];
        if (p.IsError)
        {
            result = p;
            return false;
        }

        switch (p.ResultType)
        {
            case CalculationResultType.StaticValue:
            case CalculationResultType.FunctionResult:
                if (p.Result == null && required)
                {
                    result = CalculationResult.Error(CalculationErrorType.FormulaParse, $"Parameter {ordinal} is required.");
                    return false;
                }
                result = p;
                return true;
            case CalculationResultType.PropertyValue:
                var propName = p.Result as string;
                if (string.IsNullOrWhiteSpace(propName))
                {
                    result = CalculationResult.Error(CalculationErrorType.FormulaParse, $"Property name was not specified.");
                    return false;
                }

                var targetsList = targets.ToArray();
                if (targetsList.Length != 1)
                {
                    result = CalculationResult.Error(CalculationErrorType.FormulaParse, $"One and only one target is supported.");
                    return false;
                }

                // Handle special built-ins
                if (string.Compare(propName, nameof(Thing.Name), StringComparison.OrdinalIgnoreCase) == 0)
                {
                    result = CalculationResult.Success(targetsList[0].Name, CalculationResultType.PropertyValue);
                    return true;
                }
                
                if (!targetsList[0].Properties.TryGetValue(propName, out object? propValue))
                {
                    result = CalculationResult.Error(CalculationErrorType.FormulaParse, $"Property [{propName}] is not found on thing '{targetsList[0].Name}' ({targetsList[0].Guid}).");
                    return false;
                }

                result = CalculationResult.Success(propValue, CalculationResultType.PropertyValue);
                return true;
            default:
                throw new NotImplementedException("???");
        }
    }
}