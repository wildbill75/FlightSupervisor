using System;
using System.Windows;
using System.Windows.Controls;

namespace FlightSupervisor.UI
{
    public partial class DebugWindow : Window
    {
        private MainWindow _mainWindow;

        public DebugWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }

        private void BtnFastForward5_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.ExecuteTimeSkip(5);
        }

        private void BtnFastForward10_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.ExecuteTimeSkip(10);
        }

        private void BtnFastForward20_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.ExecuteTimeSkip(20);
        }

        private void BtnForcePhase_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string phase)
            {
                _mainWindow.ExecuteForcePhase(phase);
            }
        }

        private void BtnCompleteOps_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.ExecuteCompleteAllOps();
        }
    }
}
