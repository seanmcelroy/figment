using Figment;

namespace jot;

public readonly record struct PossibleMatch(Reference Reference, object Entity)
{
    public Reference Reference { get; init; } = Reference;
    public object Entity { get; init; } = Entity;

    public override string ToString()
    {
        switch (Reference.Type)
        {
            case Reference.ReferenceType.Schema:
                return $"Schema '{((Schema)Entity).Name}'";
            case Reference.ReferenceType.Thing:
                return $"Thing '{((Thing)Entity).Name}'";
            default:
                return base.ToString() ?? string.Empty;
        }
    }
}