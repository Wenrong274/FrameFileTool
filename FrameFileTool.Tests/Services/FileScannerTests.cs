using FluentAssertions;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class FileScannerTests
{
    private readonly FileScanner _sut = new();

    [Fact]
    public void Scan_資料夾不存在_應回傳空結果且無錯誤()
    {
        var result = _sut.Scan(@"C:\FrameFileTool\NotExists", [".png"], includeSubfolders: false);

        result.Files.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Scan_副檔名清單為空_應回傳空結果且無錯誤()
    {
        using var sandbox = TestDirectory.Create();
        sandbox.WriteFile("a.png");

        var result = _sut.Scan(sandbox.Path, [], includeSubfolders: false);

        result.Files.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Scan_副檔名應大小寫不敏感且只回傳指定類型()
    {
        using var sandbox = TestDirectory.Create();
        sandbox.WriteFile("a.PNG");
        sandbox.WriteFile("b.jpg");
        sandbox.WriteFile("c.txt");

        var result = _sut.Scan(sandbox.Path, ["png"], includeSubfolders: false);

        result.Files.Should().ContainSingle();
        result.Files[0].Name.Should().Be("a.PNG");
    }

    [Fact]
    public void Scan_副檔名可省略前置點且支援多個類型()
    {
        using var sandbox = TestDirectory.Create();
        sandbox.WriteFile("a.png");
        sandbox.WriteFile("b.JPG");
        sandbox.WriteFile("c.txt");

        var result = _sut.Scan(sandbox.Path, ["png", ".jpg"], includeSubfolders: false);

        result.Files.Select(file => file.Name).Should().Equal("a.png", "b.JPG");
    }

    [Fact]
    public void Scan_同一資料夾內應使用自然排序()
    {
        using var sandbox = TestDirectory.Create();
        sandbox.WriteFile("10.png");
        sandbox.WriteFile("2.png");
        sandbox.WriteFile("1.png");

        var result = _sut.Scan(sandbox.Path, [".png"], includeSubfolders: false);

        result.Files.Select(file => file.Name).Should().Equal("1.png", "2.png", "10.png");
    }

    [Fact]
    public void Scan_包含子資料夾時_應先依資料夾分組再各自自然排序()
    {
        using var sandbox = TestDirectory.Create();
        var folderA = sandbox.CreateDirectory("A");
        var folderB = sandbox.CreateDirectory("B");
        sandbox.WriteFile("2.png", folderA);
        sandbox.WriteFile("1.png", folderA);
        sandbox.WriteFile("2.png", folderB);
        sandbox.WriteFile("1.png", folderB);

        var result = _sut.Scan(sandbox.Path, [".png"], includeSubfolders: true);

        result.Files.Select(file => Path.GetRelativePath(sandbox.Path, file.FullPath))
            .Should()
            .Equal(
                Path.Combine("A", "1.png"),
                Path.Combine("A", "2.png"),
                Path.Combine("B", "1.png"),
                Path.Combine("B", "2.png"));
    }

    [Fact]
    public void Scan_未勾選包含子資料夾_不應回傳子資料夾檔案()
    {
        using var sandbox = TestDirectory.Create();
        var child = sandbox.CreateDirectory("Child");
        sandbox.WriteFile("root.png");
        sandbox.WriteFile("child.png", child);

        var result = _sut.Scan(sandbox.Path, [".png"], includeSubfolders: false);

        result.Files.Select(file => file.Name).Should().Equal("root.png");
    }

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
                $"FrameFileToolScannerTests_{Guid.NewGuid():N}");

            Directory.CreateDirectory(path);
            return new TestDirectory(path);
        }

        public string CreateDirectory(string name)
        {
            var path = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(path);
            return path;
        }

        public void WriteFile(string name, string? directory = null)
        {
            var folder = directory ?? Path;
            File.WriteAllText(System.IO.Path.Combine(folder, name), "test");
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
