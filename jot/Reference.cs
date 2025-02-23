using System.ComponentModel;

namespace Figment;

public readonly record struct Reference
{
    public enum ReferenceType
    {
        [Description("Unknown")]
        Unknown = 0,
        [Description("Link")]
        Link = 1,
        [Description("Page")]
        Page = 2,
        [Description("Schema")]
        Schema = 3,
        [Description("Thing")]
        Thing = 4,
    }

    public static readonly Reference EMPTY = new Reference { Type = ReferenceType.Unknown, Guid = System.Guid.Empty.ToString() };

    public readonly ReferenceType Type { get; init; }
    public readonly string Guid { get; init; }

    public async Task<object?> LoadAsync(CancellationToken cancellationToken)
    {
        return Type switch
        {
            ReferenceType.Schema => await Schema.LoadAsync(Guid, cancellationToken),
            ReferenceType.Thing => await Thing.LoadAsync(Guid, cancellationToken),
            ReferenceType.Unknown => throw new NotImplementedException(),
            ReferenceType.Link => throw new NotImplementedException(),
            ReferenceType.Page => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };
    }

    public static implicit operator Reference(Link? l) => new() { Type = ReferenceType.Link, Guid = l?.Guid ?? string.Empty };
    public static implicit operator Reference(Schema? s) => new() { Type = ReferenceType.Schema, Guid = s?.Guid ?? string.Empty };
    public static implicit operator Reference(Thing? t) => new() { Type = ReferenceType.Thing, Guid = t?.Guid ?? string.Empty };
}