using SemiTool.Hardware;

namespace SemiTool.Tests;

/// <summary>
/// Tests the local equipment-PC DLL workflow without requiring the real vendor DLL in public CI.
/// </summary>
/// <remarks>
/// Fake files are enough for resolver tests because Assembly.LoadFrom behavior is covered separately by the
/// BadImageFormatException test. This protects the Visual Studio path setup while keeping GitHub clean.
/// </remarks>
public sealed class VendorDllResolverTests
{
    [Fact]
    public void AbsoluteExistingDllPath_Resolves()
    {
        using var directory = new TemporaryDirectory();
        var path = CreateFakeDllFile(directory.Path);

        var result = VendorDllResolver.Resolve(path);

        Assert.True(result.Success);
        Assert.Equal(path, result.ResolvedPath);
    }

    [Fact]
    public void RelativePath_ResolvesFromSuppliedCurrentDirectory()
    {
        using var directory = new TemporaryDirectory();
        var libs = Directory.CreateDirectory(Path.Combine(directory.Path, "libs"));
        var expected = Path.Combine(libs.FullName, VendorDllResolver.ExpectedFileName);
        File.WriteAllText(expected, "fake");

        var result = VendorDllResolver.Resolve(
            VendorDllResolver.DefaultRelativePath,
            currentDirectory: directory.Path,
            appBaseDirectory: Path.Combine(directory.Path, "bin"));

        Assert.True(result.Success);
        Assert.Equal(expected, result.ResolvedPath);
    }

    [Fact]
    public void MissingDllResult_IncludesAttemptedPaths()
    {
        using var directory = new TemporaryDirectory();

        // Missing DLL is the normal public-repo and CI case; the result must guide the operator instead of crashing.
        var result = VendorDllResolver.Resolve(
            VendorDllResolver.DefaultRelativePath,
            currentDirectory: directory.Path,
            appBaseDirectory: Path.Combine(directory.Path, "output"));

        Assert.False(result.Success);
        Assert.Null(result.ResolvedPath);
        Assert.NotEmpty(result.AttemptedPaths);
    }

    [Fact]
    public void MissingDllError_MentionsLocalLibsPath()
    {
        using var directory = new TemporaryDirectory();

        var result = VendorDllResolver.Resolve(
            "missing.dll",
            currentDirectory: directory.Path,
            appBaseDirectory: Path.Combine(directory.Path, "output"));

        Assert.Contains("libs", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(VendorDllResolver.ExpectedFileName, result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingDllError_MentionsSimulatorModeDoesNotRequireDll()
    {
        using var directory = new TemporaryDirectory();

        var result = VendorDllResolver.Resolve(
            "missing.dll",
            currentDirectory: directory.Path,
            appBaseDirectory: Path.Combine(directory.Path, "output"));

        Assert.Contains("Simulator mode does not require the DLL", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BadImageFormatException_IsWrappedWithArchitectureGuidance()
    {
        using var directory = new TemporaryDirectory();
        var fakeDll = CreateFakeDllFile(directory.Path);
        var controller = new Ieg3268EthercatController(TestProfile.Load(), fakeDll);

        // A text file has the same observable load failure shape as a wrong-bitness DLL for this adapter boundary.
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.ConnectAsync());

        Assert.IsType<BadImageFormatException>(exception.InnerException);
        Assert.Contains("architecture mismatch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("x86", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Simulator mode is still available", exception.Message, StringComparison.Ordinal);
    }

    private static string CreateFakeDllFile(string directory)
    {
        var path = Path.Combine(directory, VendorDllResolver.ExpectedFileName);
        File.WriteAllText(path, "not a real dotnet assembly");
        return path;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = Directory.CreateTempSubdirectory("semitool-resolver-test-").FullName;
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
