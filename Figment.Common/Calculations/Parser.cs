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

namespace Figment.Common.Calculations;

/// <summary>
/// The parser class translates formulas into functional parse trees which can be invoked to result in a <see cref="CalculationResult"/>.
/// </summary>
public static class Parser
{
    /// <summary>
    /// This method parses a function definition into a <see cref="Func"/> that can be calculated.
    /// </summary>
    /// <param name="formula">The text formula to parse into a function.</param>
    /// <returns>A tuple indicating whether the parsing was successful, a message if it was not,
    /// and a parsed functon tree that can be invoked with a thing as the input if it was.</returns>
    /// <example>
    /// =TODAY()\r\n
    /// =LOWER(UPPER(LOWER("HELLO")))\r\n
    /// The <paramref name="formula"/> is similar in form to how spreadsheets use formulas.
    /// </example>
    public static (bool success, string? message, Func<IEnumerable<Thing>, CalculationResult>? root) ParseFormula(string formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
        {
            return (false, "Formula is not defined.", null);
        }

        if (!formula.StartsWith('='))
        {
            return (false, "Formulas must start with the equal sign.", null);
        }

        var depth = 0;
        var pos = 1;
        var (success, message, root) = ParseFormulaInternal(formula, ref pos, ref depth);
        if (depth != -1)
        {
            return (false, "Formula parse error: Uneven grouping", null);
        }

        if (pos != formula.Length)
        {
            return (false, "Formula parse error: Incomplete processing of formula", null);
        }

        if (success && root == null)
        {
            return (false, "Formula parse error: Missing parse tree on success", null);
        }

        return new(success, message, success ? t => root!(t) : null);
    }

    private static (bool success, string? message, Func<IEnumerable<Thing>, CalculationResult>? root) ParseFormulaInternal(string formula, ref int pos, ref int depth)
    {
        // Example LOWER(UPPER(LOWER("HELLO")))
        List<Func<IEnumerable<Thing>, CalculationResult>> parameters = [];
        Func<IEnumerable<Thing>, CalculationResult[], CalculationResult>? nextFunction = null;
        CalculationResult WhatToReturn(IEnumerable<Thing> t)
        {
            return nextFunction == null
                ? CalculationResult.Error(CalculationErrorType.InternalError, "nextFunction undefined")
                : nextFunction(t, [.. parameters.Select(p => p(t))]);
        }

        while (pos < formula.Length)
        {
            var nextLeftParen = formula.IndexOf('(', pos);
            var nextRightParen = formula.IndexOf(')', pos);
            var nextDoubleQuotation = formula.IndexOf('"', pos);
            var nextSingleQuotation = formula.IndexOf('\'', pos);
            var nextBracket = formula.IndexOf('[', pos);
            var nextValidNumber = formula.IndexOfAny(['-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.']);

            if (formula[pos] == ',' || formula[pos] == ' ')
            {
                // Skip commas and whitespace.
                pos++;
                continue;
            }

            string? nextToken;
            if (nextRightParen == pos) // Ending a capture with a spurious right parenthesis
            {
                nextToken = ")";
            }
            else if (nextDoubleQuotation == pos) // Ending a capture with a spurious right parenthesis
            {
                nextToken = "\"";
                var quotationStartPos = pos;
                pos++;
                nextDoubleQuotation = formula.IndexOf('"', pos);
                if (nextDoubleQuotation == -1)
                {
                    return (false, $"Unterminated double-quoted string starts at position {quotationStartPos}", null);
                }

                var quoted = formula[pos..nextDoubleQuotation];
                pos = nextDoubleQuotation + 1;

                // debug Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Quotation from {quotationStartPos} to {pos - 1}: {quoted}");
                // Do not adjust depth unless we hit a right parenth after this.
                if (formula.Length > pos && formula[pos] == ')')
                {
                    depth--;
                }

                return (true, "static string", t => CalculationResult.Success(quoted, CalculationResultType.StaticValue));
            }
            else if (nextSingleQuotation == pos) // Ending a capture with a spurious right parenthesis
            {
                // Same as before
                nextToken = "\'";
                var quotationStartPos = pos;
                pos++;
                nextSingleQuotation = formula.IndexOf('\'', pos);
                if (nextSingleQuotation == -1)
                {
                    return (false, $"Unterminated single-quoted string starts at position {quotationStartPos}", null);
                }

                var quoted = formula[pos..nextSingleQuotation];
                pos = nextSingleQuotation + 1;

                // debug Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Quotation from {quotationStartPos} to {pos - 1}: {quoted}");
                // Do not adjust depth, just return.
                // Do not adjust depth unless we hit a right parenth after this.
                if (formula.Length > pos && formula[pos] == ')')
                {
                    depth--;
                }

                return (true, "static string", t => CalculationResult.Success(quoted, CalculationResultType.StaticValue));
            }
            else if (nextBracket == pos)
            {
                // Same as before
                nextToken = "]";
                var openingBracketStartPos = pos;
                pos++;
                nextBracket = formula.IndexOf(']', pos);
                if (nextBracket == -1)
                {
                    return (false, $"Unterminated bracketed expression starts at position {openingBracketStartPos}", null);
                }

                var bracketed = formula[pos..nextBracket];
                pos = nextBracket + 1;

                // debug Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Bracketed expression from {closingBracketStartPos} to {pos - 1}: {bracketed}");
                // Do not adjust depth, just return.
                // Do not adjust depth unless we hit a right parenth after this.
                if (formula.Length > pos && formula[pos] == ')')
                {
                    depth--;
                }

                return (true, "property value", t => CalculationResult.Success(bracketed, CalculationResultType.PropertyValue));
            }
            else if (nextValidNumber == pos)
            {
                var numberStartPos = pos;
                pos++;

                // '-' is not included here since it cannot be the SECOND entry after the first.
                char[] validNextNumberCharacters = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ','];
                while (pos < formula.Length
                    && validNextNumberCharacters.Contains(formula[pos]))
                {
                    // A decimal can only appear once.
                    if (formula[pos] == '.')
                    {
                        validNextNumberCharacters = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ','];
                    }

                    pos++;
                }

                var nextNonNumber = pos;

                var numberString = formula[numberStartPos..nextNonNumber];

                object val;
                if (numberString.IndexOf('.') > -1)
                {
                    if (double.TryParse(numberString, out double d))
                    {
                        val = d;
                    }
                    else
                    {
                        return (false, $"Unable to parse potential number '{numberString}' as a double at position {numberStartPos}", null);
                    }
                }
                else
                {
                    if (ulong.TryParse(numberString, out ulong u))
                    {
                        val = u;
                    }
                    else
                    {
                        return (false, $"Unable to parse potential number '{numberString}' as a u64 at position {numberStartPos}", null);
                    }
                }

                if (formula.Length > pos && formula[pos] == ')')
                {
                    depth--;
                }

                return (true, "static number", t => CalculationResult.Success(val, CalculationResultType.StaticValue));
            }
            else
            {
                nextToken = nextLeftParen == -1 ? null : formula[pos..(nextLeftParen + 1)];
                if (nextToken != null)
                {
                    switch (nextToken.ToLowerInvariant())
                    {
                        case "datediff(":
                            nextFunction = (t, p) => new DateDiff().Evaluate(p, t);
                            break;
                        case "floor(":
                            nextFunction = (t, p) => new Floor().Evaluate(p, t);
                            break;
                        case "len(":
                            nextFunction = (t, p) => new Len().Evaluate(p, t);
                            break;
                        case "lower(":
                            nextFunction = (t, p) => new Lower().Evaluate(p, t);
                            break;
                        case "now(":
                            nextFunction = (t, p) => new Now().Evaluate(p, t);
                            break;
                        case "null(":
                            nextFunction = (t, p) => new Null().Evaluate(p, t);
                            break;
                        case "today(":
                            nextFunction = (t, p) => new Today().Evaluate(p, t);
                            break;
                        case "trim(":
                            nextFunction = (t, p) => new Trim().Evaluate(p, t);
                            break;
                        case "upper(":
                            nextFunction = (t, p) => new Upper().Evaluate(p, t);
                            break;
                        case "(":
                            // Let this fall through.
                            // This occurs when there's an extra ( grouping in front of the token
                            // like in =(TODAY())
                            nextFunction = (t, p) => new NoOp().Evaluate(p, t);
                            break;
                        default:
                            return (false, $"Unable to parse token: {nextToken}", null);
                    }
                }
            }

            if (formula.Length > pos + (nextToken?.Length ?? 0)
             && formula[pos + (nextToken?.Length ?? 0)] == ')')
            {
                // End capture
                // debug Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Captured {nextToken[..^1]}");
                pos += (nextToken?.Length - 1 ?? 0) + 2;
                depth--;
                return new(true, null, WhatToReturn);
            }

            // A nested function
            // debug Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Starting capture {nextToken}");
            pos += (nextToken?.Length - 1 ?? 0) + 1;
            depth++;

            var startingDepth = depth;
            (bool success, string? message, Func<IEnumerable<Thing>, CalculationResult>? root) sub = default;
            while (startingDepth == depth)
            {
                sub = ParseFormulaInternal(formula, ref pos, ref depth);
                if (!sub.success)
                {
                    return sub;
                }

                if (sub.root == null)
                {
                    return (false, $"Internal error: sub.root undefined at formula: {formula}, pos:{pos}, depth:{depth}", null);
                }

                parameters.Add(sub.root);
            }

            // Handle closing parenthesis
            if (pos < formula.Length && formula[pos] == ')')
            {
                // debug Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Ending capture {nextToken}");
                pos++;
                depth--;
                return new(true, null, WhatToReturn);
            }

            pos++;
        }

        return (false, $"Ran out of groupings at position {pos}", null);
    }

    /// <summary>
    /// Combines the <see cref="ParseFormula"/> over a string <paramref name="formula"/> step
    /// with an invocation over <see cref="Thing"/> entities provided in <paramref name="targets"/>.
    /// </summary>
    /// <param name="formula">The text formula to parse.</param>
    /// <param name="targets">The target(s) over which to calculate the result.</param>
    /// <returns>A single result from the calculation.</returns>
    public static CalculationResult Calculate(string formula, params Thing[] targets)
    {
        var (success, message, root) = ParseFormula(formula);
        if (!success)
        {
            return CalculationResult.Error(CalculationErrorType.FormulaParse, message ?? "Error when parsing formula");
        }

        if (root == null)
        {
            return CalculationResult.Error(CalculationErrorType.InternalError, message ?? "Internal error, root was null");
        }

        return root.Invoke(targets);
    }
}