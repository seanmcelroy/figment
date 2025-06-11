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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

namespace jot;

/// <inheritdoc/>
public sealed class TypeRegistrar(IHostBuilder builder, IHost host) : ITypeRegistrar
{
    private readonly IHostBuilder _builder = builder;

    /// <summary>
    /// Gets the host that was built in this registrar.
    /// </summary>
    internal IHost Host { get; init; } = host;

    /// <inheritdoc/>
    public ITypeResolver Build()
    {
        return new TypeResolver(Host);
    }

    /// <inheritdoc/>
#pragma warning disable IL2092 // 'DynamicallyAccessedMemberTypes' on the parameter of method don't match overridden parameter of method. All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
    public void Register(
        Type service,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.PublicFields |
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type implementation)
    {
        _builder.ConfigureServices((_, services) => services.AddSingleton(service, implementation));
    }
#pragma warning restore IL2092 // 'DynamicallyAccessedMemberTypes' on the parameter of method don't match overridden parameter of method. All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.

    /// <inheritdoc/>
    public void RegisterInstance(Type service, object implementation)
    {
        _builder.ConfigureServices((_, services) => services.AddSingleton(service, implementation));
    }

    /// <inheritdoc/>
    public void RegisterLazy(Type service, Func<object> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        _builder.ConfigureServices((_, services) => services.AddSingleton(service, _ => func()));
    }
}