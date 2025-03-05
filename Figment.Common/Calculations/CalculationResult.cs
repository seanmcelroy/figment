namespace Figment.Common.Calculations;

public readonly record struct CalculationResult(CalculationErrorType ErrorType, string? Message, object? Result, CalculationResultType ResultType)
{
    public CalculationErrorType ErrorType { get; init; } = ErrorType;
    public string? Message { get; init; } = Message;
    public object? Result { get; init; } = Result;
    public CalculationResultType ResultType { get; init; } = ResultType;

    public bool IsError => ErrorType != CalculationErrorType.Success;

    public static CalculationResult Success(object? result, CalculationResultType resultType) => new(CalculationErrorType.Success, null, result, resultType);
    public static CalculationResult Error(CalculationErrorType error, string message) => new(error, message, null, CalculationResultType.Error);

}