namespace SemiTool.Hardware;

public sealed record VendorDllResolutionResult(
    bool Success,
    string? ResolvedPath,
    IReadOnlyList<string> AttemptedPaths,
    string ErrorMessage);

public static class VendorDllResolver
{
    public const string ExpectedFileName = "IEG3268_Dll.dll";
    public static readonly string DefaultRelativePath = Path.Combine("libs", ExpectedFileName);

    public static VendorDllResolutionResult Resolve(
        string? userConfiguredPath,
        string? currentDirectory = null,
        string? appBaseDirectory = null,
        string? repositoryRoot = null)
    {
        currentDirectory ??= Environment.CurrentDirectory;
        appBaseDirectory ??= AppContext.BaseDirectory;
        var configuredPath = string.IsNullOrWhiteSpace(userConfiguredPath)
            ? DefaultRelativePath
            : userConfiguredPath;

        var attempted = new List<string>();

        if (Path.IsPathFullyQualified(configuredPath))
        {
            if (TryUse(configuredPath, attempted, out var resolvedAbsolute))
            {
                return Success(resolvedAbsolute, attempted);
            }
        }
        else
        {
            if (TryUse(Path.Combine(currentDirectory, configuredPath), attempted, out var resolvedFromCurrent))
            {
                return Success(resolvedFromCurrent, attempted);
            }

            if (TryUse(Path.Combine(appBaseDirectory, configuredPath), attempted, out var resolvedFromOutput))
            {
                return Success(resolvedFromOutput, attempted);
            }
        }

        var discoveredRoot = repositoryRoot
            ?? FindRepositoryRoot(currentDirectory)
            ?? FindRepositoryRoot(appBaseDirectory);
        if (!string.IsNullOrWhiteSpace(discoveredRoot) &&
            TryUse(Path.Combine(discoveredRoot, DefaultRelativePath), attempted, out var resolvedFromRepo))
        {
            return Success(resolvedFromRepo, attempted);
        }

        if (TryUse(Path.Combine(appBaseDirectory, DefaultRelativePath), attempted, out var resolvedOutputLocal))
        {
            return Success(resolvedOutputLocal, attempted);
        }

        return new VendorDllResolutionResult(false, null, attempted, BuildErrorMessage(attempted));
    }

    private static bool TryUse(string path, List<string> attempted, out string resolvedPath)
    {
        resolvedPath = Path.GetFullPath(path);
        if (!attempted.Contains(resolvedPath, StringComparer.OrdinalIgnoreCase))
        {
            attempted.Add(resolvedPath);
        }

        return File.Exists(resolvedPath);
    }

    private static VendorDllResolutionResult Success(string resolvedPath, List<string> attempted) =>
        new(true, resolvedPath, attempted, string.Empty);

    private static string BuildErrorMessage(IReadOnlyList<string> attempted)
    {
        var attempts = attempted.Count == 0
            ? "No paths were attempted."
            : string.Join(Environment.NewLine, attempted.Select(path => $"  - {path}"));

        return
            $"Real Hardware mode requires local {ExpectedFileName}.{Environment.NewLine}" +
            "The DLL is intentionally not committed to GitHub." + Environment.NewLine +
            $"Place it under {DefaultRelativePath} or set an absolute path in Settings." + Environment.NewLine +
            "Simulator mode does not require the DLL." + Environment.NewLine +
            "Attempted paths:" + Environment.NewLine +
            attempts;
    }

    private static string? FindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SemiTool.EtherCAT.WPF.ControlSuite.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
