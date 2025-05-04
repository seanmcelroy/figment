using System.Text;

namespace Figment.Common.Calculations.Parsing;

/// <summary>
/// A parser that converts a string expression into an abstract syntax tree
/// that can be calculated using <see cref="NodeBase.Evaluate(EvaluationContext)"/>.
/// </summary>
public class ExpressionParser
{
    private string _input = string.Empty;
    private int _pos;

    private char Peek(int count = 1)
    {
        var idx = _pos + count - 1;
        return idx >= _input.Length ? '\0' : _input[idx];
    }

    private char Next() => _input[_pos++];

    private bool Match(char c)
    {
        if (Peek() == c)
        {
            _pos++;
            return true;
        }

        return false;
    }

    private void Expect(char c)
    {
        if (!Match(c))
        {
            throw new ParseException($"Expected '{c}' but found '{Peek()}'", _pos);
        }
    }

    private void SkipWhitespace()
    {
        while (char.IsWhiteSpace(Peek()))
        {
            _pos++;
        }
    }

    /// <summary>
    /// Converts an expression into an abstract syntax tree, or throws a <see cref="ParseException"/>
    /// if it could not be parsed.
    /// </summary>
    /// <param name="input">The expression to parse.</param>
    /// <returns>A <see cref="NodeBase"/> implementation at the root of the abstract syntax tree.</returns>
    public NodeBase Parse(string input)
    {
        _input = input.StartsWith('=') ? input[1..] : input;
        _pos = 0;
        return ParseExpression();
    }

    private NodeBase ParseExpression()
    {
        var left = ParseTerm();
        SkipWhitespace();

        var peeked = Peek();
        if (peeked == '&')
        {
            while (Match('&'))
            {
                var right = ParseTerm();
                left = new ConcatNode(left, right);
            }
        }
        else if (peeked == '=')
        {
            while (Match('='))
            {
                var right = ParseTerm();
                left = new LogicalEqualsNode(left, right);
            }
        }

        return left;
    }

    private NodeBase ParseTerm()
    {
        SkipWhitespace();

        // Handle extraneous enclosing parenthesis.
        if (Match('('))
        {
            var inner = ParseExpression();
            Expect(')');
            return inner;
        }

        char peeked = Peek();

        if (peeked == '[')
        {
            return ParseField();
        }

        if (peeked == '\'')
        {
            return ParseSingleQuotedString();
        }

        if (peeked == '"')
        {
            return ParseDoubleQuotedString();
        }

        if (peeked == '-' || char.IsNumber(peeked))
        {
            return ParseNumber();
        }

        if (char.IsLetter(peeked))
        {
            return ParseFunction();
        }

        throw new ParseException($"Unexpected character: {Peek()}", _pos);
    }

    private FieldRefNode ParseField()
    {
        Expect('[');
        var sb = new StringBuilder();
        while (Peek() != ']')
        {
            sb.Append(Next());
        }

        Expect(']');
        return new FieldRefNode(sb.ToString());
    }

    private LiteralNode ParseSingleQuotedString()
    {
        Expect('\'');
        var sb = new StringBuilder();
        while (Peek() != '\'')
        {
            sb.Append(Next());
        }

        Expect('\'');
        return new LiteralNode(sb.ToString());
    }

    private LiteralNode ParseDoubleQuotedString()
    {
        Expect('"');
        var sb = new StringBuilder();
        while (Peek() != '"')
        {
            sb.Append(Next());
        }

        Expect('"');
        return new LiteralNode(sb.ToString());
    }

    private LiteralNode ParseNumber()
    {
        var sb = new StringBuilder();
        bool anySeen = false;
        bool digitSeen = false;
        bool decimalSeen = false;
        while (true)
        {
            var peeked = Peek();

            // Negative symbol
            if (!anySeen)
            {
                anySeen = true;
                if (peeked == '-')
                {
                    sb.Append(Next());
                    continue;
                }
            }

            // Thousands separator
            if (peeked == ',')
            {
                if (!digitSeen)
                {
                    throw new ParseException("Digit separator seen before digit seen", _pos);
                }

                // The next 3 need to be numbers.
                if (char.IsDigit(Peek(2))
                    && char.IsDigit(Peek(3))
                    && char.IsDigit(Peek(4)))
                {
                    Next();
                    continue; // Skip over separators.
                }

                break; // Simply the end of the number, with a trailing comma (not a thousands separator).
            }

            if (peeked == '.')
            {
                if (decimalSeen)
                {
                    throw new ParseException("Decimal already observed in number", _pos);
                }

                decimalSeen = true;
                sb.Append(Next());
                continue;
            }

            if (char.IsDigit(peeked))
            {
                digitSeen = true;
                sb.Append(Next());
                continue;
            }

            break;
        }

        if (int.TryParse(sb.ToString(), out int i))
        {
            return new LiteralNode(i);
        }

        if (double.TryParse(sb.ToString(), out double d))
        {
            return new LiteralNode(d);
        }

        throw new ParseException("Unexpected termination of number", _pos);
    }

    private FunctionNode ParseFunction()
    {
        var name = ParseIdentifier();
        Expect('(');
        var args = new List<NodeBase>();
        if (Peek() != ')')
        {
            do
            {
                SkipWhitespace();
                args.Add(ParseExpression());
                SkipWhitespace();
            }
            while (Match(','));
        }

        Expect(')');
        return new FunctionNode(name, args);
    }

    private string ParseIdentifier()
    {
        var sb = new StringBuilder();
        while (char.IsLetterOrDigit(Peek()))
        {
            sb.Append(Next());
        }

        return sb.ToString();
    }
}