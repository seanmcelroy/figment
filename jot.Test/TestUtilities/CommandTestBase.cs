/*
Figment
Copyright (C) 2025  Sean McElroy

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Figment.Common.Data;
using Figment.Common.Errors;
using Figment.Data.Memory;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace jot.Test.TestUtilities;

[TestClass]
public abstract class CommandTestBase
{
    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    protected MemoryStorageProvider StorageProvider { get; private set; } = null!;
    protected TestConsole TestConsole { get; private set; } = null!;
    protected CommandApp App { get; private set; } = null!;
    protected string TestDataDirectory { get; private set; } = null!;

    [TestInitialize]
    public virtual void Setup()
    {
        // Create test data directory
        TestDataDirectory = Path.Combine(Path.GetTempPath(), "jot.Test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(TestDataDirectory);

        // Setup memory storage provider
        StorageProvider = new MemoryStorageProvider();

        // Setup test console
        TestConsole = new TestConsole();

        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddSingleton<IStorageProvider>(StorageProvider);
        services.AddSingleton(StorageProvider);
        services.AddSingleton<IErrorProvider, TestErrorProvider>();
        
        ServiceProvider = services.BuildServiceProvider();

        // Setup ambient contexts
        AmbientStorageContext.StorageProvider = StorageProvider;
        AmbientErrorContext.Provider = ServiceProvider.GetRequiredService<IErrorProvider>();

        // Setup command app
        App = new CommandApp(new TypeRegistrar(ServiceProvider));
        ConfigureCommands(App);
    }

    [TestCleanup]
    public virtual void Cleanup()
    {
        // Clean up test data directory
        if (Directory.Exists(TestDataDirectory))
        {
            Directory.Delete(TestDataDirectory, true);
        }

        // Reset ambient contexts
        AmbientStorageContext.StorageProvider = null;
        AmbientErrorContext.Provider = new DefaultConsoleErrorProvider();

        ServiceProvider?.Dispose();
    }

    protected virtual void ConfigureCommands(CommandApp app)
    {
        // Override in derived classes to configure specific commands
    }

    protected async Task<CommandAppResult> ExecuteCommandAsync(params string[] args)
    {
        return await App.RunAsync(args);
    }

    protected string CreateTestCsvFile(string content, string? fileName = null)
    {
        fileName ??= $"test_{Guid.NewGuid()}.csv";
        var filePath = Path.Combine(TestDataDirectory, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    protected string CreateTestJsonFile(string content, string? fileName = null)
    {
        fileName ??= $"test_{Guid.NewGuid()}.json";
        var filePath = Path.Combine(TestDataDirectory, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }
}