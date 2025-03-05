using Figment.Common.Calculations.Functions;

namespace Figment.Common.Calculations;

public static class Parser
{
    public static (bool success, string? message, Func<IEnumerable<Thing>, CalculationResult>? root) ParseFormula(string formula)
    {
        // Example =TODAY()
        // Example =LOWER(UPPER(LOWER("HELLO")))
        if (string.IsNullOrWhiteSpace(formula))
            return (false, "Formula is not defined.", null);
        if (!formula.StartsWith('='))
            return (false, "Formulas must start with the equal sign.", null);

        var depth = 0;
        var pos = 1;
        var (success, message, root) = ParseFormulaInternal(formula, ref pos, ref depth);
        if (depth != -1)
            return (false, "Formula parse error: Uneven grouping", null);
        if (pos != formula.Length)
            return (false, "Formula parse error: Incomplete processing of formula", null);
        if (success && root == null)
            return (false, "Formula parse error: Missing parse tree on success", null);
        return new(success, message, success ? t => root!(t) : null);
    }

    private static (bool success, string? message, Func<IEnumerable<Thing>, CalculationResult>? root) ParseFormulaInternal(string formula, ref int pos, ref int depth)
    {
        // Example LOWER(UPPER(LOWER("HELLO")))
        List<Func<IEnumerable<Thing>, CalculationResult>> parameters = [];
        Func<IEnumerable<Thing>, CalculationResult[], CalculationResult> nextFunction = null;
        CalculationResult whatToReturn(IEnumerable<Thing> t) => nextFunction(t, [.. parameters.Select(p => p(t))]);

        while (pos < formula.Length)
        {
            var nextLeftParen = formula.IndexOf('(', pos);
            //if (nextLeftParen == -1)
            //    Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - No next left paren");
            var nextRightParen = formula.IndexOf(')', pos);
            var nextDoubleQuotation = formula.IndexOf('"', pos);
            var nextSingleQuotation = formula.IndexOf('\'', pos);
            var nextBracket = formula.IndexOf('[', pos);

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
                    return (false, $"Unterminated double-quoted string starts at position {quotationStartPos}", null);
                var quoted = formula[pos..nextDoubleQuotation];
                pos = nextDoubleQuotation + 1;

                Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Quotation from {quotationStartPos} to {pos - 1}: {quoted}");
                // Do not adjust depth unless we hit a right parenth after this.
                if (formula.Length > pos && formula[pos] == ')')
                    depth--;
                return (true, "static value", t => CalculationResult.Success(quoted, CalculationResultType.StaticValue));
            }
            else if (nextSingleQuotation == pos) // Ending a capture with a spurious right parenthesis
            {
                // Same as before
                nextToken = "\'";
                var quotationStartPos = pos;
                pos++;
                nextSingleQuotation = formula.IndexOf('\'', pos);
                if (nextSingleQuotation == -1)
                    return (false, $"Unterminated single-quoted string starts at position {quotationStartPos}", null);
                var quoted = formula[pos..nextSingleQuotation];
                pos = nextSingleQuotation + 1;

                Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Quotation from {quotationStartPos} to {pos - 1}: {quoted}");
                // Do not adjust depth, just return.
                // Do not adjust depth unless we hit a right parenth after this.
                if (formula.Length > pos && formula[pos] == ')')
                    depth--;
                return (true, "static value", t => CalculationResult.Success(quoted, CalculationResultType.StaticValue));
            }
            else if (nextBracket == pos)
            {
                // Same as before
                nextToken = "]";
                var closingBracketStartPos = pos;
                pos++;
                nextBracket = formula.IndexOf(']', pos);
                if (nextBracket == -1)
                    return (false, $"Unterminated bracketed expression starts at position {closingBracketStartPos}", null);
                var bracketed = formula[pos..nextBracket];
                pos = nextBracket + 1;

                Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Bracketed expression from {closingBracketStartPos} to {pos - 1}: {bracketed}");
                // Do not adjust depth, just return.
                // Do not adjust depth unless we hit a right parenth after this.
                if (formula.Length > pos && formula[pos] == ')')
                    depth--;
                return (true, "property value", t => CalculationResult.Success(bracketed, CalculationResultType.PropertyValue));
            }
            else
            {
                nextToken = nextLeftParen == -1 ? null : formula[pos..(nextLeftParen + 1)];
                if (nextToken != null)
                    switch (nextToken.ToLowerInvariant())
                    {
                        case "datediff(":
                            nextFunction = (t, p) => new DateDiff().Evaluate(p, t);
                            break;
                        case "floor(":
                            nextFunction = (t, p) => new Floor().Evaluate(p, t);
                            break;
                        case "lower(":
                            nextFunction = (t, p) => new Lower().Evaluate(p, t);
                            break;
                        case "now(":
                            nextFunction = (t, p) => new Now().Evaluate(p, t);
                            break;
                        case "today(":
                            nextFunction = (t, p) => new Today().Evaluate(p, t);
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

            if (formula.Length > pos + (nextToken?.Length ?? 0)
             && formula[pos + (nextToken?.Length ?? 0)] == ')')
            {
                // End capture
                Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Captured {nextToken[..^1]}");
                pos += (nextToken?.Length - 1 ?? 0) + 2;
                depth--;
                return new(true, null, whatToReturn);
            }

            // A nested function
            Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Starting capture {nextToken}");
            pos += (nextToken?.Length - 1 ?? 0) + 1;
            depth++;

            var startingDepth = depth;
            (bool success, string? message, Func<IEnumerable<Thing>, CalculationResult>? root) sub = default;
            while (startingDepth == depth)
            {
                sub = ParseFormulaInternal(formula, ref pos, ref depth);
                if (!sub.success)
                    return sub;
                parameters.Add(sub.root);
            }

            // Handle closing parenthesis
            if (pos < formula.Length && formula[pos] == ')')
            {
                Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Ending capture {nextToken}");
                pos++;
                depth--;
                return new(true, null, whatToReturn);
                //return sub;
            }
            pos++;
        }

        throw new InvalidOperationException($"Ran out of groupings at position {pos}!");
    }

    public static async Task<CalculationResult> CalculateAsync(string formula, params Thing[] targets)
    {
        var (success, message, root) = ParseFormula(formula);
        if (!success)
            return CalculationResult.Error(CalculationErrorType.FormulaParse, message ?? "Error when parsing formula");
        if (root == null)
            return CalculationResult.Error(CalculationErrorType.InternalError, message ?? "Internal error, root was null");

        var result = root.Invoke(targets);
        return result;
    }
}