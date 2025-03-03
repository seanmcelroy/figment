using System.Diagnostics;
using Figment.Common.Calculations.Functions;

namespace Figment.Common.Calculations;

public static class Parser
{
    public static (bool success, string? message, Func<CalculationResult>? root) ParseFormula(string formula)
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
        return new(success, message, () => root());
    }

    private static (bool success, string? message, Func<CalculationResult>? root) ParseFormulaInternal(string formula, ref int pos, ref int depth)
    {
        // Example LOWER(UPPER(LOWER("HELLO")))
        List<Func<CalculationResult>> parameters = [];
        Func<CalculationResult[], CalculationResult> nextFunction = null;
        CalculationResult whatToReturn() => nextFunction([.. parameters.Select(p => p())]);

        while (pos < formula.Length)
        {
            var nextLeftParen = formula.IndexOf('(', pos);
            if (nextLeftParen == -1)
                Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - No next left paren");
            var nextRightParen = formula.IndexOf(')', pos);

            string? nextToken;
            if (nextRightParen == pos) // Ending a capture with a spurious right parenthesis
            {
                nextToken = ")";
            }
            else
            {
                nextToken = nextLeftParen == -1 ? null : formula[pos..(nextLeftParen + 1)];
                //Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Next token is: {nextToken}");

                if (nextToken != null)
                    switch (nextToken.ToLowerInvariant())
                    {
                        case "lower(":
                            nextFunction = p => new Lower().Evaluate(p);
                            break;
                        case "today(":
                            nextFunction = p => new Today().Evaluate(p);
                            break;
                        case "(":
                            // Let this fall through.
                            // This occurs when there's an extra ( grouping in front of the token
                            // like in =(TODAY())
                            nextFunction = p => new NoOp().Evaluate(p);
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
            }

            /*if (formula.Length > pos + (nextToken?.Length ?? 0) + 1
                && formula[pos + (nextToken?.Length ?? 0) + 1] == '(')
            {
                // Begin capture
                pos += (nextToken?.Length ?? 0) + 2;
                depth++;
                var res = ParseFormulaInternal(formula, ref pos, ref depth);
                if (!res.success)
                    return res;
            }
            else*/
            if (formula.Length > pos + (nextToken?.Length ?? 0) 
             && formula[pos + (nextToken?.Length ?? 0) ] == ')')
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

            var sub = ParseFormulaInternal(formula, ref pos, ref depth);
            if (!sub.success)
                return sub;
            parameters.Add(sub.root);

            // Handle closing parenthesis
            if (pos < formula.Length && formula[pos] == ')') {
                Console.Error.WriteLine($"formula: {formula}, pos:{pos}, depth:{depth} - Ending capture {nextToken}");
                pos++;
                depth--;
                return sub;
            }
            //Debug.Assert(formula[pos] == ')');
            pos++;
        }

        throw new InvalidOperationException($"Ran out of groupings at position {pos}!");
    }

    public static async Task<CalculationResult> Calculate(string formula, Thing target)
    {
        return CalculationResult.Error(CalculationErrorType.FormulaParse, "???");
    }
}