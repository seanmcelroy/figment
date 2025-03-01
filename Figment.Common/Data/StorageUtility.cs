namespace Figment.Common.Data;

public static class StorageUtility
{
    private static readonly AsyncLocal<IStorageProvider> _StorageProvider = new();

    public static IStorageProvider StorageProvider {
        get => _StorageProvider.Value;
        set => _StorageProvider.Value = value;
    }
}