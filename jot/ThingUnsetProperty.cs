namespace Figment;

public readonly record struct ThingUnsetProperty
{
    /// <summary>
    /// The property name rendered for display with any accompanying <see cref="Schema">
    /// </summary>
    /// <remarks>
    /// Such as Person.email
    /// </remarks>
    public required readonly string FullDisplayName { get; init; }
    /// <summary>
    /// The property name rendered for display
    /// </summary>
    /// <remarks>
    /// Such as email
    /// </remarks>
    public required readonly string SimpleDisplayName { get; init; }
    /// <summary>
    /// The Guid of the <see cref="Schema"> to which this property is associated
    /// </summary>
    public required readonly string SchemaGuid { get; init; }
    /// <summary>
    /// The name of the <see cref="Schema"> to which this property is associated
    /// </summary>
    public required readonly string SchemaName { get; init; }
    /// <summary>
    /// The field which is not set on the <see cref="Thing"/>
    /// </summary>
    public required readonly SchemaFieldBase Field { get; init; }
}