using System.Diagnostics;
using Spectre.Console;

namespace Figment;

public class SchemaPhoneField(string Name) : SchemaTextField(Name)
{
    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("phone");

    public override Task<string?> GetMarkedUpFieldValue(object? value, CancellationToken cancellationToken)
    {
        if (value == null)
            return Task.FromResult(default(string?));

        var str = value as string;

        if (Debugger.IsAttached 
            || !AnsiConsole.Profile.Capabilities.Links 
            || str?.IndexOfAny(['[', ']']) > -1)
            return Task.FromResult(str); // No link wrapping.

        return Task.FromResult((string?)$"[link=tel:{value}]{value}[/]");
    }
}