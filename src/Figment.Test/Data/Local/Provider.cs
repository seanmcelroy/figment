using Figment.Data.Local;

namespace Figment.Test.Data.Local;

[TestClass]
public sealed class Provider
{
    [TestMethod]
    public async Task Initialize_Null_Settings()
    {
        var sp = new LocalDirectoryStorageProvider();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await sp.InitializeAsync(null, CancellationToken.None));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [TestMethod]
    public async Task Initialize_Missing_DatabasePath_Settings()
    {
        var sp = new LocalDirectoryStorageProvider();
        await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await sp.InitializeAsync(new Dictionary<string, string>(), CancellationToken.None));
    }

    [TestMethod]
    public async Task Initialize_Null_DatabasePath_Settings()
    {
        var sp = new LocalDirectoryStorageProvider();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await sp.InitializeAsync(new Dictionary<string, string>() { { LocalDirectoryStorageProvider.SETTINGS_KEY_DB_PATH, null } }, CancellationToken.None));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [TestMethod]
    public async Task Initialize_Invalid_DatabasePath_Settings()
    {
        var sp = new LocalDirectoryStorageProvider();
        var res = await sp.InitializeAsync(new Dictionary<string, string>() { { LocalDirectoryStorageProvider.SETTINGS_KEY_DB_PATH, @"CWH3ATEVER" } }, CancellationToken.None);
        Assert.IsFalse(res);
    }
}