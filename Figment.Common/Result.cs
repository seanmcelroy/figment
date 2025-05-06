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

namespace Figment.Common;

public readonly record struct Result<TSuccess, TError>(TSuccess? Success, TError? Error)
{
    public bool IsSuccess => Success != null;

    public Result<TSuccess, TError> Ok([DisallowNull] TSuccess value)
    {
        return new Result<TSuccess, TError>(value, default);
    }

    public Result<TSuccess, TError> Err([DisallowNull] TError error)
    {
        return new Result<TSuccess, TError>(default, error);
    }
}