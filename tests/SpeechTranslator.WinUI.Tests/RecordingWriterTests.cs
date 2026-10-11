using System.Text;
using SpeechTranslatorWinUI.Core;

namespace SpeechTranslator.WinUI.Tests;

/// <summary>Tests transcript validation and exact append-only UTF-8 output in private folders.</summary>
public sealed class RecordingWriterTests
{
    /// <summary>Disables recording before inspecting even an invalid folder.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void Validate_BlankFilename_ReturnsNullWithoutRequiringOrCreatingFolder(string filename)
    {
        // Arrange
        using var directory = new TestDirectory();
        var missing = directory.GetPath("missing");
        var before = directory.Snapshot();
        var writer = new RecordingWriter();
        string?[] folders = [null, "relative", missing, directory.Root];

        foreach (var folder in folders)
        {
            // Act
            var path = writer.Validate(folder, filename);

            // Assert
            Assert.Null(path);
            directory.AssertUnchanged(before);
            Assert.False(Directory.Exists(missing));
        }
    }

    /// <summary>Returns the normalized path and preserves both directory inventory and existing bytes.</summary>
    [Theory]
    [InlineData("a", "a")]
    [InlineData("ABCxyz09_-", "ABCxyz09_-")]
    [InlineData("length199", "length199")]
    [InlineData("length200", "length200")]
    [InlineData(" \tMiXeD_9-\r\n", "MiXeD_9-")]
    public void Validate_ValidStem_ReturnsTxtPathWithoutCreatingTranscript(string filename, string stem)
    {
        // Arrange
        filename = ExpandLength(filename);
        stem = ExpandLength(stem);
        using var directory = new TestDirectory();
        File.WriteAllBytes(directory.GetPath("unrelated.bin"), [0, 255, 17]);
        Directory.CreateDirectory(directory.GetPath("existing-subfolder"));
        var expected = directory.GetPath(stem + ".txt");
        var before = directory.Snapshot();
        var writer = new RecordingWriter();

        // Act
        var path = writer.Validate(directory.Root, filename);

        // Assert
        Assert.Equal(expected, path);
        Assert.False(File.Exists(expected));
        directory.AssertUnchanged(before);

        // Arrange
        File.WriteAllBytes(expected, [239, 187, 191, 65, 10, 255]);
        before = directory.Snapshot();

        // Act
        var existingPath = writer.Validate(directory.Root, filename);

        // Assert
        Assert.Equal(expected, existingPath);
        directory.AssertUnchanged(before);
    }

    /// <summary>Rejects invalid stems before checking a missing or unselected folder.</summary>
    [Theory]
    [InlineData("length201")]
    [InlineData("in ternal")]
    [InlineData("日本")]
    [InlineData("name.txt")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("C:\\drive")]
    [InlineData("\\\\server\\share")]
    [InlineData("a:b")]
    [InlineData("a?b")]
    [InlineData("a*b")]
    [InlineData("a|b")]
    [InlineData("a<b")]
    [InlineData("a>b")]
    [InlineData("a\"b")]
    [InlineData("a.b")]
    [InlineData("a\tb")]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    [InlineData("a\0b")]
    public void Validate_InvalidStem_ThrowsArgumentExceptionBeforeFolderChecks(string filename)
    {
        // Arrange
        using var directory = new TestDirectory();
        filename = ExpandLength(filename);
        var writer = new RecordingWriter();
        var before = directory.Snapshot();
        string?[] folders = [null, directory.GetPath("missing"), directory.Root];

        foreach (var folder in folders)
        {
            // Act
            var error = Record.Exception(() => writer.Validate(folder, filename));

            // Assert
            Assert.IsType<ArgumentException>(error);
            directory.AssertUnchanged(before);
        }
    }

    /// <summary>Rejects every reserved device name regardless of letter casing.</summary>
    [Theory]
    [InlineData("CON")]
    [InlineData("prn")]
    [InlineData("AuX")]
    [InlineData("nUl")]
    [InlineData("CoM1")]
    [InlineData("CoM2")]
    [InlineData("CoM3")]
    [InlineData("CoM4")]
    [InlineData("CoM5")]
    [InlineData("CoM6")]
    [InlineData("CoM7")]
    [InlineData("CoM8")]
    [InlineData("CoM9")]
    [InlineData("lPt1")]
    [InlineData("lPt2")]
    [InlineData("lPt3")]
    [InlineData("lPt4")]
    [InlineData("lPt5")]
    [InlineData("lPt6")]
    [InlineData("lPt7")]
    [InlineData("lPt8")]
    [InlineData("lPt9")]
    public void Validate_ReservedNames_RejectsCaseInsensitively(string filename)
    {
        // Arrange
        using var directory = new TestDirectory();
        var writer = new RecordingWriter();
        var before = directory.Snapshot();

        // Act
        var error = Record.Exception(() => writer.Validate(directory.Root, " \t" + filename + " "));

        // Assert
        Assert.IsType<ArgumentException>(error);
        directory.AssertUnchanged(before);
    }

    /// <summary>Accepts names immediately outside the exact reserved-name grammar.</summary>
    [Theory]
    [InlineData("COM0")]
    [InlineData("COM10")]
    [InlineData("LPT0")]
    [InlineData("LPT10")]
    [InlineData("CON_")]
    [InlineData("_NUL")]
    [InlineData("AUX-")]
    public void Validate_NearReservedNames_AreAccepted(string filename)
    {
        // Arrange
        using var directory = new TestDirectory();
        var writer = new RecordingWriter();
        var before = directory.Snapshot();

        // Act
        var path = writer.Validate(directory.Root, filename);

        // Assert
        Assert.Equal(directory.GetPath(filename + ".txt"), path);
        Assert.False(File.Exists(path));
        directory.AssertUnchanged(before);
    }

    /// <summary>Requires an existing fully qualified directory without implicit creation.</summary>
    [Theory]
    [InlineData("null")]
    [InlineData("empty")]
    [InlineData("relative")]
    [InlineData("root-relative")]
    [InlineData("missing")]
    [InlineData("file")]
    public void Validate_NonblankFilenameAndInvalidFolder_ThrowsIOException(string kind)
    {
        // Arrange
        using var directory = new TestDirectory();
        File.WriteAllBytes(directory.GetPath("file"), [65, 66]);
        string? folder = kind switch
        {
            "null" => null,
            "empty" => "",
            "relative" => "relative",
            "root-relative" => "\\SpeechTranslator.WinUI.Tests-missing",
            "missing" => directory.GetPath("missing"),
            "file" => directory.GetPath("file"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var before = directory.Snapshot();
        var writer = new RecordingWriter();

        // Act
        var error = Record.Exception(() => writer.Validate(folder, "valid"));

        // Assert
        Assert.IsType<IOException>(error);
        directory.AssertUnchanged(before);
        Assert.False(Directory.Exists(directory.GetPath("missing")));
    }

    /// <summary>Preserves ordered pairs and any prefix as exact BOM-free UTF-8 without leaked handles.</summary>
    [Theory]
    [InlineData("", "", false)]
    [InlineData("原文🌍", "訳文é", false)]
    [InlineData("source\r\nline\n", "訳\n別\r\n", false)]
    [InlineData("", "", true)]
    [InlineData("原文🌍", "訳文é", true)]
    [InlineData("source\r\nline\n", "訳\n別\r\n", true)]
    public void Append_NewAndExistingFile_AppendsUnicodePairsAsBomlessUtf8(string source, string target, bool existing)
    {
        // Arrange
        using var directory = new TestDirectory();
        var path = directory.GetPath("transcript.txt");
        var prefix = existing ? new byte[] { 239, 187, 191, 80, 82, 69, 10 } : [];
        if (existing) File.WriteAllBytes(path, prefix);
        var writer = new RecordingWriter();
        var newline = Environment.NewLine;
        var firstBytes = new UTF8Encoding(false).GetBytes(source + newline + target + newline + newline);

        // Act
        writer.Append(path, new Translation(source, target));

        // Assert
        Assert.Equal(prefix.Concat(firstBytes).ToArray(), File.ReadAllBytes(path));
        using (var handle = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Assert.Equal(prefix.Length + firstBytes.Length, handle.Length);

        // Arrange
        var second = new Translation("second原文", "second訳文");
        var secondBytes = new UTF8Encoding(false).GetBytes("second原文" + newline + "second訳文" + newline + newline);

        // Act
        writer.Append(path, second);

        // Assert
        Assert.Equal(prefix.Concat(firstBytes).Concat(secondBytes).ToArray(), File.ReadAllBytes(path));
        using var reopened = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.Equal(prefix.Length + firstBytes.Length + secondBytes.Length, reopened.Length);
    }

    /// <summary>Propagates filesystem failures without alternate output, truncation or parent creation.</summary>
    [Theory]
    [InlineData("locked")]
    [InlineData("directory")]
    [InlineData("missing-parent")]
    public void Append_LockedPathOrMissingParent_PropagatesFailure(string kind)
    {
        // Arrange
        using var directory = new TestDirectory();
        var original = directory.GetPath("locked.txt");
        File.WriteAllBytes(original, [80, 82, 69, 255]);
        var before = directory.Snapshot();
        var path = kind switch
        {
            "locked" => original,
            "directory" => directory.Root,
            "missing-parent" => directory.GetPath(Path.Combine("missing", "transcript.txt")),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var writer = new RecordingWriter();
        Exception? error;

        // Act
        using (var handle = kind == "locked" ? new FileStream(original, FileMode.Open, FileAccess.ReadWrite, FileShare.None) : null)
            error = Record.Exception(() => writer.Append(path, new Translation("new source", "new target")));

        // Assert
        if (kind == "directory")
            Assert.IsType<UnauthorizedAccessException>(error);
        else
            Assert.IsAssignableFrom<IOException>(error);
        directory.AssertUnchanged(before);
        Assert.False(Directory.Exists(directory.GetPath("missing")));
    }

    private static string ExpandLength(string value) => value.StartsWith("length", StringComparison.Ordinal)
        ? new string('a', int.Parse(value.AsSpan(6))) : value;
}
