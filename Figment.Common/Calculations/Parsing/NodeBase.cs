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
/// An abstract base class from which all nodes in the abstract syntax tree derive.
/// </summary>
public abstract class NodeBase
{
    /// <summary>
    /// Evaluates a node to produce a result from the expression given the supplied <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The context given to the node to generate its result.</param>
    /// <returns>An expression result indicating whether the evaluation was functionally successful, and if so, the result.</returns>
    public abstract ExpressionResult Evaluate(EvaluationContext context);

    /// <summary>
    /// Evalutes a node with mocked parameter values for the given <paramref name="schema"/>.
    /// </summary>
    /// <param name="schema">The schema over which to generate a context with mocked parameters for the purposes of attempting an evaluation. </param>
    /// <param name="expressionResult">An expression result indicating whether the evaluation was functionally successful, and if so, the result.</param>
    /// <returns>A value indicating whether the evaluation would have been successful for things of the supplied schema.</returns>
    /// <remarks>
    /// The purpose of this method is to allow formulas to be tested against fields actually available on a schema, without an instance a <see cref="Thing"/>.
    /// </remarks>
    public bool TryEvaluate(Schema schema, out ExpressionResult expressionResult)
    {
        var context = new EvaluationContext(schema);
        expressionResult = Evaluate(context);
        return expressionResult.IsSuccess;
    }
}