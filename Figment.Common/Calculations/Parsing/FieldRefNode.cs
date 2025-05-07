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

namespace Figment.Common.Calculations.Parsing;

/// <summary>
/// A type of node that retrieves the value of a named property from the <see cref="EvaluationContext"/>.
/// </summary>
/// <param name="FieldName">The name of the field to retrieve from the <see cref="EvaluationContext"/>.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class FieldRefNode(string FieldName) : NodeBase
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <inheritdoc/>
    public override ExpressionResult Evaluate(EvaluationContext context) =>
        context.GetField(FieldName);
}