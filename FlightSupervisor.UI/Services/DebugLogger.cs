using System;
using System.IO;

namespace FlightSupervisor.UI.Services
{
    public static class DebugLogger
    {
        private static string _logFilePath;
        private static readonly object _lock = new object();
        private static bool _isInitialized = false;

        public static void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                _logFilePath = Path.Combine(logDir, $"Session_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                
                File.WriteAllText(_logFilePath, $"=== Flight Supervisor Runtime Debug Log ===\nStarted at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize DebugLogger: {ex.Message}");
            }
        }

        public static void Log(string category, string message)
        {
            if (!_isInitialized) Initialize();
            
            try
            {
                lock (_lock)
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    string logLine = $"[{timestamp}] [{category.ToUpperInvariant()}] {message}\n";
                    File.AppendAllText(_logFilePath, logLine);
                }
            }
            catch
            {
                // Suppress logging errors to prevent crashing the main app
            }
        }

        public static void MarkBug()
        {
            Log("MARKER", "========================================");
            Log("MARKER", "       USER BUG MARKER TRIGGERED        ");
            Log("MARKER", "========================================");
        }
    }
}
