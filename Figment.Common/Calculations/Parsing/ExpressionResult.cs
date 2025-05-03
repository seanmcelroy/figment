namespace Figment.Common.Calculations.Parsing;

public readonly record struct ExpressionResult
{
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
    public bool TryConvertString(out string? result)
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

        result = Result.ToString();
        return true;
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