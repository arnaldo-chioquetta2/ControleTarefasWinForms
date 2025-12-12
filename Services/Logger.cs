using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ControleTarefasWinForms.Services
{
    internal static class Logger
    {
        private static readonly object _lock = new object();

        // Log na pasta do exe
        private static readonly string _logPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");

        public static void Info(string msg) => Write("INFO", msg);
        public static void Error(string msg, Exception ex = null)
            => Write("ERRO", ex == null ? msg : $"{msg} | EX: {ex}");

        private static void Write(string level, string msg)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {msg}";
            System.Diagnostics.Debug.WriteLine("c");
            // Debug.WriteLine(line);

            lock (_lock)
            {
                File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
    }
}
