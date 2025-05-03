using System.Text;

namespace Figment.Common.Calculations.Parsing;

public class ExpressionParser
{
    private string _input;
    private int _pos;

    private char Peek() => _pos >= _input.Length ? '\0' : _input[_pos];

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

    public NodeBase Parse(string input)
    {
        _input = input.StartsWith('=') ? input[1..] : input;
        _pos = 0;
        return ParseExpression();
    }

    private NodeBase ParseExpression()
    {
        var left = ParseTerm();
        while (Match('&'))
        {
            var right = ParseTerm();
            left = new ConcatNode(left, right);
        }

        return left;
    }

    private NodeBase ParseTerm()
    {
        SkipWhitespace();
        if (Peek() == '[')
        {
            return ParseField();
        }

        if (Peek() == '"')
        {
            return ParseString();
        }

        if (char.IsLetter(Peek()))
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

    private LiteralNode ParseString()
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

    private NodeBase ParseFunction()
    {
        var name = ParseIdentifier();
        Expect('(');
        var args = new List<NodeBase>();
        if (Peek() != ')')
        {
            do
            {
                args.Add(ParseExpression());
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