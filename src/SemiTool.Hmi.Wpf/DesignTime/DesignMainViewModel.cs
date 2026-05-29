namespace SemiTool.Hmi.Wpf.DesignTime;

/// <summary>
/// Design-time shell model for MainWindow.xaml.
/// </summary>
/// <remarks>
/// MainWindow's runtime DataContext is still MainViewModel from App.xaml.cs.
/// This object is referenced only through d:DataContext so Visual Studio can
/// render the first Machine Twin tab with realistic sample data at design time.
/// </remarks>
public sealed class DesignMainViewModel
{
    public DesignMainViewModel()
    {
        MachineTwin = new DesignMachineTwinViewModel();
    }

    public DesignMachineTwinViewModel MachineTwin { get; }
}
