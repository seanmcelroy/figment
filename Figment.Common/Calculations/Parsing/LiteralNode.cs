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
/// This node holds a literal value, typically a constant in an expression or a constant
/// resulting from an evaluation.
/// </summary>
/// <param name="Value">The literal value the node contains.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class LiteralNode(object? Value) : NodeBase, IEquatable<LiteralNode>
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// A default instance that contains only 'null'.
    /// </summary>
    public static readonly LiteralNode NULL = new(default);

    private object? Value { get; } = Value;

    /// <inheritdoc/>
    public bool Equals(LiteralNode? other)
    {
        if (other == null)
        {
            return false;
        }

        if (Value == null || other.Value == null)
        {
            return false;
        }

        return Value.Equals(other.Value);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as LiteralNode);

    /// <inheritdoc/>
    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    /// <inheritdoc/>
    public override ExpressionResult Evaluate(EvaluationContext context) =>
        ExpressionResult.Success(Value);

    /// <inheritdoc/>
    public override string ToString() => Value?.ToString() ?? string.Empty;
}