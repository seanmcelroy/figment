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

using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

namespace jot;

/// <inheritdoc/>
public sealed class TypeResolver(IHost provider) : ITypeResolver, IDisposable
{
    private readonly IHost _host = provider ?? throw new ArgumentNullException(nameof(provider));

    /// <inheritdoc/>
    public object? Resolve(Type? type)
    {
        return type != null ? _host.Services.GetService(type) : null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // _host.Dispose();
    }
}