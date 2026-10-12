namespace Tsubakimoto.SpeechTranslator.WinUIApp.Tests;

internal sealed class TestDirectory : IDisposable
{
    internal string Root { get; } = Path.Combine(Path.GetTempPath(), "Tsubakimoto.SpeechTranslator.WinUIApp.Tests", Guid.NewGuid().ToString("N"));

    internal TestDirectory() => Directory.CreateDirectory(Root);

    internal string GetPath(string name) => Path.Combine(Root, name);

    internal Dictionary<string, byte[]?> Snapshot() => Directory
        .EnumerateFileSystemEntries(Root, "*", SearchOption.AllDirectories)
        .ToDictionary(path => Path.GetRelativePath(Root, path), path => File.Exists(path) ? File.ReadAllBytes(path) : null);

    internal void AssertUnchanged(Dictionary<string, byte[]?> before)
    {
        var after = Snapshot();
        Assert.Equal(before.Keys.OrderBy(key => key), after.Keys.OrderBy(key => key));
        foreach (var entry in before)
            Assert.Equal(entry.Value, after[entry.Key]);
    }

    /// <summary>Deletes the private fixture tree after all owned handles have closed.</summary>
    public void Dispose()
    {
        if (Directory.Exists(Root))
            Directory.Delete(Root, recursive: true);
    }
}
