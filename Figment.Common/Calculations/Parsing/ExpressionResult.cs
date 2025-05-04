using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Figment.Common.Calculations.Functions;

namespace Figment.Common.Calculations.Parsing;

public readonly record struct ExpressionResult : IEquatable<ExpressionResult>
{
    /// <summary>
    /// True.
    /// </summary>
    public static readonly ExpressionResult TRUE = Success(true);

    /// <summary>
    /// False.
    /// </summary>
    public static readonly ExpressionResult FALSE = Success(false);

    required public bool IsSuccess { get; init; }
    required public CalculationErrorType ErrorType { get; init; }
    required public string? Message { get; init; }
    required public object? Result { get; init; }

    public static ExpressionResult Success<T>(T result)
        => new()
        {
            IsSuccess = true,
            ErrorType = CalculationErrorType.Success,
            Message = null,
            Result = result,
        };

    public static ExpressionResult Error(CalculationErrorType errorType, string message) => new()
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

        if (Result is double d && d > 0) // Serial value
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