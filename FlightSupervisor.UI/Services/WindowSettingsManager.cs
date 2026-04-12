using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace FlightSupervisor.UI.Services
{
    public class WindowRect
    {
        public double Top { get; set; }
        public double Left { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public WindowState State { get; set; }
    }

    public static class WindowSettingsManager
    {
        private static string SettingsFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor_WindowSettings.json");
        private static Dictionary<string, WindowRect> _settings;

        static WindowSettingsManager()
        {
            LoadSettings();
        }

        private static void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    _settings = JsonSerializer.Deserialize<Dictionary<string, WindowRect>>(json) ?? new Dictionary<string, WindowRect>();
                }
                catch
                {
                    _settings = new Dictionary<string, WindowRect>();
                }
            }
            else
            {
                _settings = new Dictionary<string, WindowRect>();
            }
        }

        public static void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch { }
        }

        // Call this when the window opens (e.g. in constructor or Window_SourceInitialized)
        public static void ApplySettings(Window window, string windowName, double defaultWidth, double defaultHeight)
        {
            if (_settings.TryGetValue(windowName, out WindowRect rect))
            {
                // Ensure the window fits within virtual screen bounds to avoid opening off-screen
                if (rect.Left < SystemParameters.VirtualScreenLeft) rect.Left = SystemParameters.VirtualScreenLeft;
                if (rect.Top < SystemParameters.VirtualScreenTop) rect.Top = SystemParameters.VirtualScreenTop;
                
                // If it goes completely off to the right or bottom, bring it back
                if (rect.Left > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100)
                    rect.Left = 100;
                if (rect.Top > SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 100)
                    rect.Top = 100;

                window.Width = rect.Width;
                window.Height = rect.Height;
                window.Top = rect.Top;
                window.Left = rect.Left;

                // Optional: Restore maximized state
                if (rect.State == WindowState.Maximized)
                {
                    window.WindowState = WindowState.Maximized;
                }
                else
                {
                    window.WindowState = WindowState.Normal;
                }
            }
            else
            {
                // Use defaults if no prior settings exist, but limit to screen size to avoid being cut off
                double maxWidth = SystemParameters.WorkArea.Width * 0.95;
                double maxHeight = SystemParameters.WorkArea.Height * 0.95;

                double finalWidth = defaultWidth;
                double finalHeight = defaultHeight;

                // Scale down width if needed
                if (finalWidth > maxWidth)
                {
                    double ratio = maxWidth / finalWidth;
                    finalWidth = maxWidth;
                    finalHeight *= ratio;
                }

                // Scale down height if needed (after width scaling)
                if (finalHeight > maxHeight)
                {
                    double ratio = maxHeight / finalHeight;
                    finalHeight = maxHeight;
                    finalWidth *= ratio;
                }

                window.Width = finalWidth;
                window.Height = finalHeight;
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        // Call this when the window closes
        public static void SaveWindowState(Window window, string windowName)
        {
            if (window == null) return;

            var rect = new WindowRect
            {
                Width = window.Width,
                Height = window.Height,
                Top = window.Top,
                Left = window.Left,
                State = window.WindowState
            };

            // If maximized, save the RestoreBounds instead so the window doesn't open huge when restoring later
            if (window.WindowState == WindowState.Maximized)
            {
                rect.Width = window.RestoreBounds.Width;
                rect.Height = window.RestoreBounds.Height;
                rect.Top = window.RestoreBounds.Top;
                rect.Left = window.RestoreBounds.Left;
            }

            _settings[windowName] = rect;
            SaveSettings();
        }
    }
}
