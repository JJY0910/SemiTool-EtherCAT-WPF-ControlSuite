using System.Windows;
using System.IO;
using SemiTool.Application;
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

        if (HasArgument(e.Args, "--capture-sequence-assets") ||
            HasArgument(e.Args, "--capture-demo-assets"))
        {
            // 기존 자동화가 깨지지 않도록 옛 캡처 인자는 유지하고,
            // 공개 문서에서는 장비 시퀀스 산출물 이름을 사용한다.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var viewModel = new MainViewModel(runtime, profilePath, settingsPath);
            await SequenceAssetCapture.CaptureAsync(runtime, viewModel);
            Shutdown();
            return;
        }

        if (e.Args.Any(arg => string.Equals(arg, "--capture-ui-debug-report", StringComparison.OrdinalIgnoreCase)))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var viewModel = new MainViewModel(runtime, profilePath, settingsPath);
            await SequenceAssetCapture.CaptureUiDebugReportAsync(runtime, viewModel);
            Shutdown();
            return;
        }

        if (e.Args.Any(arg => string.Equals(arg, "--capture-full-pipeline-qa", StringComparison.OrdinalIgnoreCase)))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var viewModel = new MainViewModel(runtime, profilePath, settingsPath);
            await SequenceAssetCapture.CaptureFullPipelineQaAsync(runtime, viewModel);
            Shutdown();
            return;
        }

        var window = new MainWindow
        {
            DataContext = new MainViewModel(runtime, profilePath, settingsPath)
        };
        window.Show();
    }

    private static bool HasArgument(IEnumerable<string> args, string value) =>
        args.Any(arg => string.Equals(arg, value, StringComparison.OrdinalIgnoreCase));

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
