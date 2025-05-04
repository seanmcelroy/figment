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

using Figment.Common.Calculations.Parsing;

namespace Figment.Common.Calculations.Functions;

/// <summary>
/// Returns 'null' as a native null value.
/// </summary>
public class Null : FunctionBase
{
    /// <summary>
    /// The identifier of this function.
    /// </summary>
    public const string IDENTIFIER = "NULL";

    /// <inheritdoc/>
    public override string Identifier => IDENTIFIER;

    /// <inheritdoc/>
    public override ExpressionResult Evaluate(EvaluationContext context, NodeBase[] arguments)
    {
        return ExpressionResult.Success(default(object?));
    }
}