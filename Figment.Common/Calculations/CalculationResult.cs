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

namespace Figment.Common.Calculations;

/// <summary>
/// A result of a parse tree created by the <see cref="Parser"/>.
/// </summary>
/// <param name="ErrorType">The success or specific error type this result represents.</param>
/// <param name="Message">The human-readable error message that describes the failure, if this calculation resulted in an error.</param>
/// <param name="Result">The output value of the calculation, if this calculation was successful.</param>
/// <param name="ResultType">The type of value stored in the <see cref="Result"/>.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public readonly record struct CalculationResult(CalculationErrorType ErrorType, string? Message, object? Result, CalculationResultType ResultType)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// Gets the success or specific error type this result represents.
    /// </summary>
    public CalculationErrorType ErrorType { get; init; } = ErrorType;

    /// <summary>
    /// Gets the human-readable error message that describes the failure, if this calculation resulted in an error.
    /// </summary>
    public string? Message { get; init; } = Message;

    /// <summary>
    /// Gets the output value of the calculation, if this calculation was successful.
    /// </summary>
    public object? Result { get; init; } = Result;

    /// <summary>
    /// Gets the type of value stored in the <see cref="Result"/>.
    /// </summary>
    public CalculationResultType ResultType { get; init; } = ResultType;

    /// <summary>
    /// Gets a value indicating whether the calculation resulted in an error.
    /// </summary>
    public bool IsError => ErrorType != CalculationErrorType.Success;

    /// <summary>
    /// Creates a <see cref="CalculationResult"/> that represents a successful termination.
    /// </summary>
    /// <param name="result">The output value of the calculation.</param>
    /// <param name="resultType">The type of value represented by <paramref name="result"/>.</param>
    /// <returns>The result of the calculation.</returns>
    public static CalculationResult Success(object? result, CalculationResultType resultType) => new(CalculationErrorType.Success, null, result, resultType);

    /// <summary>
    /// Creates a <see cref="CalculationResult"/> that represents a failed termination.
    /// </summary>
    /// <param name="error">The error experienced during the calculation.</param>
    /// <param name="message">A human-readable error message that describes the failure.</param>
    /// <returns>The error result of the calculation.</returns>
    public static CalculationResult Error(CalculationErrorType error, string message) => new(error, message, null, CalculationResultType.Error);
}