/*
Figment
Copyright (C) 2025  Sean McElroy

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Figment.Common.Calculations.Parsing;

namespace Figment.Common.Calculations.Functions;

/// <summary>
/// The abstract base class from which all functions usable in formulas by the <see cref="Parser"/> derive.
/// </summary>
public abstract class FunctionBase
{
    /// <summary>
    /// Evaluates the function using the given input <paramref name="parameters"/> over the supplied <paramref name="targets"/>.
    /// </summary>
    /// <param name="parameters">The input parameters to provide for the funciton to perform its calculation.</param>
    /// <param name="targets">The targets over which the calculation should be preformed.</param>
    /// <returns>The outcome calculation result, whether a success or failure.</returns>
    public abstract CalculationResult Evaluate(CalculationResult[] parameters, IEnumerable<Thing> targets);

/// <summary>
    /// Evaluates the function using the given input <paramref name="arguments"/> over the supplied <paramref name="targets"/>.
    /// </summary>
    /// <param name="context">The context for the evaluation.</param>
    /// <param name="arguments">The arguments to provide for the funciton to perform its calculation.</param>
    /// <returns>The outcome calculation result, whether a success or failure.</returns>
    public abstract ExpressionResult Evaluate(EvaluationContext context, NodeBase[] arguments);

    /// <summary>
    /// Attempts to retrieve a parameter from the <paramref name="parameters"/> array as a boolean value.
    /// </summary>
    /// <param name="ordinal">The index of the <paramref name="parameters"/> array from which to attempt to parse the value.</param>
    /// <param name="required">A value indicating whether or not the parameter is required.</param>
    /// <param name="parameters">The array of parametes from which to parse a value.</param>
    /// <param name="targets">The things over which the function parse tree should operate. </param>
    /// <param name="calculationResult">The output result, which would either be a success if the parameter could be retrieved, or a failure with an error message.</param>
    /// <param name="booleanResult">The boolean value that was retrieved, if successful.</param>
    /// <returns>Whether or not the parameter could be retrieved.</returns>
    public bool TryGetBooleanParameter(
        int ordinal,
        bool required,
        CalculationResult[] parameters,
        IEnumerable<Thing> targets,
        out CalculationResult calculationResult,
        out bool? booleanResult)
    {
        var res = TryGetParameter(ordinal, required, parameters, targets, out calculationResult);
        if (!res)
        {
            booleanResult = null;
            return false;
        }

        if (calculationResult.Result is bool pb1)
        {
            booleanResult = pb1;
            return true;
        }

        var stringResult = calculationResult.Result?.ToString();
        if (string.IsNullOrWhiteSpace(stringResult) && required)
        {
            calculationResult = CalculationResult.Error(CalculationErrorType.FormulaParse, $"Parameter {ordinal} is required.");
            booleanResult = null;
            return false;
        }

        // Not supplied and not required, technically success.
        if (string.IsNullOrEmpty(stringResult) && !required)
        {
            booleanResult = null;
            return true;
        }

        if (SchemaBooleanField.TryParseBoolean(stringResult, out bool pb))
        {
            booleanResult = pb;
            return true;
        }

        calculationResult = CalculationResult.Error(CalculationErrorType.BadValue, $"Cannot parse {stringResult} as a boolean.");
        booleanResult = null;
        return false;
    }

    /// <summary>
    /// Attempts to retrieve a parameter from the <paramref name="parameters"/> array as a date.
    /// </summary>
    /// <param name="ordinal">The index of the <paramref name="parameters"/> array from which to attempt to parse the value.</param>
    /// <param name="required">A value indicating whether or not the parameter is required.</param>
    /// <param name="parameters">The array of parametes from which to parse a value.</param>
    /// <param name="targets">The things over which the function parse tree should operate. </param>
    /// <param name="calculationResult">The output result, which would either be a success if the parameter could be retrieved, or a failure with an error message.</param>
    /// <param name="dateResult">The date that was retrieved, if successful.</param>
    /// <returns>Whether or not the parameter could be retrieved.</returns>
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
        if (DateTimeOffset.TryParseExact(stringResult, SchemaDateField._completeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset resultAsDto))
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

    /// <summary>
    /// Attempts to retrieve a parameter from the <paramref name="parameters"/> array as a double.
    /// </summary>
    /// <param name="ordinal">The index of the <paramref name="parameters"/> array from which to attempt to parse the value.</param>
    /// <param name="required">A value indicating whether or not the parameter is required.</param>
    /// <param name="parameters">The array of parametes from which to parse a value.</param>
    /// <param name="targets">The things over which the function parse tree should operate. </param>
    /// <param name="calculationResult">The output result, which would either be a success if the parameter could be retrieved, or a failure with an error message.</param>
    /// <param name="doubleResult">The double that was retrieved, if successful.</param>
    /// <returns>Whether or not the parameter could be retrieved.</returns>
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

        // Unparsable and required - try parsing as a double.
        if (double.TryParse(stringResult, out doubleResult))
        {
            calculationResult = CalculationResult.Success(doubleResult, parameters[ordinal - 1].ResultType);
            return true;
        }

        calculationResult = CalculationResult.Error(CalculationErrorType.BadValue, $"Cannot parse {stringResult} as a double.");
        return false;
    }

    /// <summary>
    /// Attempts to retrieve a parameter from the <paramref name="parameters"/> array as a string.
    /// </summary>
    /// <param name="ordinal">The index of the <paramref name="parameters"/> array from which to attempt to parse the value.</param>
    /// <param name="required">A value indicating whether or not the parameter is required.</param>
    /// <param name="parameters">The array of parametes from which to parse a value.</param>
    /// <param name="targets">The things over which the function parse tree should operate. </param>
    /// <param name="calculationResult">The output result, which would either be a success if the parameter could be retrieved, or a failure with an error message.</param>
    /// <param name="stringResult">The string that was retrieved, if successful.</param>
    /// <returns>Whether or not the parameter could be retrieved.</returns>
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

    /// <summary>
    /// Attempts to retrieve a parameter from the <paramref name="parameters"/> array as a generic result.
    /// </summary>
    /// <param name="ordinal">The index of the <paramref name="parameters"/> array from which to attempt to parse the value.</param>
    /// <param name="required">A value indicating whether or not the parameter is required.</param>
    /// <param name="parameters">The array of parametes from which to parse a value.</param>
    /// <param name="targets">The things over which the function parse tree should operate. </param>
    /// <param name="result">The value that was retrieved, if successful.</param>
    /// <returns>Whether or not the parameter could be retrieved.</returns>
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
                if (string.Equals(propName, nameof(Thing.Name), StringComparison.OrdinalIgnoreCase))
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
                throw new InvalidOperationException($"Unsupported result type {p.ResultType}");
        }
    }
}
