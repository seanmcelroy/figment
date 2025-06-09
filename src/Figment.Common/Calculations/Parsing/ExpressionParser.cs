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

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Figment.Common.Calculations.Parsing;

/// <summary>
/// A parser that converts a string expression into an abstract syntax tree
/// that can be calculated using <see cref="NodeBase.Evaluate(EvaluationContext)"/>.
/// </summary>
public class ExpressionParser
{
    /// <summary>
    /// Maximum input string size that this expression parser will attempt to parse.
    /// </summary>
    private const int MAX_INPUT_LENGTH = 32767;

    /// <summary>
    /// Maximum field name length.
    /// </summary>
    private const int MAX_FIELD_NAME_LENGTH = 256;

    /// <summary>
    /// Maximum recursion depth.
    /// </summary>
    private const int MAX_RECURSION_DEPTH = 100;

    /// <summary>
    /// Maximum string literal length.
    /// </summary>
    private const int MAX_STRING_LENGTH = 8192;

    /// <summary>
    /// Maximum identifier length.
    /// </summary>
    private const int MAX_IDENTIFIER_LENGTH = 256;

    /// <summary>
    /// Maximum number length.
    /// </summary>
    private const int MAX_NUMBER_LENGTH = 64;

    private string _input = string.Empty;
    private int _pos;
    private int _recursionDepth = 0;

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
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        if (input.Length > MAX_INPUT_LENGTH)
        {
            throw new ParseException("Input too long", 0);
        }

        _input = input.StartsWith('=') ? input[1..] : input;
        _pos = 0;
        return ParseExpression();
    }

    /// <summary>
    /// Attempts to convert an expression into an abstract syntax tree.
    /// </summary>
    /// <param name="input">The expression to parse.</param>
    /// <param name="expression">The root of the abstract syntax tree, if the <paramref name="input"/> could be parsed.</param>
    /// <returns>A value indicating whether the expression could be parsed.</returns>
    public static bool TryParse(string input, [NotNullWhen(true)] out NodeBase? expression)
    {
        var parser = new ExpressionParser();

        if (string.IsNullOrEmpty(input))
        {
            expression = null;
            return false;
        }

        try
        {
            expression = parser.Parse(input);
        }
        catch (ParseException)
        {
            expression = null;
            return false;
        }

        return true;
    }

    private NodeBase ParseExpression()
    {
        if (++_recursionDepth > MAX_RECURSION_DEPTH)
        {
            throw new ParseException("Expression too complex; recursion limit reached.", _pos);
        }

        try
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
        finally
        {
            _recursionDepth--;
        }
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
        int charCount = 0;
        while (Peek() != ']')
        {
            if (++charCount > MAX_FIELD_NAME_LENGTH)
            {
                throw new ParseException("Field name too long", _pos);
            }

            sb.Append(Next());
        }

        Expect(']');
        return new FieldRefNode(sb.ToString());
    }

    private LiteralNode ParseSingleQuotedString()
    {
        Expect('\'');
        var sb = new StringBuilder();
        int charCount = 0;
        while (Peek() != '\'' && Peek() != '\0')
        {
            if (++charCount > MAX_STRING_LENGTH)
            {
                throw new ParseException("String literal too long", _pos);
            }

            sb.Append(Next());
        }

        // Handle missing terminator
        if (Peek() == '\0')
        {
            throw new ParseException("Unterminated string literal", _pos);
        }

        Expect('\'');
        return new LiteralNode(sb.ToString());
    }

    private LiteralNode ParseDoubleQuotedString()
    {
        Expect('"');
        var sb = new StringBuilder();
        int charCount = 0;
        while (Peek() != '"' && Peek() != '\0')
        {
            if (++charCount > MAX_STRING_LENGTH)
            {
                throw new ParseException("String literal too long", _pos);
            }

            sb.Append(Next());
        }

        // Handle missing terminator
        if (Peek() == '\0')
        {
            throw new ParseException("Unterminated string literal", _pos);
        }

        Expect('"');
        return new LiteralNode(sb.ToString());
    }

    private LiteralNode ParseNumber()
    {
        var sb = new StringBuilder();
        int charCount = 0;
        bool anySeen = false;
        bool digitSeen = false;
        bool decimalSeen = false;
        while (true)
        {
            if (++charCount > MAX_NUMBER_LENGTH)
            {
                throw new ParseException("Number literal too long", _pos);
            }

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
        int charCount = 0;
        while (char.IsLetterOrDigit(Peek()))
        {
            if (++charCount > MAX_IDENTIFIER_LENGTH)
            {
                throw new ParseException("Identifier too long", _pos);
            }

            sb.Append(Next());
        }

        return sb.ToString();
    }
}