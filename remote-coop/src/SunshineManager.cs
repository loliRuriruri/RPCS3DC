using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace DragonCrownRemoteCoop
{
    /// <summary>
    /// Sunshine host manager: multi-path install detection, service / Web UI / process status,
    /// config backup + controller-only policy (controller=enabled, gamepad=x360, keyboard/mouse=disabled).
    /// No passwords, tokens or pairing PINs are ever read, stored or logged.
    /// </summary>
    public static class SunshineManager
    {
        public const string ServiceName = "SunshineService";
        public const int WebUiPort = 47990;

        // ------------------------------------------------------------------ detection
        public static string InstallDir()
        {
            var candidates = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Sunshine"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Sunshine"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sunshine"),
            };
            foreach (var c in candidates)
                if (File.Exists(Path.Combine(c, "sunshine.exe"))) return c;

            string reg = RegistryUninstallDir("^Sunshine");
            if (!string.IsNullOrEmpty(reg) && File.Exists(Path.Combine(reg, "sunshine.exe"))) return reg;
            return null;
        }

        public static string ExePath
        {
            get
            {
                string dir = InstallDir();
                return dir == null ? null : Path.Combine(dir, "sunshine.exe");
            }
        }

        public static bool IsInstalled => ExePath != null;

        public static string Version()
        {
            try
            {
                if (ExePath != null)
                {
                    var v = FileVersionInfo.GetVersionInfo(ExePath).FileVersion;
                    if (!string.IsNullOrEmpty(v)) return v;
                }
            }
            catch { }
            return RegistryUninstallVersion("^Sunshine") ?? "";
        }

        public static string ConfigDir
        {
            get
            {
                string dir = InstallDir();
                if (dir != null && Directory.Exists(Path.Combine(dir, "config"))) return Path.Combine(dir, "config");
                string local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sunshine", "config");
                if (Directory.Exists(local)) return local;
                if (dir != null) return Path.Combine(dir, "config");   // expected after first run
                return null;
            }
        }

        public static string ConfFile => ConfigDir == null ? null : Path.Combine(ConfigDir, "sunshine.conf");
        public static string AppsFile => ConfigDir == null ? null : Path.Combine(ConfigDir, "apps.json");

        // ------------------------------------------------------------------ runtime status
        public static bool IsProcessRunning()
        {
            try
            {
                return Process.GetProcesses().Any(p =>
                { try { return p.ProcessName.Equals("sunshine", StringComparison.OrdinalIgnoreCase); } catch { return false; } });
            }
            catch { return false; }
        }

        public sealed class ServiceInfo
        {
            public bool Exists;
            public bool Running;
            public string State = "not found";
        }

        public static ServiceInfo Service()
        {
            var info = new ServiceInfo();
            try
            {
                var psi = new ProcessStartInfo("sc.exe", "query " + ServiceName)
                { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
                using var p = Process.Start(psi);
                string o = p.StandardOutput.ReadToEnd();
                if (o.Contains("SERVICE_NAME") || o.Contains("STATE"))
                {
                    info.Exists = true;
                    var m = Regex.Match(o, @"STATE\s*:\s*\d+\s+(\w+)");
                    if (m.Success) info.State = m.Groups[1].Value;
                    info.Running = info.State.Equals("RUNNING", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { }
            if (!info.Exists)
            {
                // some builds register the service with a different name
                foreach (var name in new[] { "Sunshine", "sunshine" })
                {
                    try
                    {
                        var psi = new ProcessStartInfo("sc.exe", "query " + name)
                        { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
                        using var p = Process.Start(psi);
                        string o = p.StandardOutput.ReadToEnd();
                        if (o.Contains("SERVICE_NAME"))
                        {
                            info.Exists = true;
                            var m = Regex.Match(o, @"STATE\s*:\s*\d+\s+(\w+)");
                            if (m.Success) info.State = m.Groups[1].Value;
                            info.Running = info.State.Equals("RUNNING", StringComparison.OrdinalIgnoreCase);
                            break;
                        }
                    }
                    catch { }
                }
            }
            return info;
        }

        public static bool WebUiReachable()
        {
            try
            {
                using var client = new TcpClient();
                var task = client.ConnectAsync("127.0.0.1", WebUiPort);
                return task.Wait(1500) && client.Connected;
            }
            catch { return false; }
        }

        // ------------------------------------------------------------------ config
        public static Dictionary<string, string> ReadConf()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (ConfFile == null || !File.Exists(ConfFile)) return d;
                foreach (var line in File.ReadAllLines(ConfFile))
                {
                    var t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#") || t.StartsWith(";")) continue;
                    int eq = t.IndexOf('=');
                    if (eq <= 0) continue;
                    d[t.Substring(0, eq).Trim()] = t.Substring(eq + 1).Trim();
                }
            }
            catch { }
            return d;
        }

        public sealed class ControllerPolicy
        {
            public bool ControllerEnabled = true;
            public bool KeyboardEnabled = true;
            public bool MouseEnabled = true;
            public string Gamepad = "";
            public bool ControllerOnly => ControllerEnabled && !KeyboardEnabled && !MouseEnabled;
            public string Summary => $"controller={(ControllerEnabled ? "enabled" : "disabled")} " +
                                     $"keyboard={(KeyboardEnabled ? "enabled" : "disabled")} " +
                                     $"mouse={(MouseEnabled ? "enabled" : "disabled")} " +
                                     $"gamepad={(Gamepad.Length > 0 ? Gamepad : "auto(default)")}";
        }

        public static ControllerPolicy ReadPolicy()
        {
            var conf = ReadConf();
            var p = new ControllerPolicy();
            if (conf.TryGetValue("controller", out string c)) p.ControllerEnabled = !c.Equals("disabled", StringComparison.OrdinalIgnoreCase);
            if (conf.TryGetValue("keyboard", out string k)) p.KeyboardEnabled = !k.Equals("disabled", StringComparison.OrdinalIgnoreCase);
            if (conf.TryGetValue("mouse", out string m)) p.MouseEnabled = !m.Equals("disabled", StringComparison.OrdinalIgnoreCase);
            if (conf.TryGetValue("gamepad", out string g)) p.Gamepad = g;
            return p;
        }

        /// <summary>Applies the controller-only policy (keyboard/mouse off, Xbox 360 virtual pad).</summary>
        public static bool ApplyControllerOnly(bool enable, out string error)
        {
            error = null;
            try
            {
                if (ConfFile == null) { error = "Sunshine config not found (install/run Sunshine once)"; return false; }
                if (!File.Exists(ConfFile)) { error = "sunshine.conf not found: " + ConfFile; return false; }
                if (IsProcessRunning()) { error = "Sunshine is running - stop it before editing the config"; return false; }

                BackupManager.BackupSunshine();

                var lines = File.ReadAllLines(ConfFile).ToList();
                void SetKey(string key, string value)
                {
                    int idx = lines.FindIndex(l => Regex.IsMatch(l, @"^\s*" + Regex.Escape(key) + @"\s*="));
                    string entry = key + " = " + value;
                    if (idx >= 0) lines[idx] = entry; else lines.Add(entry);
                }
                SetKey("controller", "enabled");
                SetKey("gamepad", "x360");                       // guarantee an XInput pad for RPCS3 P2
                SetKey("keyboard", enable ? "disabled" : "enabled");
                SetKey("mouse", enable ? "disabled" : "enabled");

                File.WriteAllLines(ConfFile, lines);
                AppEnv.Log("sunshine controller-only=" + enable + " -> " + ConfFile);
                return true;
            }
            catch (Exception ex) { error = ex.Message; AppEnv.Log("sunshine policy failed: " + ex.Message); return false; }
        }

        // ------------------------------------------------------------------ actions
        public static bool StartService(out string output)
        {
            output = "";
            try
            {
                var psi = new ProcessStartInfo("sc.exe", "start " + ServiceName)
                { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
                using var p = Process.Start(psi);
                output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                return p.ExitCode == 0 || output.Contains("RUNNING");
            }
            catch (Exception ex) { output = ex.Message; return false; }
        }

        public static void OpenWebUi() =>
            Process.Start(new ProcessStartInfo("https://localhost:" + WebUiPort + "/") { UseShellExecute = true });

        public static void OpenExe()
        {
            if (ExePath == null) return;
            Process.Start(new ProcessStartInfo(ExePath)
            { WorkingDirectory = Path.GetDirectoryName(ExePath), UseShellExecute = true });
        }

        public static void OpenConfigFolder()
        {
            string dir = ConfigDir;
            if (dir == null) return;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + dir + "\"") { UseShellExecute = true });
        }

        // ------------------------------------------------------------------ registry helpers
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
