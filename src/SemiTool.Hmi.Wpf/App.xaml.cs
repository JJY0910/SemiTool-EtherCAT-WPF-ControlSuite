using System.Windows;
using System.IO;
using SemiTool.Application;
using SemiTool.Hardware;
using SemiTool.Hmi.Wpf.ViewModels;
using SemiTool.Infrastructure;

namespace SemiTool.Hmi.Wpf;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var profilePath = ResolvePath(Path.Combine("config", "EquipmentProfile.finaltest.json"));
        var settingsPath = ResolvePath(Path.Combine("config", "appsettings.local.json"), mustExist: false);

        var profile = await new EquipmentProfileLoader().LoadAsync(profilePath);
        var controller = new SelectableEthercatController(profile);
        var alarms = new AlarmService();
        var events = new EventLogService();
        var safety = new SafetyInterlockService(alarms, events);
        var recipes = new RecipeService(profile);
        var sequence = new EquipmentSequenceService(controller, profile, safety, alarms, events);
        var scheduler = new TransferScheduler(recipes);
        var runtime = new RuntimeCoordinator(profile, controller, sequence, scheduler, safety, alarms, events, recipes);

        if (e.Args.Any(arg => string.Equals(arg, "--capture-demo-assets", StringComparison.OrdinalIgnoreCase)))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var viewModel = new MainViewModel(runtime, profilePath, settingsPath);
            await DemoAssetCapture.CaptureAsync(runtime, viewModel);
            Shutdown();
            return;
        }

        if (e.Args.Any(arg => string.Equals(arg, "--capture-ui-debug-report", StringComparison.OrdinalIgnoreCase)))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var viewModel = new MainViewModel(runtime, profilePath, settingsPath);
            await DemoAssetCapture.CaptureUiDebugReportAsync(runtime, viewModel);
            Shutdown();
            return;
        }

        var window = new MainWindow
        {
            DataContext = new MainViewModel(runtime, profilePath, settingsPath)
        };
        window.Show();
    }

    private static string ResolvePath(string relativePath, bool mustExist = true)
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (File.Exists(basePath) || !mustExist)
        {
            return basePath;
        }

        var currentPath = Path.Combine(Environment.CurrentDirectory, relativePath);
        if (File.Exists(currentPath))
        {
            return currentPath;
        }

        return basePath;
    }
}
