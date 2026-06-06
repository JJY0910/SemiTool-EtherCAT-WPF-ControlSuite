namespace SemiTool.Tests;

public sealed class StartupSafetyBoundaryTests
{
    [Fact]
    public void WpfStartup_DoesNotAutoConnectOrAutoRun()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "App.xaml.cs");

        Assert.Contains("new SelectableEthercatController(profile)", source, StringComparison.Ordinal);
        Assert.Contains("new MainViewModel(runtime, profilePath, settingsPath)", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".ConnectAsync(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StartAuto", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RunTransferSequence", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CaptureArguments_DoNotUseRealHardwareConnectionPath()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "App.xaml.cs");

        Assert.Contains("--capture-sequence-assets", source, StringComparison.Ordinal);
        Assert.Contains("--capture-full-pipeline-qa", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Ieg3268EthercatController", source, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { TestProfile.FindRepositoryRoot() }.Concat(parts).ToArray()));
}
