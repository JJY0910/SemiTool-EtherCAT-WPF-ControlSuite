using System.Windows;
using SemiTool.EtherCAT.ControlSuite.ViewModels;

namespace SemiTool.EtherCAT.ControlSuite;

/// <summary>
/// 실장비 웨이퍼 이송 제어 트레이너의 메인 운전 화면입니다.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
