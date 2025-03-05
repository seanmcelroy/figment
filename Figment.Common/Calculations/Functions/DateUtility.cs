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