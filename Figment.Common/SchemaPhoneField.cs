using System.Diagnostics;

namespace Figment.Common;

public class SchemaPhoneField(string Name) : SchemaTextField(Name)
{
    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("phone");
}