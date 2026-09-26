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
    /// <summary>Project paths, graphics presets, diagnostics, backup/restore (Phase 1).</summary>
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
        public string PresetsDir => Path.Combine(ReShadeDir, "Presets");
        public string ShadersDir => Path.Combine(Root, "Mods_Patches", "ReShade", "Shaders");
        public string BackupDir => Path.Combine(Root, "Backups");
        public string GameDir { get; private set; }
        public string GameExe => string.IsNullOrEmpty(GameDir) ? null : Path.Combine(GameDir, "PS3_GAME", "USRDIR", "EBOOT.BIN");
        public string TitleId { get; private set; } = "BCAS20298";

        public string DisplayMode { get; set; } = "Borderless4K";
        public bool ReShadeEnabled { get; set; } = false;          // Standard = RPCS3 only
        public bool ReShadeSilent { get; set; } = true;
        public bool VSync { get; set; } = true;
        public string GraphicsPreset { get; set; } = "Standard";   // Standard | ProEnhanced

        public const string PresetStandard = "Standard";
        public const string PresetProEnhanced = "ProEnhanced";
        public const string Profile4K = "Dragons_Crown/DC_PRO_4K";
        public const string Profile5K = "Dragons_Crown/DC_PRO_MAX_5K";
        public const string ProfileNetplay = "Dragons_Crown/DC_NETPLAY";
        public const string ProfileNetplaySafe = "Dragons_Crown/NETPLAY_SAFE";
        public const string ProfileCheat = "Dragons_Crown/DC_CHEAT_OFFLINE";

        public string ProfilePath(string name) => Path.Combine(ProfilesDir, name.Replace('/', Path.DirectorySeparatorChar), "config.yml");

        // ------------------------------------------------------------ root detection
        public static Project Discover()
        {
            var p = new Project();
            string exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            foreach (var c in new[] { Directory.GetParent(exeDir)?.FullName, exeDir })
            {
                if (!string.IsNullOrEmpty(c) && File.Exists(Path.Combine(c, "RPCS3", "rpcs3.exe"))) { p.Root = c; break; }
            }
            if (p.Root == null)
            {
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
            try
            {
                string games = Path.Combine(Rpcs3Dir, "config", "games.yml");
                if (File.Exists(games))
                {
                    foreach (var line in File.ReadAllLines(games))
                    {
                        var m = Regex.Match(line, @"^\s*" + TitleId + @"\s*:\s*(.+?)\s*$");
                        if (!m.Success) continue;
                        string dir = m.Groups[1].Value.Trim().Trim('"').Replace('/', Path.DirectorySeparatorChar);
                        if (!Directory.Exists(dir)) continue;
                        if (File.Exists(Path.Combine(dir, "PS3_GAME", "USRDIR", "EBOOT.BIN"))) { GameDir = dir; return; }
                        var parent = Directory.GetParent(dir)?.FullName;
                        if (parent != null && File.Exists(Path.Combine(parent, "PS3_GAME", "USRDIR", "EBOOT.BIN"))) { GameDir = parent; return; }
                    }
                }
            }
            catch { }
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
                        case "GraphicsPreset": GraphicsPreset = v; break;
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
                    "GraphicsPreset=" + GraphicsPreset,
                });
            }
            catch { }
        }

        // ------------------------------------------------------------ graphics presets
        /// <summary>Applies Graphics A (Standard) or Graphics B (Pro Enhanced).</summary>
        public void ApplyGraphicsPreset(string preset)
        {
            GraphicsPreset = preset;
            ReShadeEnabled = preset == PresetProEnhanced;
            string ini = Path.Combine(Rpcs3Dir, "ReShade.ini");
            if (File.Exists(ini))
            {
                string text = File.ReadAllText(ini);
                string target = Path.Combine(PresetsDir, preset == PresetProEnhanced ? "DC_PRO_ENHANCED.ini" : "DC_POSTFX_MINIMAL.ini");
                if (Regex.IsMatch(text, @"(?m)^PresetPath="))
                    text = Regex.Replace(text, @"(?m)^PresetPath=.*$", m => "PresetPath=" + target);
                else
                    text = text.Replace("[GENERAL]", "[GENERAL]\r\nPresetPath=" + target);
                File.WriteAllText(ini, text);
            }
            ApplyReShadeSilent(ReShadeSilent);
            SaveSettings();
            Log($"graphics preset -> {preset} (ReShade {(ReShadeEnabled ? "ON" : "OFF")})");
        }

        // ------------------------------------------------------------ runtime config
        public string BuildRuntimeConfig(string profileName)
        {
            string src = ProfilePath(profileName);
            if (!File.Exists(src)) throw new FileNotFoundException("프로필을 찾을 수 없습니다: " + src);
            Directory.CreateDirectory(RuntimeDir);
            string dst = Path.Combine(RuntimeDir, $"{profileName.Replace('/', '_')}__{DisplayMode}.yml");
            string text = File.ReadAllText(src);
            text = Regex.Replace(text, @"(?m)^(\s*Start games in fullscreen mode:\s*).*$", "${1}" + (DisplayMode == "Fullscreen" ? "true" : "false"));
            text = Regex.Replace(text, @"(?m)^(\s*VSync Mode:\s*).*$", "${1}" + (VSync ? "Full" : "Disabled"));
            File.WriteAllText(dst, text, new UTF8Encoding(false));
            return dst;
        }

        // ------------------------------------------------------------ ReShade silent
        public void ApplyReShadeSilent(bool silent)
        {
            string ini = Path.Combine(Rpcs3Dir, "ReShade.ini");
            if (!File.Exists(ini)) return;
            Directory.CreateDirectory(Path.Combine(ReShadeDir, "Backup"));
            string backup = Path.Combine(ReShadeDir, "Backup", "ReShade.ini.before_silent");
            if (!File.Exists(backup)) File.Copy(ini, backup);
            string text = File.ReadAllText(ini);
            string overlay =
                "[OVERLAY]\r\n" +
                "TutorialProgress=" + (silent ? 4 : 0) + "\r\n" +
                "ShowClock=0\r\nShowFPS=0\r\nShowFrameTime=0\r\nShowPresetName=0\r\n" +
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
            string savedata = Path.Combine(Rpcs3Dir, "dev_hdd0", "home", "00000001", "savedata");
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
                    manifest.Add(f.Substring(dst.Length).TrimStart(Path.DirectorySeparatorChar) + "  " + Sha256(f));
                    files++;
                }
            }
            File.WriteAllLines(Path.Combine(dst, "BACKUP_MANIFEST.txt"), manifest);
            foreach (var old in Directory.GetDirectories(Path.Combine(SavesDir, "AutoBackup")).OrderByDescending(d => d).Skip(20))
            { try { Directory.Delete(old, true); } catch { } }
            Log($"save backup: {Path.GetFileName(dst)} ({files} files)");
            return dst;
        }

        // ------------------------------------------------------------ settings backup / restore
        public string BackupSettings(string label)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string dst = Path.Combine(BackupDir, $"{stamp}_{label}");
            Directory.CreateDirectory(dst);
            void CopyFile(string rel)
            {
                string src = Path.Combine(Root, rel);
                if (!File.Exists(src)) return;
                string target = Path.Combine(dst, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(src, target, true);
            }
            CopyFile(Path.Combine("RPCS3", "config", "config.yml"));
            CopyFile(Path.Combine("RPCS3", "config", "custom_configs", TitleId + ".yml"));
            CopyFile(Path.Combine("RPCS3", "ReShade.ini"));
            CopyFile(Path.Combine("RPCS3", "GuiConfigs", "CurrentSettings.ini"));
            foreach (var p in new[] { "DC_PRO_4K", "DC_PRO_MAX_5K", "DC_NETPLAY", "NETPLAY_SAFE", "DC_CHEAT_OFFLINE" })
                CopyFile(Path.Combine("Profiles", "Dragons_Crown", p, "config.yml"));
            CopyFile(Path.Combine("ReShade", "Presets", "DC_PRO_ENHANCED.ini"));
            var inputs = Path.Combine(Rpcs3Dir, "config", "input_configs");
            if (Directory.Exists(inputs))
            {
                string t = Path.Combine(dst, "RPCS3", "config", "input_configs");
                CopyDir(inputs, t);
            }
            Log("settings backup -> " + dst);
            return dst;
        }

        public List<string> ListSettingsBackups()
        {
            if (!Directory.Exists(BackupDir)) return new List<string>();
            return Directory.GetDirectories(BackupDir)
                .Where(d => File.Exists(Path.Combine(d, "RPCS3", "config", "config.yml")) || File.Exists(Path.Combine(d, "Profiles", "Dragons_Crown", "DC_PRO_4K", "config.yml")))
                .OrderByDescending(d => d).ToList();
        }

        public void RestoreSettingsBackup(string dir)
        {
            if (!Directory.Exists(dir)) throw new DirectoryNotFoundException(dir);
            int n = 0;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                string rel = f.Substring(dir.Length).TrimStart(Path.DirectorySeparatorChar);
                string target = Path.Combine(Root, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(f, target, true);
                n++;
            }
            Log($"settings restored from {Path.GetFileName(dir)} ({n} files)");
        }

        public void ResetGraphics()
        {
            foreach (var name in new[] { "DC_PRO_4K", "DC_PRO_MAX_5K", "DC_NETPLAY", "NETPLAY_SAFE", "DC_CHEAT_OFFLINE" })
            {
                string p = ProfilePath("Dragons_Crown/" + name);
                if (!File.Exists(p)) continue;
                string t = File.ReadAllText(p);
                t = Regex.Replace(t, @"(?m)^(\s*Anisotropic Filter Override:\s*).*$", "${1}16");
                t = Regex.Replace(t, @"(?m)^(\s*MSAA:\s*).*$", "${1}Auto");
                t = Regex.Replace(t, @"(?m)^(\s*Stretch To Display Area:\s*).*$", "${1}false");
                t = Regex.Replace(t, @"(?m)^(\s*Aspect ratio:\s*).*$", "${1}16:9");
                t = Regex.Replace(t, @"(?m)^(\s*VSync Mode:\s*).*$", "${1}Full");
                t = Regex.Replace(t, @"(?m)^(\s*Vblank Rate:\s*).*$", "${1}60");
                t = Regex.Replace(t, @"(?m)^(\s*Enable Frame Skip:\s*).*$", "${1}false");
                File.WriteAllText(p, t);
            }
            ApplyGraphicsPreset(PresetStandard);
            Log("graphics reset (AF16 / MSAA Auto / stretch off / 16:9 / VSync Full / VBlank 60 / frame skip off)");
        }

        public void ResetMultiplayer()
        {
            foreach (var name in new[] { "DC_NETPLAY", "NETPLAY_SAFE" })
            {
                string p = ProfilePath("Dragons_Crown/" + name);
                if (!File.Exists(p)) continue;
                string t = File.ReadAllText(p);
                t = Regex.Replace(t, @"(?m)^(\s*Internet enabled:\s*).*$", "${1}Connected");
                t = Regex.Replace(t, @"(?m)^(\s*PSN status:\s*).*$", "${1}RPCN");
                t = Regex.Replace(t, @"(?m)^(\s*UPNP Enabled:\s*).*$", "${1}false");
                t = Regex.Replace(t, @"(?m)^(\s*Clans Enabled:\s*).*$", "${1}false");
                t = Regex.Replace(t, @"(?m)^(\s*Bind address:\s*).*$", "${1}0.0.0.0");
                File.WriteAllText(p, t);
            }
            Log("multiplayer reset (RPCN on / UPNP off / clans off / bind 0.0.0.0)");
        }

        // ------------------------------------------------------------ info
        public string Rpcs3Build()
        {
            string text = ReadShared(Path.Combine(Rpcs3Dir, "log", "RPCS3.log"));
            if (text != null)
            {
                var m = Regex.Match(text, @"RPCS3 (v[0-9][^\r\n|]*)");
                if (m.Success) return m.Groups[1].Value.Trim();
            }
            return File.Exists(Rpcs3Exe) ? "rpcs3.exe 있음" : "없음";
        }

        public string PpuHash()
        {
            string text = ReadShared(Path.Combine(Rpcs3Dir, "log", "RPCS3.log"));
            if (text != null)
            {
                var m = Regex.Match(text, @"PPU executable hash:\s*(PPU-[0-9a-f]{40})");
                if (m.Success) return m.Groups[1].Value;
            }
            return "미확인";
        }

        public string FirmwareVersion()
        {
            try
            {
                string v = Path.Combine(Rpcs3Dir, "dev_flash", "vsh", "etc", "version.txt");
                if (File.Exists(v))
                {
                    var m = Regex.Match(File.ReadAllText(v), @"release:(\d+)\.(\d+)");
                    if (m.Success) return m.Groups[1].Value + "." + m.Groups[2].Value;
                }
            }
            catch { }
            return "미확인";
        }

        public string GameVersion()
        {
            foreach (var sfo in new[]
            {
                Path.Combine(Rpcs3Dir, "dev_hdd0", "game", TitleId, "PARAM.SFO"),
                GameDir == null ? null : Path.Combine(GameDir, "PS3_GAME", "PARAM.SFO"),
            })
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
                    if (Encoding.UTF8.GetString(d, kStart, kEnd - kStart) != key) continue;
                    int vStart = dataTable + valOff;
                    return Encoding.UTF8.GetString(d, vStart, Math.Min(len, d.Length - vStart)).TrimEnd('\0');
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
                if (File.Exists(dll)) return FileVersionInfo.GetVersionInfo(dll).FileVersion ?? "확인됨";
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

        /// <summary>Returns the configured pad handler for a player (1..4) or "없음".</summary>
        public string ControllerHandler(int player)
        {
            try
            {
                string cfg = Path.Combine(Rpcs3Dir, "config", "input_configs", "global", "Default.yml");
                if (!File.Exists(cfg)) return "없음";
                var text = File.ReadAllText(cfg);
                var m = Regex.Match(text, @"(?ms)^Player " + player + @" Input:.*?^\s*Handler:\s*""?([^""\r\n]+)""?");
                if (m.Success) return m.Groups[1].Value.Trim();
            }
            catch { }
            return "없음";
        }

        public bool IsProcessRunning(string name)
        {
            try { return Process.GetProcesses().Any(p => { try { return p.ProcessName.Equals(name, StringComparison.OrdinalIgnoreCase); } catch { return false; } }); }
            catch { return false; }
        }

        public string SunshineStatus()
        {
            bool proc = IsProcessRunning("sunshine");
            bool svc = false;
            try
            {
                var psi = new ProcessStartInfo("sc.exe", "query SunshineService") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using var p = Process.Start(psi);
                string o = p.StandardOutput.ReadToEnd();
                svc = o.Contains("RUNNING");
            }
            catch { }
            if (proc || svc) return proc ? "실행 중" : "서비스 실행 중";
            // installed but not running?
            bool installed = File.Exists(@"C:\Program Files\Sunshine\sunshine.exe") ||
                             File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sunshine", "sunshine.exe"));
            return installed ? "설치됨(중지 상태)" : "미설치";
        }


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

        public void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                File.AppendAllText(LauncherLog, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}\r\n");
            }
            catch { }
        }

        // ------------------------------------------------------------ diagnostics
        public class DiagItem
        {
            public string Name; public bool Ok; public string Detail; public string Hint;
        }

        public List<DiagItem> RunDiagnostics()
        {
            var list = new List<DiagItem>();
            void Add(string name, bool ok, string detail, string hint = "")
                => list.Add(new DiagItem { Name = name, Ok = ok, Detail = detail, Hint = ok ? "" : hint });

            Add("RPCS3 executable", File.Exists(Rpcs3Exe), Rpcs3Exe, "RPCS3를 <ROOT>\\RPCS3 에 설치하세요.");
            Add("KnownGood fallback", File.Exists(KnownGoodExe), File.Exists(KnownGoodExe) ? "있음" : "없음",
                "MAINTENANCE → KnownGood 실행을 쓰려면 스냅샷이 필요합니다.");
            Add("Game path", GameExe != null && File.Exists(GameExe), GameExe ?? "미탐지",
                "RPCS3 게임 목록에 Dragon's Crown을 추가하세요(config\\games.yml).");
            Add("Game version", GameVersion().StartsWith("01."), GameVersion(), "공식 업데이트(v1.09)를 적용하세요.");
            Add("Firmware", FirmwareVersion() != "미확인", FirmwareVersion(), "RPCS3에 PS3 firmware를 설치하세요.");
            Add("PPU hash", PpuHash().StartsWith("PPU-"), PpuHash(), "게임을 한 번 실행하면 기록됩니다.");
            Add("Save data", Directory.Exists(Path.Combine(Rpcs3Dir, "dev_hdd0", "home", "00000001", "savedata")),
                Path.Combine(Rpcs3Dir, "dev_hdd0", "home", "00000001", "savedata"), "세이브 폴더가 없습니다. 게임을 한 번 저장하세요.");

            string p1 = ControllerHandler(1), p2 = ControllerHandler(2);
            Add("Controller 1", p1 != "없음" && p1 != "Null", p1, "RPCS3 GUI → 게임패드 설정에서 1P 패드를 설정하세요.");
            Add("Controller 2 (Local/Remote Co-op)", p2 != "없음" && p2 != "Null", p2,
                "2P 패드가 없습니다. Local/Remote Co-op 에는 2P 설정이 필요합니다.");

            Add("Display mode", true, DisplayMode, "");
            Add("Resolution / Aspect", File.Exists(ProfilePath(Profile4K)), "Scale 300% · 3840x2160 · 16:9 · Stretch Off",
                "프로필이 없습니다. TOOLS → Reset Graphics 를 실행하세요.");
            Add("Anisotropic Filter", true, "16x (Graphics A)", "");

            Add("ReShade runtime", ReShadeVersion() != "없음", ReShadeVersion(), "reshade.me 에서 ReShade 6.8.0 Addon(Vulkan)을 설치하세요.");
            Add("ReShade preset (Pro Enhanced)", File.Exists(Path.Combine(PresetsDir, "DC_PRO_ENHANCED.ini")),
                Path.Combine(PresetsDir, "DC_PRO_ENHANCED.ini"), "프리셋 파일이 없습니다. TOOLS → Restore Defaults 로 복원하세요.");
            Add("ReShade shaders", File.Exists(Path.Combine(ShadersDir, "CAS.fx")) && File.Exists(Path.Combine(ShadersDir, "Deband.fx")),
                ShadersDir, "셰이더 파일이 없습니다(공식 reshade-shaders 에서 복사).");
            Add("ReShade Silent", File.Exists(Path.Combine(Rpcs3Dir, "ReShade.ini")), ReShadeSilent ? "ON" : "OFF",
                "ReShade.ini 가 없습니다.");

            Add("Sunshine (Remote Co-op)", SunshineStatus() == "실행 중" || SunshineStatus() == "서비스 실행 중",
                SunshineStatus(), "Sunshine을 설치/실행하세요(https://github.com/LizardByte/Sunshine). Moonlight로 접속합니다.");
            Add("Network adapter", true, "VPN/이중 NIC 주의", "유선만 사용하고 VPN을 끄면 RPCN 안정성이 올라갑니다.");
            Add("RPCN configuration", RpcnConfigured(), RpcnConfigured() ? "설정됨" : "미설정",
                "RPCS3 → RPCN → Create Account 로 계정을 만들고 로그인하세요.");
            Add("Cheats / patches", !CheatsOrPatchesEnabled(), CheatsOrPatchesEnabled() ? "항목 있음" : "OFF",
                "NETPLAY 전에 치트/패치를 모두 비활성화하세요.");

            foreach (var prof in new[] { Profile4K, Profile5K, ProfileNetplay, ProfileNetplaySafe, ProfileCheat })
                Add("Profile " + prof.Split('/').Last(), File.Exists(ProfilePath(prof)), prof, "TOOLS → Restore Defaults 로 복원하세요.");

            Log("diagnostics: " + list.Count(i => i.Ok) + "/" + list.Count + " ok");
            return list;
        }

        // ------------------------------------------------------------ launch
        public Process Launch(string profileName, bool useKnownGood, bool borderless)
        {
            string cfg = BuildRuntimeConfig(profileName);
            string exe = useKnownGood && File.Exists(KnownGoodExe) ? KnownGoodExe : Rpcs3Exe;
            if (GameExe == null || !File.Exists(GameExe)) throw new FileNotFoundException("게임 덤프를 찾을 수 없습니다 (games.yml 확인).");

            Log($"launch: profile={profileName} mode={DisplayMode} preset={GraphicsPreset} vsync={(VSync ? "Full" : "Disabled")} " +
                $"reshade={(ReShadeEnabled ? "on" : "off")} silent={ReShadeSilent} knownGood={useKnownGood}");
            Log($"  config: {cfg}");
            Log($"  build : {Rpcs3Build()} | game={TitleId} v{GameVersion()} | ppu={PpuHash()}");

            var psi = new ProcessStartInfo { FileName = exe, WorkingDirectory = Path.GetDirectoryName(exe), UseShellExecute = false, CreateNoWindow = true };
            psi.ArgumentList.Add("--config");
            psi.ArgumentList.Add(cfg);
            psi.ArgumentList.Add(GameExe);
            if (!ReShadeEnabled) psi.Environment["DISABLE_VK_LAYER_reshade_1"] = "1";

            var proc = Process.Start(psi);
            if (borderless && DisplayMode == "Borderless4K")
                new BorderlessEngine(this, proc.Id).Start();
            return proc;
        }
    }

    /// <summary>Minimal screen info helper (avoids a WinForms reference).</summary>
    internal static class SystemWindowsFormsScreenShim
    {
        public static List<string> Screens()
        {
            var result = new List<string>();
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"Add-Type -AssemblyName System.Windows.Forms; " +
                    "[System.Windows.Forms.Screen]::AllScreens | ForEach-Object { $_.DeviceName + ' ' + $_.Bounds.Width + 'x' + $_.Bounds.Height }\"" )
                { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using var p = Process.Start(psi);
                string o = p.StandardOutput.ReadToEnd();
                foreach (var line in o.Split('\n'))
                    if (line.Trim().Length > 0) result.Add(line.Trim());
            }
            catch { }
            return result;
        }
    }
}
