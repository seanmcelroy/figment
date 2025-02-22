namespace Figment;

public class Link(string Guid, string SourceGuid, string DestinationGuid)
{
    private const string NameIndexFileName = $"_link.names.csv";

    public string Guid { get; init; } = Guid;
    public string SourceGuid { get; init; } = SourceGuid;
    public string DestinationGuid { get; init; } = DestinationGuid;
}