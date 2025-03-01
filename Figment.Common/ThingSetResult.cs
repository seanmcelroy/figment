namespace Figment.Common;

/// <summary>
/// The result of the <see cref="Thing.Set"/> operation
/// </summary>
/// <param name="Success">True if the operation was successful, otherwise false</param>
public readonly record struct ThingSetResult(bool Success)
{

}