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