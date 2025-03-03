namespace Figment.Common.Calculations;

public readonly record struct CalculationResult(CalculationErrorType ErrorType, string? Message, object? Result)
{
    public CalculationErrorType ErrorType { get; init; } = ErrorType;
    public string? Message { get; init; } = Message;
    public object? Result { get; init; } = Result;

    public bool IsError => ErrorType != CalculationErrorType.Success;

    public static CalculationResult Success(object? result) => new(CalculationErrorType.Success, null, result);
    public static CalculationResult Error(CalculationErrorType error, string message) => new(error, message, null);

}