using System.Diagnostics.CodeAnalysis;

namespace Figment.Common.Calculations.Functions;

public static class DateUtility
{
    public static readonly DateTime TwentiethCentry = new DateTime(1900, 01, 01, 0, 0, 0, DateTimeKind.Utc).Date;

    public static bool TryParseDate(double functionalDateValue, out DateTime dateTime)
    {
        dateTime = TwentiethCentry.AddDays(functionalDateValue);
        return true;
    }

    public static bool TryParseFunctionalDateValue([NotNullWhen(true)] DateTime? dateTime, out double fdv)
    {
        if (dateTime == null) {
            fdv = 0;
            return false;
        }

        TimeSpan ts = dateTime.Value - TwentiethCentry;
        fdv = ts.TotalDays;
        return true;
    }

    public static bool TryParseFunctionalDateValue([NotNullWhen(true)] string? dateTime, out double fdv)
    {
        if (DateTime.TryParse(dateTime, out DateTime dt))
        {
            var result = TryParseFunctionalDateValue(dt, out double fdv2);
            fdv = fdv2;
            return result;
        }

        if (double.TryParse(dateTime, out double x))
        {
            fdv = x;
            return true;
        }

        fdv = 0;
        return false;
    }
}