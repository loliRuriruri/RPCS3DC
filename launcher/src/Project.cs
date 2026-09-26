using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DragonCrownProEnhanced
{
    /// <summary>Project paths and settings (root is resolved relative to this exe, never hardcoded).</summary>
    public sealed class Project
    {
        public string Root { get; private set; }
        public string Rpcs3Dir => Path.Combine(Root, "RPCS3");
        public string Rpcs3Exe => Path.Combine(Rpcs3Dir, "rpcs3.exe");
        public string KnownGoodExe => Path.Combine(Rpcs3Dir, "KnownGood", "rpcs3.exe");
        public string ProfilesDir => Path.Combine(Root, "Profiles");
        public string RuntimeDir => Path.Combine(ProfilesDir, "_runtime");
        public string LogDir => Path.Combine(Root, "Logs");
        public string LauncherLog => Path.Combine(LogDir, "launcher.log");
        public string SavesDir => Path.Combine(Root, "Saves", "Dragons_Crown");
        public string ReShadeDir => Path.Combine(Root, "ReShade");
        public string ZeroBannerDll => Path.Combine(Root, "Mods_Patches", "ReShade", "ZeroBanner", "build", "ReShade64.dll");
        public string OfficialDll => Path.Combine(Root, "Mods_Patches", "ReShade", "Official_Backup", "ReShade64.dll");
        public string GameDir { get; private set; }
        public string GameExe => string.IsNullOrEmpty(GameDir) ? null : Path.Combine(GameDir, "PS3_GAME", "USRDIR", "EBOOT.BIN");
        public string TitleId { get; private set; } = "BCAS20298";

        public string DisplayMode { get; set; } = "Borderless4K";
        public bool ReShadeEnabled { get; set; } = true;
        public bool ReShadeSilent { get; set; } = true;
        public bool VSync { get; set; } = true;

        private const string DefaultProfile = "Dragons_Crown/DC_PRO_4K";

        public string ProfilePath(string name) => Path.Combine(ProfilesDir, name.Replace('/', Path.DirectorySeparatorChar), "config.yml");

        // ------------------------------------------------------------ root detection
        public static Project Discover()
        {
            var p = new Project();
            string exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string[] candidates =
            {
                Directory.GetParent(exeDir)?.FullName,   // <ROOT>\Launcher\  -> <ROOT>
                exeDir,                                  // exe placed directly in <ROOT>
            };
            foreach (var c in candidates)
            {
                if (string.IsNullOrEmpty(c)) continue;
                if (File.Exists(Path.Combine(c, "RPCS3", "rpcs3.exe")))
                {
                    p.Root = c;
                    break;
                }
            }
            if (p.Root == null)
            {
                // fall back to a saved root next to the exe
                string hint = Path.Combine(exeDir, "root.txt");
                if (File.Exists(hint))
                {
                    string saved = File.ReadAllText(hint).Trim();
                    if (File.Exists(Path.Combine(saved, "RPCS3", "rpcs3.exe"))) p.Root = saved;
                }
            }
            p.Root ??= exeDir;
            p.ResolveGame();
            p.LoadSettings();
            return p;
        }

        public void SaveRootHint()
        {
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "root.txt"), Root); } catch { }
        }

        private void ResolveGame()
        {
            // 1) games.yml in the RPCS3 config (written by RPCS3 itself)
            try
            {
                string games = Path.Combine(Rpcs3Dir, "config", "games.yml");
                if (File.Exists(games))
                {
                    foreach (var line in File.ReadAllLines(games))
                    {
                        var m = Regex.Match(line, @"^\s*" + TitleId + @"\s*:\s*(.+?)\s*$");
                        if (m.Success)
                        {
                            string dir = m.Groups[1].Value.Trim().Trim('"').Replace('/', Path.DirectorySeparatorChar);
                            if (Directory.Exists(dir))
                            {
                                string probe = Path.Combine(dir, "PS3_GAME", "USRDIR", "EBOOT.BIN");
                                if (File.Exists(probe)) { GameDir = dir; return; }
                                // maybe the path already points at PS3_GAME or its parent
                                var parent = Directory.GetParent(dir)?.FullName;
                                if (parent != null && File.Exists(Path.Combine(parent, "PS3_GAME", "USRDIR", "EBOOT.BIN"))) { GameDir = parent; return; }
                            }
                        }
                    }
                }
            }
            catch { }
            // 2) explicit hint file
            try
            {
                string hint = Path.Combine(AppContext.BaseDirectory, "game.txt");
                if (File.Exists(hint))
                {
                    string dir = File.ReadAllText(hint).Trim();
                    if (File.Exists(Path.Combine(dir, "PS3_GAME", "USRDIR", "EBOOT.BIN"))) GameDir = dir;
                }
            }
            catch { }
        }

        // ------------------------------------------------------------ settings
        private string SettingsFile => Path.Combine(AppContext.BaseDirectory, "settings.json");

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFile)) return;
                foreach (var line in File.ReadAllLines(SettingsFile))
                {
                    var kv = line.Split(new[] { '=' }, 2);
                    if (kv.Length != 2) continue;
                    string k = kv[0].Trim(), v = kv[1].Trim();
                    switch (k)
                    {
                        case "DisplayMode": DisplayMode = v; break;
                        case "ReShade": ReShadeEnabled = v == "1"; break;
                        case "ReShadeSilent": ReShadeSilent = v == "1"; break;
                        case "VSync": VSync = v == "1"; break;
                    }
                }
            }
            catch { }
        }

        public void SaveSettings()
        {
            try
            {
                File.WriteAllLines(SettingsFile, new[]
                {
                    "DisplayMode=" + DisplayMode,
                    "ReShade=" + (ReShadeEnabled ? "1" : "0"),
                    "ReShadeSilent=" + (ReShadeSilent ? "1" : "0"),
                    "VSync=" + (VSync ? "1" : "0"),
                });
            }
            catch { }
        }

        // ------------------------------------------------------------ runtime config
        public string BuildRuntimeConfig(string profileName)
        {
            string src = ProfilePath(profileName);
            if (!File.Exists(src)) throw new FileNotFoundException("프로필을 찾을 수 없습니다: " + src);
            Directory.CreateDirectory(RuntimeDir);
            string dst = Path.Combine(RuntimeDir, $"{profileName.Replace('/', '_')}__{DisplayMode}.yml");

            string text = File.ReadAllText(src);
            string fullscreen = DisplayMode == "Fullscreen" ? "true" : "false";
            text = Regex.Replace(text, @"(?m)^(\s*Start games in fullscreen mode:\s*).*$", "${1}" + fullscreen);
            text = Regex.Replace(text, @"(?m)^(\s*VSync Mode:\s*).*$", "${1}" + (VSync ? "Full" : "Disabled"));
            File.WriteAllText(dst, text, new UTF8Encoding(false));
            return dst;
        }

        // ------------------------------------------------------------ ReShade silent
        public void ApplyReShadeSilent(bool silent)
        {
            string ini = Path.Combine(Rpcs3Dir, "ReShade.ini");
            if (!File.Exists(ini)) return;
            string backupDir = Path.Combine(ReShadeDir, "Backup");
            Directory.CreateDirectory(backupDir);
            string backup = Path.Combine(backupDir, "ReShade.ini.before_silent");
            if (!File.Exists(backup)) File.Copy(ini, backup);

            string text = File.ReadAllText(ini);
            string overlay =
                "[OVERLAY]\r\n" +
                "TutorialProgress=" + (silent ? 4 : 0) + "\r\n" +
                "ShowClock=0\r\nShowFPS=" + (silent ? 0 : 0) + "\r\nShowFrameTime=0\r\nShowPresetName=0\r\n" +
                "ShowScreenshotMessage=" + (silent ? 0 : 1) + "\r\n" +
                "ShowPresetTransitionMessage=" + (silent ? 0 : 1) + "\r\n" +
                "ShowForceLoadEffectsButton=" + (silent ? 0 : 1) + "\r\n";
            if (Regex.IsMatch(text, @"(?m)^\[OVERLAY\]\s*$"))
                text = Regex.Replace(text, @"(?ms)^\[OVERLAY\].*?(?=^\[|\Z)", overlay);
            else
                text = text.TrimEnd() + "\r\n\r\n" + overlay;
            File.WriteAllText(ini, text);
        }

        // ------------------------------------------------------------ save backup
        public string BackupSaves(string label)
        {
            string home = Path.Combine(Rpcs3Dir, "dev_hdd0", "home", "00000001");
            string savedata = Path.Combine(home, "savedata");
            if (!Directory.Exists(savedata)) throw new DirectoryNotFoundException("savedata 폴더가 없습니다: " + savedata);

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string dst = Path.Combine(SavesDir, "AutoBackup", $"DC_SAVE_{stamp}_{label}");
            Directory.CreateDirectory(dst);

            var manifest = new List<string> { "backup: " + Path.GetFileName(dst), "created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "source: " + savedata, "" };
            int files = 0;
            foreach (var dir in Directory.GetDirectories(savedata, TitleId + "*"))
            {
                string target = Path.Combine(dst, Path.GetFileName(dir));
                CopyDir(dir, target);
                foreach (var f in Directory.GetFiles(target, "*", SearchOption.AllDirectories))
                {
                    string rel = f.Substring(dst.Length).TrimStart(Path.DirectorySeparatorChar);
                    manifest.Add($"{rel}  {Sha256(f)}");
                    files++;
                }
            }
            File.WriteAllLines(Path.Combine(dst, "BACKUP_MANIFEST.txt"), manifest);

            // retention: keep newest 20
            var all = Directory.GetDirectories(Path.Combine(SavesDir, "AutoBackup")).OrderByDescending(d => d).ToList();
            foreach (var old in all.Skip(20)) { try { Directory.Delete(old, true); } catch { } }

            Log($"save backup: {Path.GetFileName(dst)} ({files} files)");
            return dst;
        }

        /// <summary>Reads a file even while RPCS3 has it open (FileShare.ReadWrite).</summary>
        public static string ReadShared(string path)
        {
            try
            {
                if (path == null || !File.Exists(path)) return null;
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                return sr.ReadToEnd();
            }
            catch { return null; }
        }

        public static string Sha256(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
        }

        private static void CopyDir(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src)) File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(src)) CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
        }

        // ------------------------------------------------------------ info / checks
        public string Rpcs3Build()
        {
            try
            {
                string log = Path.Combine(Rpcs3Dir, "log", "RPCS3.log");
                string text = ReadShared(log);
                if (text != null)
                {
                    var m = Regex.Match(text, @"RPCS3 (v[0-9][^\r\n|]*)");
                    if (m.Success) return m.Groups[1].Value.Trim();
                }
            }
            catch { }
            return File.Exists(Rpcs3Exe) ? "rpcs3.exe 있음" : "없음";
        }

        public string PpuHash()
        {
            try
            {
                string log = Path.Combine(Rpcs3Dir, "log", "RPCS3.log");
                string text = ReadShared(log);
                if (text != null)
                {
                    var m = Regex.Match(text, @"PPU executable hash:\s*(PPU-[0-9a-f]{40})");
                    if (m.Success) return m.Groups[1].Value;
                }
            }
            catch { }
            return "미확인";
        }

        /// <summary>Reads APP_VER from the installed update PARAM.SFO (falls back to the disc dump).</summary>
        public string GameVersion()
        {
            string[] sfos =
            {
                Path.Combine(Rpcs3Dir, "dev_hdd0", "game", TitleId, "PARAM.SFO"),
                GameDir == null ? null : Path.Combine(GameDir, "PS3_GAME", "PARAM.SFO"),
            };
            foreach (var sfo in sfos)
            {
                string v = ReadSfoString(sfo, "APP_VER");
                if (!string.IsNullOrEmpty(v)) return v;
            }
            return "미확인";
        }

        public static string ReadSfoString(string path, string key)
        {
            try
            {
                if (path == null || !File.Exists(path)) return null;
                byte[] d = File.ReadAllBytes(path);
                int keyTable = BitConverter.ToInt32(d, 8);
                int dataTable = BitConverter.ToInt32(d, 12);
                int count = BitConverter.ToInt32(d, 16);
                for (int i = 0; i < count; i++)
                {
                    int off = 20 + i * 16;
                    int keyOff = BitConverter.ToUInt16(d, off);
                    int len = BitConverter.ToInt32(d, off + 4);
                    int valOff = BitConverter.ToInt32(d, off + 12);
                    int kStart = keyTable + keyOff;
                    int kEnd = kStart;
                    while (kEnd < d.Length && d[kEnd] != 0) kEnd++;
                    string k = Encoding.UTF8.GetString(d, kStart, kEnd - kStart);
                    if (k == key)
                    {
                        int vStart = dataTable + valOff;
                        return Encoding.UTF8.GetString(d, vStart, Math.Min(len, d.Length - vStart)).TrimEnd('\0');
                    }
                }
            }
            catch { }
            return null;
        }

        public string ReShadeVersion()
        {
            try
            {
                string dll = @"C:\ProgramData\ReShade\ReShade64.dll";
                if (File.Exists(dll))
                {
                    var vi = FileVersionInfo.GetVersionInfo(dll);
                    return vi.FileVersion ?? "확인됨";
                }
            }
            catch { }
            return "없음";
        }

        public bool RpcnConfigured()
        {
            try
            {
                string cfg = Path.Combine(Rpcs3Dir, "config", "rpcn.yml");
                if (!File.Exists(cfg)) return false;
                string t = File.ReadAllText(cfg);
                return Regex.IsMatch(t, @"(?m)^\s*npid:\s*\S+") && (Regex.IsMatch(t, @"(?m)^\s*token:\s*\S+") || Regex.IsMatch(t, @"(?m)^\s*password:\s*\S+"));
            }
            catch { return false; }
        }

        public bool CheatsOrPatchesEnabled()
        {
            foreach (var f in new[] { Path.Combine(Rpcs3Dir, "config", "cheats.yml"), Path.Combine(Rpcs3Dir, "config", "patch_config.yml") })
            {
                try { if (File.Exists(f) && File.ReadAllText(f).Contains(TitleId)) return true; } catch { }
            }
            return false;
        }

        public void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                File.AppendAllText(LauncherLog, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}\r\n");
            }
            catch { }
        }

        // ------------------------------------------------------------ launch
        public Process Launch(string profileName, bool useKnownGood, bool borderless)
        {
            string cfg = BuildRuntimeConfig(profileName);
            string exe = useKnownGood && File.Exists(KnownGoodExe) ? KnownGoodExe : Rpcs3Exe;
            string work = Path.GetDirectoryName(exe);
            if (GameExe == null || !File.Exists(GameExe)) throw new FileNotFoundException("게임 덤프를 찾을 수 없습니다 (games.yml 확인).");

            Log($"launch: profile={profileName} mode={DisplayMode} vsync={(VSync ? "Full" : "Disabled")} reshade={(ReShadeEnabled ? "on" : "off")} silent={ReShadeSilent} knownGood={useKnownGood}");
            Log($"  config: {cfg}");
            Log($"  build : {Rpcs3Build()} | game={TitleId} v{GameVersion()} | ppu={PpuHash()}");

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = work,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("--config");
            psi.ArgumentList.Add(cfg);
            psi.ArgumentList.Add(GameExe);
            if (!ReShadeEnabled) psi.Environment["DISABLE_VK_LAYER_reshade_1"] = "1";

            var proc = Process.Start(psi);
            if (borderless && DisplayMode == "Borderless4K")
            {
                var engine = new BorderlessEngine(this, proc.Id);
                engine.Start();
            }
            return proc;
        }
    }
}
