namespace SemiTool.Hardware;

/// <summary>
/// Result object for resolving the machine-local IEG3268 vendor DLL.
/// </summary>
/// <param name="Success">True when a usable DLL path was found.</param>
/// <param name="ResolvedPath">The full path selected for runtime loading, or null when resolution failed.</param>
/// <param name="AttemptedPaths">All paths probed in order. This is surfaced to the operator for fast equipment-PC setup checks.</param>
/// <param name="ErrorMessage">A user-facing message that explains why Real Hardware mode cannot connect yet.</param>
public sealed record VendorDllResolutionResult(
    bool Success,
    string? ResolvedPath,
    IReadOnlyList<string> AttemptedPaths,
    string ErrorMessage);

/// <summary>
/// Resolves the optional IEG3268 vendor DLL without making it a build-time dependency.
/// </summary>
/// <remarks>
/// The DLL is intentionally not committed to GitHub because it is vendor-owned and machine-local.
/// Simulator mode must build and run without it; only Real Hardware Connect needs the DLL.
/// Search order is:
/// 1. absolute user-configured path,
/// 2. user path relative to the current working directory,
/// 3. user path relative to <see cref="AppContext.BaseDirectory"/>,
/// 4. repository-root <c>libs/IEG3268_Dll.dll</c>,
/// 5. output-local <c>libs/IEG3268_Dll.dll</c>.
/// </remarks>
public static class VendorDllResolver
{
    public const string ExpectedFileName = "IEG3268_Dll.dll";
    public static readonly string DefaultRelativePath = Path.Combine("libs", ExpectedFileName);

    /// <summary>
    /// Resolves the configured vendor DLL path and returns a diagnostic result instead of throwing on a missing file.
    /// </summary>
    /// <remarks>
    /// Missing DLLs are expected on developer and CI machines. Returning a result keeps public CI green while still
    /// producing a clear operator message when Real Hardware mode is selected on an equipment PC without the DLL.
    /// </remarks>
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

        // User settings win first so an equipment PC can point at a locked-down vendor folder.
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

        // When running from Visual Studio, the current directory is often the repo root; when running from output,
        // AppContext.BaseDirectory is under bin. Walk both so local libs/IEG3268_Dll.dll works in either case.
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

        // Keep this message operational: it is shown when the user tries Real Hardware mode without the local DLL.
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
