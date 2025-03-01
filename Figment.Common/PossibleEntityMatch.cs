namespace Figment.Common;

public readonly record struct PossibleEntityMatch(Reference Reference, object Entity)
{
    public Reference Reference { get; init; } = Reference;
    public object Entity { get; init; } = Entity;

    public override string ToString()
    {
        return Reference.Type switch
        {
            Reference.ReferenceType.Schema => $"Schema '{((Schema)Entity).Name}'",
            Reference.ReferenceType.Thing => $"Thing '{((Thing)Entity).Name}'",
            _ => base.ToString() ?? string.Empty,
        };
    }
}