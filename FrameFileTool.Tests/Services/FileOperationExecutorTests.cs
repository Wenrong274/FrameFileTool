using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class FileOperationExecutorTests
{
    private readonly FileOperationExecutor _sut = new();

    [Fact]
    public void RenameFiles_只處理改名且無錯誤項目()
    {
        using var sandbox = TestDirectory.Create();
        var source = sandbox.WriteFile("a.png");
        var keep = sandbox.WriteFile("b.png");
        var error = sandbox.WriteFile("c.png");
        var excluded = sandbox.WriteFile("d.png");

        var items = new[]
        {
            MakePreview(source, "a.png", OperationActionKind.Rename, "renamed.png"),
            MakePreview(keep, "b.png", OperationActionKind.Keep, string.Empty),
            MakePreview(error, "c.png", OperationActionKind.Rename, "ignored.png", hasError: true),
            MakePreview(excluded, "d.png", OperationActionKind.Rename, "excluded.png", isIncluded: false),
        };

        var result = _sut.RenameFiles(items);

        result.SuccessCount.Should().Be(1);
        result.SkippedCount.Should().Be(3);
        result.Errors.Should().BeEmpty();
        File.Exists(Path.Combine(sandbox.Path, "renamed.png")).Should().BeTrue();
        File.Exists(keep).Should().BeTrue();
        File.Exists(error).Should().BeTrue();
        File.Exists(excluded).Should().BeTrue();
        File.Exists(Path.Combine(sandbox.Path, "excluded.png")).Should().BeFalse();
    }

    [Fact]
    public void RenameFiles_來源不存在_應回傳錯誤()
    {
        using var sandbox = TestDirectory.Create();
        var missingPath = Path.Combine(sandbox.Path, "missing.png");

        var result = _sut.RenameFiles(
        [
            MakePreview(missingPath, "missing.png", OperationActionKind.Rename, "renamed.png"),
        ]);

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("檔案不存在");
    }

    [Fact]
    public void RenameFiles_目標檔名包含路徑_應拒絕執行並保留原檔()
    {
        using var sandbox = TestDirectory.Create();
        var source = sandbox.WriteFile("a.png");

        var result = _sut.RenameFiles(
        [
            MakePreview(source, "a.png", OperationActionKind.Rename, @"..\evil.png"),
        ]);

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("目標檔名不安全");
        File.Exists(source).Should().BeTrue();
    }

    [Fact]
    public void RenameFiles_第二階段失敗_應嘗試還原原始檔名()
    {
        using var sandbox = TestDirectory.Create();
        var source = sandbox.WriteFile("a.png");
        Directory.CreateDirectory(Path.Combine(sandbox.Path, "blocked.png"));

        var result = _sut.RenameFiles(
        [
            MakePreview(source, "a.png", OperationActionKind.Rename, "blocked.png"),
        ]);

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().ContainSingle()
            .Which.Should().ContainAll("最終改名失敗", "已還原原始檔名");
        File.Exists(source).Should().BeTrue();
        Directory.EnumerateFiles(sandbox.Path, ".__FrameFileTool_*.tmp")
            .Should().BeEmpty();
    }

    [Fact]
    public void DeleteToRecycleBin_HasError項目_應略過不執行()
    {
        using var sandbox = TestDirectory.Create();
        var source = sandbox.WriteFile("a.png");

        var result = _sut.DeleteToRecycleBin(
        [
            MakePreview(source, "a.png", OperationActionKind.Delete, string.Empty, hasError: true),
        ]);

        result.SuccessCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().BeEmpty();
        File.Exists(source).Should().BeTrue();
    }

    [Fact]
    public void DeleteToRecycleBin_取消勾選項目_應略過不執行()
    {
        using var sandbox = TestDirectory.Create();
        var source = sandbox.WriteFile("a.png");

        var result = _sut.DeleteToRecycleBin(
        [
            MakePreview(source, "a.png", OperationActionKind.Delete, string.Empty, isIncluded: false),
        ]);

        result.SuccessCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().BeEmpty();
        File.Exists(source).Should().BeTrue();
    }

    private static OperationPreviewItem MakePreview(
        string fullPath,
        string originalName,
        OperationActionKind actionKind,
        string targetName,
        bool hasError = false,
        bool isIncluded = true) =>
        new()
        {
            FullPath = fullPath,
            OriginalName = originalName,
            ActionKind = actionKind,
            TargetName = targetName,
            HasError = hasError,
            IsIncluded = isIncluded,
        };

    private sealed class TestDirectory : IDisposable
    {
        private TestDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TestDirectory Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"FrameFileToolTests_{Guid.NewGuid():N}");

            Directory.CreateDirectory(path);
            return new TestDirectory(path);
        }

        public string WriteFile(string name)
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, "test");
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
