using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DragonCrownRemoteCoop
{
    /// <summary>
    /// Moonlight guest manager: install detection, launching, and guided pairing/streaming.
    /// Pairing PINs are never read, stored or logged (they are shown by Moonlight itself).
    /// </summary>
    public static class MoonlightManager
    {
        public static string ExePath()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Moonlight", "Moonlight.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Moonlight", "Moonlight.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Moonlight", "Moonlight.exe"),
            };
            foreach (var c in candidates) if (File.Exists(c)) return c;

            try
            {
                var psi = new ProcessStartInfo("where.exe", "moonlight")
                { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using var p = Process.Start(psi);
                string o = p.StandardOutput.ReadToEnd();
                var first = o.Split('\n').Select(s => s.Trim()).FirstOrDefault(s => s.Length > 0 && File.Exists(s));
                if (first != null) return first;
            }
            catch { }

            string reg = RegistryUninstallDir("^Moonlight");
            if (!string.IsNullOrEmpty(reg) && File.Exists(Path.Combine(reg, "Moonlight.exe"))) return Path.Combine(reg, "Moonlight.exe");
            return null;
        }

        public static bool IsInstalled => ExePath() != null;

        public static string Version()
        {
            try
            {
                if (ExePath() != null)
                {
                    var v = FileVersionInfo.GetVersionInfo(ExePath()).FileVersion;
                    if (!string.IsNullOrEmpty(v)) return v;
                }
            }
            catch { }
            return RegistryUninstallVersion("^Moonlight") ?? "";
        }

        public static bool IsProcessRunning()
        {
            try
            {
                return Process.GetProcesses().Any(p =>
                { try { return p.ProcessName.Equals("Moonlight", StringComparison.OrdinalIgnoreCase); } catch { return false; } });
            }
            catch { return false; }
        }

        public static void Open()
        {
            string exe = ExePath();
            if (exe == null) return;
            Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe), UseShellExecute = true });
        }

        public static (bool ok, string output) RunCli(string arguments)
        {
            try
            {
                string exe = ExePath();
                if (exe == null) return (false, "Moonlight not installed");
                var psi = new ProcessStartInfo(exe, arguments)
                { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(exe) };
                using var p = Process.Start(psi);
                string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                bool exited = p.WaitForExit(4000);
                AppEnv.Log("moonlight cli: " + arguments + " -> " + (exited ? "exit " + p.ExitCode : "running"));
                return (true, o.Trim());
            }
            catch (Exception ex) { AppEnv.Log("moonlight cli failed: " + ex.Message); return (false, ex.Message); }
        }

        /// <summary>Pair with the host (PIN is shown by Moonlight; the user approves it in the Sunshine Web UI).</summary>
        public static (bool ok, string message) Pair(string host)
        {
            if (!IsValidHost(host, out string error)) return (false, error);
            Open();                                   // official GUI is the primary path (v1)
            var (ok, output) = RunCli("pair " + host);
            string msg = ok
                ? $"Moonlight pairing started for {host}.\n\n" +
                  "1) Moonlight 창에 표시되는 4자리 PIN 확인\n" +
                  "2) HOST PC의 Sunshine Web UI(https://localhost:47990) → PIN 입력\n" +
                  "3) 완료 후 [OPEN MOONLIGHT] 로 Desktop 스트림 시작"
                : "Moonlight CLI 를 실행할 수 없습니다. Moonlight GUI 에서 직접 HOST 를 추가하세요.\n" + output;
            return (ok, msg);
        }

        /// <summary>Starts the Desktop stream (v1 uses Desktop, not a direct app entry).</summary>
        public static (bool ok, string message) StreamDesktop(string host)
        {
            if (!IsValidHost(host, out string error)) return (false, error);
            Open();
            var (ok, output) = RunCli("stream " + host + " desktop");
            return ok
                ? (true, $"Moonlight Desktop 스트림을 시작했습니다 ({host}).\n게임은 HOST 에서 DragonCrownProEnhanced.exe → MULTIPLAYER → Remote Co-op 로 실행하세요.")
                : (false, "Moonlight CLI 실행 실패: " + output);
        }

        public static void OpenWindowsGameControllers()
        {
            try { Process.Start(new ProcessStartInfo("joy.cpl") { UseShellExecute = true }); }
            catch { }
        }

        public static bool IsValidHost(string host, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(host)) { error = "Host address is empty."; return false; }
            host = host.Trim();
            if (host.Length > 253 || host.Any(c => char.IsWhiteSpace(c))) { error = "Host address is invalid."; return false; }
            if (host.Contains("://") || host.Contains("/") || host.Contains("\\")) { error = "Enter an IP address or hostname only."; return false; }
            return true;
        }

        private static string RegistryUninstallDir(string namePattern)
        {
            foreach (var root in new[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" })
            {
                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(root);
                    if (key == null) continue;
                    foreach (var sub in key.GetSubKeyNames())
                    {
                        using var sk = key.OpenSubKey(sub);
                        string name = sk?.GetValue("DisplayName") as string;
                        if (string.IsNullOrEmpty(name) || !Regex.IsMatch(name, namePattern, RegexOptions.IgnoreCase)) continue;
                        string loc = sk.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrEmpty(loc)) return loc.TrimEnd('\\');
                    }
                }
                catch { }
            }
            return null;
        }

        private static string RegistryUninstallVersion(string namePattern)
        {
            foreach (var root in new[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" })
            {
                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(root);
                    if (key == null) continue;
                    foreach (var sub in key.GetSubKeyNames())
                    {
                        using var sk = key.OpenSubKey(sub);
                        string name = sk?.GetValue("DisplayName") as string;
                        if (string.IsNullOrEmpty(name) || !Regex.IsMatch(name, namePattern, RegexOptions.IgnoreCase)) continue;
                        string v = sk.GetValue("DisplayVersion") as string;
                        if (!string.IsNullOrEmpty(v)) return v;
                    }
                }
                catch { }
            }
            return null;
        }
    }
}
