using System.Windows.Controls;

namespace SemiTool.Hmi.Wpf.Views;

/// <summary>
/// Runtime WPF view for the simulator Machine Twin tab.
/// </summary>
/// <remarks>
/// This code-behind intentionally stays thin. All machine state, simulator
/// demo sequencing, and safety boundary text live in
/// <see cref="ViewModels.MachineTwinViewModel"/> so the same view model can
/// drive both the running app and the screenshot/debug-evidence capture modes.
///
/// Keeping the real UI and capture UI on this same UserControl prevents the
/// portfolio screenshots from drifting into disconnected mockups.
/// </remarks>
public partial class MachineTwinView : UserControl
{
    /// <summary>
    /// Initializes the Machine Twin visual tree declared in XAML.
    /// </summary>
    public MachineTwinView()
    {
        InitializeComponent();
    }
}
