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

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Figment.Common.Calculations.Functions;

namespace Figment.Common.Calculations.Parsing;

/// <summary>
/// The result of a call to <see cref="ExpressionParser.Parse(string)"/>.
/// </summary>
public readonly record struct ExpressionResult : IEquatable<ExpressionResult>
{
    /// <summary>
    /// True.
    /// </summary>
    public static readonly ExpressionResult TRUE = new()
    {
        IsSuccess = true,
        ErrorType = CalculationErrorType.Success,
        Message = "TRUE",
        Result = true,
    };

    /// <summary>
    /// False.
    /// </summary>
    public static readonly ExpressionResult FALSE = new()
    {
        IsSuccess = true,
        ErrorType = CalculationErrorType.Success,
        Message = "FALSE",
        Result = false,
    };

    /// <summary>
    /// Null.
    /// </summary>
    public static readonly ExpressionResult NULL = new()
    {
        IsSuccess = true,
        ErrorType = CalculationErrorType.Success,
        Message = "NULL",
        Result = null,
    };

    /// <summary>
    /// Gets a value indicating whether the evaluation completed without throwing an error.
    /// </summary>
    required public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets a value indicating the error, if one was encountered when attempting to evaluation an expression.
    /// </summary>
    required public CalculationErrorType ErrorType { get; init; }

    /// <summary>
    /// Gets the optional message provided with the expression result.
    /// </summary>
    required public string? Message { get; init; }

    /// <summary>
    /// Gets the resulting value of the evaluation, if successful.
    /// </summary>
    /// <remarks>
    /// Some evaluations may return null.  For this reason, the value of <see cref="IsSuccess"/>
    /// indicates whether an evaluation was successful, not solely a non-null result in this property.
    /// </remarks>
    required public object? Result { get; init; }

    /// <summary>
    /// A helper method that provides a successful <see cref="ExpressionResult"/> given the resulting value.
    /// </summary>
    /// <param name="result">The resulting value that was successfully evaluated.</param>
    /// <returns>An <see cref="ExpressionResult"/> that sets <see cref="IsSuccess"/> to true and the <see cref="Result"/> to the <paramref name="result"/> value.</returns>
    public static ExpressionResult Success(object? result)
    {
        if (result == null)
        {
            return NULL;
        }

        if (result is ExpressionResult er)
        {
            // Do it this way so we don't ever wrap Success(Success())
            return new()
            {
                IsSuccess = true,
                ErrorType = CalculationErrorType.Success,
                Message = er.Message,
                Result = er.Result,
            };
        }

        return new()
        {
            IsSuccess = true,
            ErrorType = CalculationErrorType.Success,
            Message = null,
            Result = result,
        };
    }

    /// <summary>
    /// A helper method that provides an error representation of a <see cref="ExpressionResult"/>.
    /// </summary>
    /// <param name="errorType">The type of error encountered.</param>
    /// <param name="message">The human-readable reason the error was encountered.</param>
    /// <returns>An <see cref="ExpressionResult"/> that sets <see cref="IsSuccess"/> to false and the <see cref="ErrorType"/> to the <paramref name="errorType"/> value.</returns>
    public static ExpressionResult Error(CalculationErrorType errorType, string? message) => new()
    {
        IsSuccess = false,
        ErrorType = errorType,
        Message = message,
        Result = null,
    };

    /// <inheritdoc/>
    public bool Equals(ExpressionResult other)
    {
        if (!IsSuccess || !other.IsSuccess)
        {
            return false;
        }

        // Null never equals null.
        if (Result == null || other.Result == null)
        {
            return false;
        }

        return Result.Equals(other.Result);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (Result == null)
        {
            return base.GetHashCode();
        }

        return Result.GetHashCode();
    }

    /// <summary>
    /// Attempts to convert the <see cref="Result"/> into a boolean value, including whether it is
    /// 'truthy' if it is not a native boolean.
    /// </summary>
    /// <param name="result">The boolean representation of the result.</param>
    /// <returns>A value indicating whteher the result could be coerced into a boolean.</returns>
    public bool TryConvertBoolean(out bool result)
    {
        if (!IsSuccess)
        {
            result = false;
            return false;
        }

        if (Result is bool booleanResult)
        {
            result = booleanResult;
            return true;
        }

        if (Result is int intResult)
        {
            result = intResult != 0;
            return true;
        }

        if (Result is string stringResult && SchemaBooleanField.TryParseBoolean(stringResult, out bool parsedBooleanResult))
        {
            result = parsedBooleanResult;
            return true;
        }

        result = false;
        return false;
    }

    /// <summary>
    /// Attempts to convert the <see cref="Result"/> into a double precision number.
    /// </summary>
    /// <param name="result">The double precision number representation of the result.</param>
    /// <returns>A value indicating whteher the result could be coerced into a double.</returns>
    public bool TryConvertDouble(out double result)
    {
        if (!IsSuccess)
        {
            result = double.MinValue;
            return false;
        }

        if (Result is double doubleResult)
        {
            result = doubleResult;
            return true;
        }

        if (Result is string stringResult && double.TryParse(stringResult, out double stringDouble))
        {
            result = stringDouble;
            return true;
        }

        result = double.MinValue;
        return false;
    }

    /// <summary>
    /// Attempts to convert the <see cref="Result"/> into a string.
    /// </summary>
    /// <param name="result">The string representation of the result.</param>
    /// <returns>A value indicating whteher the result could be coerced into a string.</returns>
    public bool TryConvertString([NotNullWhen(true)] out string? result)
    {
        if (!IsSuccess || Result == null)
        {
            result = null;
            return false;
        }

        if (Result is string s)
        {
            result = s;
            return true;
        }

        result = Result.ToString() ?? string.Empty;
        return true;
    }

    /// <summary>
    /// Attempts to convert the <see cref="Result"/> into a string.
    /// </summary>
    /// <param name="result">The string representation of the result.</param>
    /// <returns>A value indicating whteher the result could be coerced into a string.</returns>
    public bool TryConvertDateTime([NotNullWhen(true)] out DateTimeOffset? result)
    {
        if (!IsSuccess || Result == null)
        {
            result = null;
            return false;
        }

        if (Result is DateTimeOffset dto)
        {
            result = dto;
            return true;
        }

        if (Result is DateTime dt)
        {
            result = dt;
            return true;
        }

        // Serial value
        if (Result is double d && d > 0)
        {
            result = DateUtility.TwentiethCentry.AddDays(d);
            return true;
        }

        if (Result is not string s || string.IsNullOrWhiteSpace(s))
        {
            result = null;
            return false;
        }

        // Unparsable and required - try parsing as a date like yyyy-mm-dd, etc.
        if (DateTimeOffset.TryParseExact(s, SchemaDateField._completeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dto2))
        {
            result = dto2;
            return true;
        }

        // That didn't work, so try to parse it as a functional date.
        if (DateUtility.TryParseFunctionalDateValue(s, out double resultAsFdv)
            && DateUtility.TryParseDate(resultAsFdv, out DateTime resultAsConverteDate))
        {
            result = resultAsConverteDate;
            return true;
        }

        result = null;
        return false;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (IsSuccess)
        {
            return Result?.ToString() ?? string.Empty;
        }

        return ErrorType switch
        {
            CalculationErrorType.FormulaParse => "#ERR",
            CalculationErrorType.NotANumber => "#NAN",
            CalculationErrorType.DivisionByZero => "#DIV",
            CalculationErrorType.Recursion => "#REC",
            CalculationErrorType.BadValue => "#VALUE",
            _ => Message ?? "Unknown error",
        };
    }
}