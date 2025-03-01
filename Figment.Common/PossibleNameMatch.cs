namespace Figment.Common;

public readonly record struct PossibleNameMatch(Reference Reference, string Name)
{
    public Reference Reference { get; init; } = Reference;
    public string Name { get; init; } = Name;

    public override string ToString() => Name;
}