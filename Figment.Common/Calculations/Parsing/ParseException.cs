namespace Figment.Common.Calculations.Parsing;

public class ParseException : Exception
{
    public int Position { get; init; }

    public ParseException(string message, int position)
        : base(message)
    {
        Position = position;
    }
}