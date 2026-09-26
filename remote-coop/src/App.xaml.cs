using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace DragonCrownRemoteCoop
{
    /// <summary>Shared environment: root discovery, logging, settings (no hardcoded project paths).</summary>
    public static class AppEnv
    {
        public static string Root { get; private set; }
        public static string LogDir => Path.Combine(Root, "Logs");
        public static string LogFile => Path.Combine(LogDir, "remote_coop_helper.log");
        public static string LocalDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DragonCrownRemoteCoop");
        public static string DownloadsDir => Path.Combine(LocalDir, "Downloads");
        public static string SettingsFile => Path.Combine(LocalDir, "settings.json");

        /// <summary>
        /// Root discovery identical in spirit to the main launcher:
        /// exe lives in &lt;ROOT&gt;\Launcher, so the parent folder is the root.
        /// Falls back to walking up / root.txt / the exe folder itself.
        /// </summary>
        public static void Discover()
        {
            if (Root != null) return;
            string exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var candidates = new List<string> { Directory.GetParent(exeDir)?.FullName, exeDir };
            var d = new DirectoryInfo(exeDir);
            for (int i = 0; i < 5 && d?.Parent != null; i++) { d = d.Parent; candidates.Add(d.FullName); }
            foreach (var c in candidates)
            {
                if (!string.IsNullOrEmpty(c) && File.Exists(Path.Combine(c, "RPCS3", "rpcs3.exe"))) { Root = c; return; }
            }
            try
            {
                string hint = Path.Combine(exeDir, "root.txt");
                if (File.Exists(hint))
                {
                    string saved = File.ReadAllText(hint).Trim();
                    if (File.Exists(Path.Combine(saved, "RPCS3", "rpcs3.exe"))) { Root = saved; return; }
                }
            }
            catch { }
            Root = Directory.GetParent(exeDir)?.FullName ?? exeDir;
        }

        public static void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                File.AppendAllText(LogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}\r\n");
            }
            catch { }
        }

        public static Dictionary<string, string> LoadSettings()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(SettingsFile)) return d;
                foreach (var line in File.ReadAllLines(SettingsFile))
                {
                    var kv = line.Split(new[] { '=' }, 2);
                    if (kv.Length == 2) d[kv[0].Trim()] = kv[1].Trim();
                }
            }
            catch { }
            return d;
        }

        public static void SaveSetting(string key, string value)
        {
            var d = LoadSettings();
            d[key] = value;
            try
            {
                Directory.CreateDirectory(LocalDir);
                File.WriteAllLines(SettingsFile, d.Select(kv => kv.Key + "=" + kv.Value));
            }
            catch { }
        }

        /// <summary>Minimal JSON value reader (no external packages).</summary>
        public static string JsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var m = System.Text.RegularExpressions.Regex.Match(json,
                "\"" + System.Text.RegularExpressions.Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }
    }

    public partial class App : Application
    {
        [DllImport("kernel32.dll")] private static extern bool AttachConsole(int pid);
        [DllImport("kernel32.dll")] private static extern bool AllocConsole();

        public static bool CliMode { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppEnv.Discover();
            var args = e.Args ?? Array.Empty<string>();
            if (args.Length > 0)
            {
                CliMode = true;
                AttachConsole(-1);
                bool keepRunning = false;
                try { keepRunning = RunCli(args); }
                catch (Exception ex) { Console.WriteLine("ERROR: " + ex.Message); AppEnv.Log("cli error: " + ex.Message); }
                if (!keepRunning) Shutdown();
                return;
            }
            base.OnStartup(e);
            new MainWindow().Show();
        }

        private static void WriteOut(string text)
        {
            try { Console.WriteLine(text); } catch { }
        }

        private static bool RunCli(string[] args)
        {
            string a0 = args[0].ToLowerInvariant();
            string a1 = args.Length > 1 ? args[1] : null;
            AppEnv.Log("cli " + string.Join(" ", args));
            switch (a0)
            {
                case "--host":
                    new MainWindow(role: "host").Show();
                    return true;
                case "--guest":
                    new MainWindow(role: "guest").Show();
                    return true;

                case "--status":
                {
                    string file = a1 ?? Path.Combine(AppEnv.LogDir, "remote_coop_status.txt");
                    var lines = new List<string>();
                    lines.AddRange(HostSetup.StatusLines());
                    lines.AddRange(GuestSetup.StatusLines());
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllLines(file, lines);
                    foreach (var l in lines) WriteOut(l);
                    AppEnv.Log("cli --status -> " + file);
                    break;
                }

                case "--diag":
                {
                    string file = a1 ?? Path.Combine(AppEnv.LogDir, "remote_coop_diag.txt");
                    var items = Diagnostics.RunHost().Concat(Diagnostics.RunGuest()).ToList();
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllLines(file, items.Select(i =>
                        (i.Ok ? "[OK]  " : "[NG]  ") + i.Name.PadRight(34) + i.Detail + (i.Hint.Length > 0 ? "  -> " + i.Hint : "")));
                    foreach (var i in items) WriteOut((i.Ok ? "[OK]  " : "[NG]  ") + i.Name.PadRight(34) + i.Detail);
                    WriteOut($"총 {items.Count}개 항목 / NG {items.Count(i => !i.Ok)}개");
                    AppEnv.Log($"cli --diag -> {file} ({items.Count} items)");
                    break;
                }

                case "--test-pad":
                {
                    string file = a1 ?? Path.Combine(AppEnv.LogDir, "remote_coop_padtest.txt");
                    var r = HostSetup.TestRemotePad(8, WriteOut);
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllLines(file, r.Lines);
                    foreach (var l in r.Lines) WriteOut(l);
                    break;
                }

                case "--resolve":
                {
                    var targets = a1 == null ? new[] { "sunshine", "moonlight", "vigem" } : new[] { a1.ToLowerInvariant() };
                    foreach (var t in targets)
                    {
                        var (owner, repo, regex, label) = ReleaseResolver.Target(t);
                        var asset = ReleaseResolver.ResolveLatestStable(owner, repo, regex);
                        if (asset == null) { WriteOut($"{label}: no matching stable asset"); continue; }
                        WriteOut($"{label}: {asset.ReleaseTag} / {asset.Name} / {asset.Size:N0} bytes / digest={asset.Digest ?? "(none)"}");
                        WriteOut($"    url: {asset.Url}");
                        AppEnv.Log($"cli --resolve {t} -> {asset.ReleaseTag} {asset.Name} digest={asset.Digest}");
                    }
                    break;
                }

                case "--download":
                {
                    var targets = a1 == null ? new[] { "sunshine" } : new[] { a1.ToLowerInvariant() };
                    foreach (var t in targets)
                    {
                        var (owner, repo, regex, label) = ReleaseResolver.Target(t);
                        var asset = ReleaseResolver.ResolveLatestStable(owner, repo, regex);
                        if (asset == null) { WriteOut($"{label}: no matching stable asset"); continue; }
                        WriteOut($"{label}: downloading {asset.Name} ({asset.Size:N0} bytes) ...");
                        var res = Downloader.DownloadVerified(asset, p => { }, out string hash, out string error);
                        WriteOut(res ? $"OK  {asset.Name}  SHA256={hash}" : $"FAILED  {error}");
                        AppEnv.Log($"cli --download {t} -> {(res ? "ok " + hash : "failed: " + error)}");
                    }
                    break;
                }

                case "--fix-p2":
                {
                    string file = a1 ?? Path.Combine(AppEnv.LogDir, "remote_coop_fixp2.txt");
                    var sb = new List<string>();
                    if (Rpcs3Integration.Player2IsXInput)
                    {
                        sb.Add("Player 2 is already XInput. No change.");
                    }
                    else
                    {
                        bool ok = Rpcs3Integration.SetPlayer2ToXInput(out string err);
                        sb.Add(ok ? "Player 2 set to XInput (input config backed up under Backups\\RemoteCoop)."
                                  : "FAILED: " + err);
                    }
                    sb.Add("Player 1: " + Rpcs3Integration.ControllerHandler(1) + "  (never modified)");
                    sb.Add("Player 2: " + Rpcs3Integration.ControllerHandler(2));
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllLines(file, sb);
                    foreach (var l in sb) WriteOut(l);
                    break;
                }

                case "--restore":
                {
                    string what = a1 ?? "all";
                    if (what == "sunshine" || what == "all")
                        WriteOut(BackupManager.RestoreSunshine(null, out string e1) ? "Sunshine config restored" : "Sunshine restore failed: " + e1);
                    if (what == "rpcs3" || what == "all")
                        WriteOut(BackupManager.RestoreRpcs3Input(null, out string e2) ? "RPCS3 input config restored" : "RPCS3 restore failed: " + e2);
                    break;
                }

                case "--help":
                default:
                    WriteOut("DragonCrownRemoteCoopSetup.exe");
                    WriteOut("  --host                 HOST setup UI (game PC)");
                    WriteOut("  --guest                GUEST setup UI (friend PC)");
                    WriteOut("  --status [file]        machine-readable status (KEY=VALUE)");
                    WriteOut("  --diag [file]          HOST+GUEST diagnostics");
                    WriteOut("  --test-pad [file]      XInput remote pad test (8s)");
                    WriteOut("  --resolve [sunshine|moonlight|vigem]   resolve latest stable release");
                    WriteOut("  --download [sunshine|moonlight|vigem]  download + verify (no install)");
                    WriteOut("  --restore [sunshine|rpcs3|all]         restore from latest backup");
                    break;
            }
            return false;
        }
    }
}
