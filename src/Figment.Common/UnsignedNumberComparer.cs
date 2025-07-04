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

namespace Figment.Common;

using System.Collections;

/// <summary>
/// A comparer of two unsigned numbers that are of various underlying types,
/// but all of which can be compared as <see cref="ulong"/>.
/// </summary>
public class UnsignedNumberComparer : IComparer
{
    /// <summary>
    /// Gets the default implementation of this stateless comparer.
    /// </summary>
    public static readonly UnsignedNumberComparer Default = new();

    /// <inheritdoc/>
    public int Compare(object? x, object? y)
    {
        if (x == null && y == null)
        {
            return 0;
        }

        if (x == null)
        {
            return -1;
        }

        if (y == null)
        {
            return 1;
        }

        ulong xx, yy;

        if (x is ulong xul)
        {
            xx = xul;
        }
        else if (x is long xl)
        {
            xx = Convert.ToUInt64(xl);
        }
        else if (x is uint xui)
        {
            xx = Convert.ToUInt64(xui);
        }
        else if (x is int xi)
        {
            xx = Convert.ToUInt64(xi);
        }
        else
        {
            return -1;
        }

        if (y is ulong yul)
        {
            yy = yul;
        }
        else if (y is long yl)
        {
            yy = Convert.ToUInt64(yl);
        }
        else if (y is uint yui)
        {
            yy = Convert.ToUInt64(yui);
        }
        else if (y is int yi)
        {
            yy = Convert.ToUInt64(yi);
        }
        else
        {
            return 1;
        }

        return xx.CompareTo(yy);
    }
}