using FluentAssertions;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class FileImportServiceTests
{
    private readonly FileImportService _sut = new(new FileScanner());

    [Fact]
    public void Import_單一支援檔案_應回傳檔案()
    {
        using var sandbox = TestDirectory.Create();
        var file = sandbox.WriteFile("a.png");

        var result = _sut.Import([file], [".png"], includeSubfolders: false, ExistingPaths());

        result.Files.Should().ContainSingle();
        result.Files[0].Name.Should().Be("a.png");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Import_多個檔案_應依資料夾與自然排序回傳()
    {
        using var sandbox = TestDirectory.Create();
        var file10 = sandbox.WriteFile("10.png");
        var file2 = sandbox.WriteFile("2.png");
        var file1 = sandbox.WriteFile("1.png");

        var result = _sut.Import([file10, file2, file1], ["png"], includeSubfolders: false, ExistingPaths());

        result.Files.Select(file => file.Name).Should().Equal("1.png", "2.png", "10.png");
    }

    [Fact]
    public void Import_資料夾_應匯入符合副檔名檔案()
    {
        using var sandbox = TestDirectory.Create();
        sandbox.WriteFile("a.png");
        sandbox.WriteFile("b.jpg");
        sandbox.WriteFile("c.txt");

        var result = _sut.Import([sandbox.Path], [".png", ".jpg"], includeSubfolders: false, ExistingPaths());

        result.Files.Select(file => file.Name).Should().Equal("a.png", "b.jpg");
    }

    [Fact]
    public void Import_資料夾且包含子資料夾_應匯入子資料夾檔案()
    {
        using var sandbox = TestDirectory.Create();
        var child = sandbox.CreateDirectory("Child");
        sandbox.WriteFile("root.png");
        sandbox.WriteFile("child.png", child);

        var result = _sut.Import([sandbox.Path], [".png"], includeSubfolders: true, ExistingPaths());

        result.Files.Select(file => Path.GetRelativePath(sandbox.Path, file.FullPath))
            .Should()
            .Equal("root.png", Path.Combine("Child", "child.png"));
    }

    [Fact]
    public void Import_資料夾且不包含子資料夾_不應匯入子資料夾檔案()
    {
        using var sandbox = TestDirectory.Create();
        var child = sandbox.CreateDirectory("Child");
        sandbox.WriteFile("root.png");
        sandbox.WriteFile("child.png", child);

        var result = _sut.Import([sandbox.Path], [".png"], includeSubfolders: false, ExistingPaths());

        result.Files.Select(file => file.Name).Should().Equal("root.png");
    }

    [Fact]
    public void Import_混合檔案與資料夾_應一起匯入()
    {
        using var sandbox = TestDirectory.Create();
        var folder = sandbox.CreateDirectory("Folder");
        var file = sandbox.WriteFile("root.png");
        sandbox.WriteFile("child.png", folder);

        var result = _sut.Import([file, folder], [".png"], includeSubfolders: false, ExistingPaths());

        result.Files.Select(item => item.Name).Should().Equal("root.png", "child.png");
    }

    [Fact]
    public void Import_不支援副檔名_應略過並回報錯誤()
    {
        using var sandbox = TestDirectory.Create();
        var file = sandbox.WriteFile("a.txt");

        var result = _sut.Import([file], [".png"], includeSubfolders: false, ExistingPaths());

        result.Files.Should().BeEmpty();
        result.Errors.Should().Contain(error => error.Contains("副檔名不支援"));
    }

    [Fact]
    public void Import_重複既有檔案_應略過並回報錯誤()
    {
        using var sandbox = TestDirectory.Create();
        var file = sandbox.WriteFile("a.png");

        var result = _sut.Import(
            [file],
            [".png"],
            includeSubfolders: false,
            ExistingPaths(file));

        result.Files.Should().BeEmpty();
        result.Errors.Should().Contain(error => error.Contains("已存在於清單"));
    }

    [Fact]
    public void Import_同一批拖放重複檔案_應只匯入一次()
    {
        using var sandbox = TestDirectory.Create();
        var file = sandbox.WriteFile("a.png");

        var result = _sut.Import([file, file], [".png"], includeSubfolders: false, ExistingPaths());

        result.Files.Should().ContainSingle();
        result.Errors.Should().Contain(error => error.Contains("已存在於清單"));
    }

    [Fact]
    public void Import_路徑不存在_應回報錯誤()
    {
        using var sandbox = TestDirectory.Create();
        var path = Path.Combine(sandbox.Path, "missing.png");

        var result = _sut.Import([path], [".png"], includeSubfolders: false, ExistingPaths());

        result.Files.Should().BeEmpty();
        result.Errors.Should().Contain(error => error.Contains("路徑不存在"));
    }

    [Fact]
    public void Import_未選擇副檔名_應回報錯誤()
    {
        using var sandbox = TestDirectory.Create();
        var file = sandbox.WriteFile("a.png");

        var result = _sut.Import([file], [], includeSubfolders: false, ExistingPaths());

        result.Files.Should().BeEmpty();
        result.Errors.Should().ContainSingle(error => error.Contains("未選擇任何副檔名"));
    }

    private static IReadOnlySet<string> ExistingPaths(params string[] paths) =>
        paths.ToHashSet(StringComparer.OrdinalIgnoreCase);

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
                $"FrameFileToolImportTests_{Guid.NewGuid():N}");

            Directory.CreateDirectory(path);
            return new TestDirectory(path);
        }

        public string CreateDirectory(string name)
        {
            var path = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(path);
            return path;
        }

        public string WriteFile(string name, string? directory = null)
        {
            var folder = directory ?? Path;
            var path = System.IO.Path.Combine(folder, name);
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
