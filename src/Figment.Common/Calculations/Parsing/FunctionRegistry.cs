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

using Figment.Common.Calculations.Functions;

namespace Figment.Common.Calculations.Parsing;

/// <summary>
/// An centralized repository of built-in functions that can be invoked by expressions
/// parsed with <see cref="ExpressionParser"/> into <see cref="FunctionNode"/> objects
/// in the abstract syntax tree by <see cref="ExpressionParser.Parse(string)"/>.
/// </summary>
internal static class FunctionRegistry
{
    private static readonly Dictionary<string, Func<EvaluationContext, NodeBase[], ExpressionResult>> Functions = new()
    {
        {
            DateDiff.IDENTIFIER, static (ctx, args) => new DateDiff().Evaluate(ctx, args)
        },
        {
            False.IDENTIFIER, static (ctx, args) => new False().Evaluate(ctx, args)
        },
        {
            Floor.IDENTIFIER, static (ctx, args) => new Floor().Evaluate(ctx, args)
        },
        {
            If.IDENTIFIER, static (ctx, args) => new If().Evaluate(ctx, args)
        },
        {
            Len.IDENTIFIER, static (ctx, args) => new Len().Evaluate(ctx, args)
        },
        {
            Lower.IDENTIFIER, static (ctx, args) => new Lower().Evaluate(ctx, args)
        },
        {
            Now.IDENTIFIER, static (ctx, args) => new Now().Evaluate(ctx, args)
        },
        {
            Null.IDENTIFIER, static (ctx, args) => new Null().Evaluate(ctx, args)
        },
        {
            Today.IDENTIFIER, static (ctx, args) => new Today().Evaluate(ctx, args)
        },
        {
            Trim.IDENTIFIER, static (ctx, args) => new Trim().Evaluate(ctx, args)
        },
        {
            True.IDENTIFIER, static (ctx, args) => new True().Evaluate(ctx, args)
        },
        {
            Upper.IDENTIFIER, static (ctx, args) => new Upper().Evaluate(ctx, args)
        },
    };

    /// <summary>
    /// Invokes a built-in function.
    /// </summary>
    /// <param name="name">The name of the built-in function.</param>
    /// <param name="context">The <see cref="EvaluationContext"/> used to invoke the function.</param>
    /// <param name="args">The arguments supplied to the function.</param>
    /// <returns>The results of the evaluation of a built-in function.</returns>
    public static ExpressionResult Invoke(string name, EvaluationContext context, NodeBase[] args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (!Functions.TryGetValue(name.ToUpperInvariant(), out var func))
        {
            return ExpressionResult.Error(CalculationErrorType.FormulaParse, $"Unknown function '{name}'");
        }

        return func(context, args);
    }
}