using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Figment.Common.Calculations.Functions;

public abstract class FunctionBase
{
    public abstract CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> targets);

    public bool TryGetDateParameter(
        int ordinal,
        bool required,
        CalculationResult[] parameters,
        IEnumerable<Thing> targets,
        out CalculationResult calculationResult,
        out DateTime? dateResult)
    {
        var res = TryGetStringParameter(ordinal, required, parameters, targets, out calculationResult, out string? stringResult);
        if (!res)
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

        // Unparsable and required - try parsing as a date like yyyy-mm-dd, etc.
        if (DateTimeOffset.TryParseExact(stringResult, SchemaDateField._formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset resultAsDto))
        {
            calculationResult = CalculationResult.Success(resultAsDto.DateTime, parameters[ordinal - 1].ResultType);
            dateResult = resultAsDto.DateTime;
            return true;
        }

        // That didn't work, so try to parse it as a functional date.
        if (DateUtility.TryParseFunctionalDateValue(stringResult, out double resultAsFdv)
            && DateUtility.TryParseDate(resultAsFdv, out DateTime resultAsConverteDate))
        {
            calculationResult = CalculationResult.Success(resultAsConverteDate, parameters[ordinal - 1].ResultType);
            dateResult = resultAsConverteDate;
            return true;
        }

        calculationResult = CalculationResult.Error(CalculationErrorType.BadValue, $"Cannot parse {stringResult} as a date.");
        dateResult = null;
        return false;
    }

    public bool TryGetDoubleParameter(
        int ordinal,
        bool required,
        CalculationResult[] parameters,
        IEnumerable<Thing> targets,
        out CalculationResult calculationResult,
        out double doubleResult)
    {
        var res = TryGetStringParameter(ordinal, required, parameters, targets, out calculationResult, out string? stringResult);
        if (!res)
        {
            doubleResult = 0;
            return false;
        }

        // Not supplied and not required, technically success.
        if (string.IsNullOrEmpty(stringResult) && !required)
        {
            calculationResult = CalculationResult.Success(null, parameters[ordinal - 1].ResultType);
            doubleResult = 0;
            return true;
        }

        // Unparsable and required - try parsing as a date like yyyy-mm-dd, etc.
        if (double.TryParse(stringResult, out doubleResult))
        {
            calculationResult = CalculationResult.Success(doubleResult, parameters[ordinal - 1].ResultType);
            return true;
        }

        calculationResult = CalculationResult.Error(CalculationErrorType.BadValue, $"Cannot parse {stringResult} as a double.");
        return false;
    }

    public bool TryGetStringParameter(
        int ordinal,
        bool required,
        CalculationResult[] parameters,
        IEnumerable<Thing> targets,
        out CalculationResult calculationResult,
        out string? stringResult)
    {
        var res = TryGetParameter(ordinal, required, parameters, targets, out calculationResult);
        if (!res)
        {
            stringResult = null;
            return false;
        }

        stringResult = calculationResult.Result?.ToString();
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
        [NotNullWhen(true)] out CalculationResult result)
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

                // Okay, now try to find the property by name
                var properties = targetsList[0].GetPropertyByName(propName, CancellationToken.None).ToBlockingEnumerable().ToImmutableArray();
                if (properties.Length == 1)
                {
                    result = CalculationResult.Success(properties[0].Value, CalculationResultType.PropertyValue);
                    return true;
                }
                else if (properties.Length > 1)
                {
                    result = CalculationResult.Error(CalculationErrorType.FormulaParse, $"Multiple properties named [{propName}] found on thing '{targetsList[0].Name}' ({targetsList[0].Guid}).  The properties were: {properties.Select(p => p.TruePropertyName).Aggregate((c, n) => $"{c},{n}")}");
                    return false;
                }

                result = CalculationResult.Error(CalculationErrorType.FormulaParse, $"Property [{propName}] is not found on thing '{targetsList[0].Name}' ({targetsList[0].Guid}).");
                return false;
            default:
                throw new NotImplementedException("???");
        }
    }
}