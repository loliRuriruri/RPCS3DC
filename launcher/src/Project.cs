using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DragonCrownProEnhanced
{
    /// <summary>Project paths, graphics presets, diagnostics, backup/restore (Phase 1 Closeout).</summary>
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
        public string ToolsDir => Path.Combine(Root, "Tools");
        public string BorderlessStopFlag => Path.Combine(LogDir, "borderless_stop.flag");
        public string GameDir { get; private set; }                 // canonical disc root (contains PS3_GAME)
        public string GameExe => string.IsNullOrEmpty(GameDir) ? null : Path.Combine(GameDir, "PS3_GAME", "USRDIR", "EBOOT.BIN");
        public string GameParamSfo => string.IsNullOrEmpty(GameDir) ? null : Path.Combine(GameDir, "PS3_GAME", "PARAM.SFO");
        public string GameTitleId => GameParamSfo == null ? null : ReadSfoString(GameParamSfo, "TITLE_ID");
        public string TitleId { get; private set; } = "BCAS20298";

        public const string NetplayRequiredVersion = "01.09";
        public const int SaveBackupRetention = 20;

        public string DisplayMode { get; set; } = "Borderless4K";
        public bool ReShadeEnabled { get; set; } = false;          // Standard = RPCS3 only
        public bool ReShadeSilent { get; set; } = true;
        public bool VSync { get; set; } = true;
        public string GraphicsPreset { get; set; } = "Standard";   // Standard | ProEnhanced
        public string ResolutionProfile { get; set; } = "4K";      // 4K (300%) | 5K (400%)

        public const string PresetStandard = "Standard";
        public const string PresetProEnhanced = "ProEnhanced";
        public const string Res4K = "4K";
        public const string Res5K = "5K";
        public const string Profile4K = "Dragons_Crown/DC_PRO_4K";
        public const string Profile5K = "Dragons_Crown/DC_PRO_MAX_5K";
        public const string ProfileNetplay = "Dragons_Crown/DC_NETPLAY";
        public const string ProfileNetplaySafe = "Dragons_Crown/NETPLAY_SAFE";
        public const string ProfileCheat = "Dragons_Crown/DC_CHEAT_OFFLINE";

        /// <summary>Profile that PLAY / Local / Remote / KnownGood currently target.</summary>
        public string ActiveProfile => ResolutionProfile == Res5K ? Profile5K : Profile4K;
        public string ActiveProfileName => ResolutionProfile == Res5K ? "DC_PRO_MAX_5K" : "DC_PRO_4K";
        public string ResolutionShort => ResolutionProfile == Res5K ? "5K 400%" : "4K 300%";
        public string ResolutionLabel => ResolutionProfile == Res5K
            ? "5K / 400% — Super Sampling (DC_PRO_MAX_5K)"
            : "4K / 300% — Recommended (DC_PRO_4K)";
        public string GraphicsSummary => $"{GraphicsPreset} · {ResolutionShort}";

        public string ProfilePath(string name) => Path.Combine(ProfilesDir, name.Replace('/', Path.DirectorySeparatorChar), "config.yml");

        // ------------------------------------------------------------ root detection
        public static Project Discover(bool discoverGame = true)
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
            if (discoverGame) p.ResolveGame();
            p.LoadSettings();
            return p;
        }

        public void SaveRootHint()
        {
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "root.txt"), Root); } catch { }
        }

        // ------------------------------------------------------------ game discovery
        /// <summary>Re-runs discovery from scratch (RPCS3 games.yml → Launcher\game.txt → portable fallbacks).</summary>
        public void RefreshGameDiscovery()
        {
            string before = GameDir;
            GameDir = null;
            ResolveGame();
            Log($"[GameResolver] refresh: {before ?? "(none)"} -> {GameDir ?? "(not found)"}");
        }

        /// <summary>Stores a user-picked game folder (disc root or PS3_GAME) into Launcher\game.txt.</summary>
        public bool SetGameRoot(string path, out string error)
        {
            if (!TryResolveGamePath(path, out string root, out string detail))
            {
                error = "Dragon's Crown [BCAS20298] 경로가 아닙니다.\n" + detail;
                return false;
            }
            GameDir = root;
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "game.txt"), root); } catch { }
            Log($"[GameResolver] manual pick -> {root} ({detail})");
            error = null;
            return true;
        }

        private void ResolveGame()
        {
            var tried = new List<string>();
            foreach (var raw in GamesYmlCandidates()) if (Attempt(raw, allowSave: false)) return;
            foreach (var raw in FileHintCandidates()) if (Attempt(raw, allowSave: false)) return;
            foreach (var raw in PortableCandidates()) if (Attempt(raw, allowSave: true)) return;
            Log("[GameResolver] not found. tried=" + string.Join(" | ", tried.Take(20)));

            bool Attempt(string raw, bool allowSave)
            {
                string norm = NormalizePath(raw);
                if (TryResolveGamePath(norm, out string root, out string detail))
                {
                    GameDir = root;
                    // Only a portable-fallback hit is persisted; games.yml / game.txt results must not
                    // overwrite a user's manual pick (game.txt).
                    if (allowSave)
                    {
                        try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "game.txt"), root); } catch { }
                    }
                    Log($"[GameResolver] OK raw=[{raw}] normalized=[{norm}] -> {root} ({detail})" +
                        (allowSave ? " [saved to game.txt]" : ""));
                    return true;
                }
                tried.Add($"raw=[{raw}] norm=[{norm}] => {detail}");
                return false;
            }
        }

        /// <summary>All BCAS20298 entries from RPCS3's games.yml (RPCS3 may store disc root, PS3_GAME, or "PS3_GAME/./").</summary>
        private List<string> GamesYmlCandidates()
        {
            var list = new List<string>();
            try
            {
                string games = Path.Combine(Rpcs3Dir, "config", "games.yml");
                if (!File.Exists(games)) return list;
                foreach (var line in File.ReadAllLines(games))
                {
                    var m = Regex.Match(line, @"^\s*" + Regex.Escape(TitleId) + @"\s*:\s*(.+?)\s*$");
                    if (m.Success && m.Groups[1].Value.Trim().Length > 0) list.Add(m.Groups[1].Value.Trim());
                }
            }
            catch { }
            return list;
        }

        private List<string> FileHintCandidates()
        {
            var list = new List<string>();
            try
            {
                string hint = Path.Combine(AppContext.BaseDirectory, "game.txt");
                if (File.Exists(hint))
                {
                    string t = File.ReadAllText(hint).Trim();
                    if (t.Length > 0) list.Add(t);
                }
            }
            catch { }
            return list;
        }

        /// <summary>Bounded portable search: ROOT\Games / ROOT\Game / shallow scan of ROOT (never a whole-drive scan).</summary>
        private List<string> PortableCandidates()
        {
            var list = new List<string>();
            void Add(string d) { if (!string.IsNullOrEmpty(d)) list.Add(d); }

            foreach (var baseName in new[] { "Games", "Game" })
            {
                string b = Path.Combine(Root, baseName);
                if (!Directory.Exists(b)) continue;
                Add(Path.Combine(b, TitleId));
                Add(Path.Combine(b, "Dragon's Crown"));
                try { foreach (var d in Directory.GetDirectories(b)) Add(d); } catch { }
            }

            var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "RPCS3", "Backups", "Logs", "Launcher", "Tools", "Mods_Patches", "Profiles",
                "ReShade", "Saves", "Cheats", "remote-coop", "GitHub", ".git", "KnownGood"
            };
            int visited = 0;
            void Scan(string dir, int depth)
            {
                if (depth > 3 || visited > 1500) return;
                visited++;
                try
                {
                    if (Path.GetFileName(dir).Equals("PS3_GAME", StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(Path.Combine(dir, "PARAM.SFO"))) Add(Directory.GetParent(dir)?.FullName);
                        return;
                    }
                    if (File.Exists(Path.Combine(dir, "PS3_GAME", "PARAM.SFO"))) { Add(dir); return; }
                    foreach (var d in Directory.GetDirectories(dir))
                    {
                        if (skip.Contains(Path.GetFileName(d))) continue;
                        Scan(d, depth + 1);
                    }
                }
                catch { }
            }
            Scan(Root, 0);
            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Resolves a raw path (games.yml / game.txt / user pick) into a canonical disc root.
        /// Supports: disc root, direct PS3_GAME, "PS3_GAME/./", EBOOT.BIN path, USRDIR path,
        /// plus 1-2 parent fallbacks. Validates BCAS20298 through PS3_GAME\PARAM.SFO.
        /// </summary>
        public bool TryResolveGamePath(string rawPath, out string root, out string detail)
        {
            root = null;
            detail = "";
            string p = NormalizePath(rawPath);
            if (p.Length == 0) { detail = "empty path"; return false; }

            var probes = new List<(string candidate, string kind)>();

            // EBOOT.BIN file path → ...\PS3_GAME\USRDIR\EBOOT.BIN
            if (File.Exists(p) && Path.GetFileName(p).Equals("EBOOT.BIN", StringComparison.OrdinalIgnoreCase))
            {
                var usrdir = new DirectoryInfo(Path.GetDirectoryName(p) ?? "");
                var ps3game = usrdir.Parent;
                if (ps3game != null && ps3game.Name.Equals("PS3_GAME", StringComparison.OrdinalIgnoreCase))
                    probes.Add((ps3game.Parent?.FullName, "EBOOT.BIN path"));
                else
                    probes.Add((usrdir.FullName, "EBOOT.BIN parent"));
            }
            // direct disc root
            probes.Add((p, "disc root"));
            // PS3_GAME itself, USRDIR, parents
            if (Path.GetFileName(p).Equals("PS3_GAME", StringComparison.OrdinalIgnoreCase))
                probes.Add((Directory.GetParent(p)?.FullName, "PS3_GAME dir"));
            else if (Path.GetFileName(p).Equals("USRDIR", StringComparison.OrdinalIgnoreCase))
                probes.Add((Directory.GetParent(p)?.Parent?.FullName, "USRDIR dir"));
            else
                probes.Add((Directory.GetParent(p)?.FullName, "parent"));
            string gp1 = Directory.GetParent(p)?.FullName;
            string gp2 = gp1 == null ? null : Directory.GetParent(gp1)?.FullName;
            if (gp1 != null) probes.Add((gp1, "parent#2"));
            if (gp2 != null) probes.Add((gp2, "grandparent"));

            foreach (var (cand, kind) in probes)
            {
                if (string.IsNullOrEmpty(cand)) continue;
                string c = NormalizePath(cand);
                string exe = Path.Combine(c, "PS3_GAME", "USRDIR", "EBOOT.BIN");
                if (!File.Exists(exe)) { detail = $"{kind}: no EBOOT.BIN ({exe})"; continue; }
                if (!ValidateGameRoot(c, out string vdetail)) { detail = $"{kind}: {vdetail}"; continue; }
                root = c;
                detail = $"{kind}: OK ({vdetail})";
                return true;
            }
            return false;
        }

        /// <summary>Validates that the root contains BCAS20298 (TITLE_ID in PS3_GAME\PARAM.SFO).</summary>
        public bool ValidateGameRoot(string root, out string detail)
        {
            string sfo = Path.Combine(root, "PS3_GAME", "PARAM.SFO");
            if (!File.Exists(sfo)) { detail = "PARAM.SFO missing"; return false; }
            string titleId = ReadSfoString(sfo, "TITLE_ID");
            string appVer = ReadSfoString(sfo, "APP_VER");
            if (!string.IsNullOrEmpty(titleId))
            {
                if (!titleId.Equals(TitleId, StringComparison.OrdinalIgnoreCase))
                { detail = $"TITLE_ID mismatch: {titleId} != {TitleId}"; return false; }
                detail = $"TITLE_ID={titleId} APP_VER={appVer}";
                return true;
            }
            detail = "TITLE_ID not present in PARAM.SFO (accepted by games.yml key)";
            return true;
        }

        /// <summary>Quotes / slashes / "." segments / trailing separators (UNC-safe).</summary>
        public static string NormalizePath(string raw)
        {
            try
            {
                string s = (raw ?? "").Trim().Trim('"').Trim();
                if (s.Length == 0) return "";
                s = s.Replace('/', Path.DirectorySeparatorChar);
                try { s = Path.GetFullPath(s); } catch { }
                while (s.Length > 3 && (s.EndsWith(Path.DirectorySeparatorChar.ToString()) || s.EndsWith(Path.AltDirectorySeparatorChar.ToString())))
                    s = s.Substring(0, s.Length - 1);
                return s;
            }
            catch { return raw ?? ""; }
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
                        case "ResolutionProfile": ResolutionProfile = v == Res5K ? Res5K : Res4K; break;
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
                    "ResolutionProfile=" + ResolutionProfile,
                });
            }
            catch { }
        }

        public void SetResolution(string which)
        {
            ResolutionProfile = which == Res5K ? Res5K : Res4K;
            SaveSettings();
            Log($"resolution profile -> {ResolutionProfile} ({ActiveProfileName})");
        }

        // ------------------------------------------------------------ graphics presets
        /// <summary>Applies Graphics A (Standard) or Graphics B (Pro Enhanced) and persists it.</summary>
        public void ApplyGraphicsPreset(string preset)
        {
            GraphicsPreset = preset == PresetProEnhanced ? PresetProEnhanced : PresetStandard;
            ReShadeEnabled = GraphicsPreset == PresetProEnhanced;
            EnsureReShadePaths();
            ApplyReShadeSilent(ReShadeSilent);
            SaveSettings();
            Log($"graphics preset -> {GraphicsPreset} (ReShade {(ReShadeEnabled ? "ON" : "OFF")}, resolution {ResolutionProfile})");
        }

        /// <summary>
        /// Rewrites machine-specific ReShade paths to this project root so the launcher
        /// works from any install folder (portable / shared installs).
        /// </summary>
        public void EnsureReShadePaths()
        {
            try
            {
                string ini = Path.Combine(Rpcs3Dir, "ReShade.ini");
                if (!File.Exists(ini)) return;
                string text = File.ReadAllText(ini);
                string shaders = Path.Combine(Root, "Mods_Patches", "ReShade", "Shaders");
                string textures = Path.Combine(Root, "Mods_Patches", "ReShade", "Textures");
                string preset = Path.Combine(PresetsDir, GraphicsPreset == PresetProEnhanced ? "DC_PRO_ENHANCED.ini" : "DC_POSTFX_MINIMAL.ini");

                void SetKey(string key, string value)
                {
                    if (Regex.IsMatch(text, @"(?m)^" + Regex.Escape(key) + @"="))
                        text = Regex.Replace(text, @"(?m)^" + Regex.Escape(key) + @"=.*$", m => key + "=" + value);
                    else if (Regex.IsMatch(text, @"(?m)^\[GENERAL\]\s*$"))
                        text = Regex.Replace(text, @"(?m)^\[GENERAL\]\s*$", m => m.Value + "\r\n" + key + "=" + value);
                    else
                        text = text.TrimEnd() + "\r\n" + key + "=" + value + "\r\n";
                }
                SetKey("EffectSearchPaths", shaders);
                SetKey("TextureSearchPaths", textures);
                SetKey("PresetPath", preset);
                text = Regex.Replace(text, @"(?m)^IntermediateCachePath=.*\r?\n?", "");   // machine-specific temp cache
                File.WriteAllText(ini, text);
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
        /// <summary>
        /// Copies the live BCAS20298 savedata into Saves\Dragons_Crown\AutoBackup.
        /// Live savedata itself is never modified or deleted; only AutoBackup copies are
        /// pruned to the newest <see cref="SaveBackupRetention"/> generations.
        /// </summary>
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
            foreach (var old in Directory.GetDirectories(Path.Combine(SavesDir, "AutoBackup")).OrderByDescending(d => d).Skip(SaveBackupRetention))
            { try { Directory.Delete(old, true); } catch { } }
            Log($"save backup: {Path.GetFileName(dst)} ({files} files, retention {SaveBackupRetention})");
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

        /// <summary>Restores a settings backup. Files that would land outside the project root are skipped.</summary>
        public void RestoreSettingsBackup(string dir)
        {
            if (!Directory.Exists(dir)) throw new DirectoryNotFoundException(dir);
            string rootFull = Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            int n = 0, skipped = 0;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                string rel = f.Substring(dir.Length).TrimStart(Path.DirectorySeparatorChar);
                string target = Path.GetFullPath(Path.Combine(Root, rel));
                if (!target.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                {
                    skipped++;
                    Log("restore: skipped out-of-root path " + rel);
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(f, target, true);
                n++;
            }
            Log($"settings restored from {Path.GetFileName(dir)} ({n} files, {skipped} skipped)");
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
            ResolutionProfile = Res4K;
            ApplyGraphicsPreset(PresetStandard);
            Log("graphics reset (4K 300% / AF16 / MSAA Auto / stretch off / 16:9 / VSync Full / VBlank 60 / frame skip off)");
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

        // ------------------------------------------------------------ profile values
        /// <summary>Reads a top-level config value from a profile (e.g. "Resolution Scale").</summary>
        public string ProfileValue(string profileName, string key)
        {
            try
            {
                string p = ProfilePath(profileName);
                if (!File.Exists(p)) return null;
                var m = Regex.Match(File.ReadAllText(p), @"(?m)^\s*" + Regex.Escape(key) + @":\s*(\S.*)$");
                return m.Success ? m.Groups[1].Value.Trim() : null;
            }
            catch { return null; }
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
                GameParamSfo,
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

        /// <summary>rpcn.yml has a non-empty NPID and token/password (does NOT mean logged in).</summary>
        public bool RpcnConfigured()
        {
            try
            {
                string cfg = Path.Combine(Rpcs3Dir, "config", "rpcn.yml");
                if (!File.Exists(cfg)) return false;
                string t = File.ReadAllText(cfg);
                string npid = Regex.Match(t, @"(?mi)^\s*npid:\s*""?([^""\r\n]*)""?\s*$").Groups[1].Value.Trim();
                if (npid.Length == 0) return false;
                string token = Regex.Match(t, @"(?mi)^\s*token:\s*""?([^""\r\n]*)""?\s*$").Groups[1].Value.Trim();
                string pass = Regex.Match(t, @"(?mi)^\s*password:\s*""?([^""\r\n]*)""?\s*$").Groups[1].Value.Trim();
                return token.Length > 0 || pass.Length > 0;
            }
            catch { return false; }
        }

        /// <summary>
        /// Login state is not exposed by RPCS3 in a stable machine-readable way.
        /// We only report a login line if the RPCS3 log actually contains one;
        /// otherwise the state stays "Not verified" (never assume "connected").
        /// </summary>
        public string RpcnLoginStatus()
        {
            try
            {
                string text = ReadShared(Path.Combine(Rpcs3Dir, "log", "RPCS3.log"));
                if (!string.IsNullOrEmpty(text))
                {
                    int start = Math.Max(0, text.Length - 200_000);
                    string tail = text.Substring(start);
                    if (Regex.IsMatch(tail, @"(?i)RPCN[^\r\n]{0,80}(logged in|login successful|signed in)"))
                        return "Login seen in log (verify in RPCS3 GUI)";
                }
            }
            catch { }
            return "Not verified";
        }

        public sealed class CheatPatchInfo
        {
            public bool CheatEntries;
            public string CheatEnabled = "NO";   // YES | NO | UNKNOWN
            public bool PatchEntries;
            public string PatchEnabled = "NO";   // YES | NO | UNKNOWN
        }

        private bool HasTitleBlock(string file)
        {
            try { return Regex.IsMatch(File.ReadAllText(file), @"(?mi)^" + Regex.Escape(TitleId) + @"\s*:"); }
            catch { return false; }
        }

        private string ParseEnabledState(string file)
        {
            try
            {
                string text = File.ReadAllText(file);
                var m = Regex.Match(text, @"(?mis)^" + Regex.Escape(TitleId) + @"\s*:(.*?)(?=^\S|\Z)");
                string block = m.Success ? m.Groups[1].Value : text;
                if (Regex.IsMatch(block, @"(?mi)^\s*Enabled:\s*true\s*$")) return "YES";
                if (Regex.IsMatch(block, @"(?mi)^\s*Enabled:\s*false\s*$")) return "NO";
                return "UNKNOWN";
            }
            catch { return "UNKNOWN"; }
        }

        /// <summary>Distinguishes "entries exist" from "entries are enabled" (UNKNOWN when the file format is not recognized).</summary>
        public CheatPatchInfo CheatPatchStatus()
        {
            var info = new CheatPatchInfo();
            string cheats = Path.Combine(Rpcs3Dir, "config", "cheats.yml");
            string patches = Path.Combine(Rpcs3Dir, "config", "patch_config.yml");
            info.CheatEntries = File.Exists(cheats) && HasTitleBlock(cheats);
            info.CheatEnabled = info.CheatEntries ? ParseEnabledState(cheats) : "NO";
            info.PatchEntries = File.Exists(patches) && HasTitleBlock(patches);
            info.PatchEnabled = info.PatchEntries ? ParseEnabledState(patches) : "NO";
            return info;
        }

        /// <summary>true = an enabled cheat/patch exists, false = none, null = cannot determine.</summary>
        public bool? EnabledCheatsOrPatches()
        {
            var s = CheatPatchStatus();
            if (s.CheatEnabled == "YES" || s.PatchEnabled == "YES") return true;
            if (s.CheatEnabled == "UNKNOWN" || s.PatchEnabled == "UNKNOWN") return null;
            return false;
        }

        /// <summary>Compatibility helper (true only when an enabled cheat/patch is positively detected).</summary>
        public bool CheatsOrPatchesEnabled() => EnabledCheatsOrPatches() == true;

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
            bool installed = File.Exists(@"C:\Program Files\Sunshine\sunshine.exe") ||
                             File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sunshine", "sunshine.exe"));
            return installed ? "설치됨(중지 상태)" : "미설치";
        }

        public bool SunshineRunning()
        {
            string s = SunshineStatus();
            return s == "실행 중" || s == "서비스 실행 중";
        }

        public string FirewallScript => Path.Combine(ToolsDir, "firewall_rpcs3_allow.cmd");

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

            // --- core
            Add("RPCS3 executable", File.Exists(Rpcs3Exe), Rpcs3Exe, "RPCS3를 <ROOT>\\RPCS3 에 설치하세요.");
            Add("KnownGood fallback", File.Exists(KnownGoodExe), File.Exists(KnownGoodExe) ? "있음" : "없음",
                "MAINTENANCE → KnownGood 실행을 쓰려면 스냅샷이 필요합니다.");
            Add("Game path", GameExe != null && File.Exists(GameExe),
                GameExe ?? "미탐지",
                "SETTINGS → Advanced → [게임 폴더 선택] 또는 RPCS3 게임 목록에 추가(config\\games.yml).");
            Add("Game version (BCAS20298 v" + NetplayRequiredVersion + ")", GameVersion() == NetplayRequiredVersion, "v" + GameVersion(),
                "RPCN / Netplay Safe 에는 v" + NetplayRequiredVersion + " 가 필요합니다. 공식 업데이트를 적용하세요.");
            Add("Firmware", FirmwareVersion() != "미확인", FirmwareVersion(), "RPCS3에 PS3 firmware를 설치하세요.");
            Add("PPU hash", PpuHash().StartsWith("PPU-"), PpuHash(), "게임을 한 번 실행하면 기록됩니다.");
            Add("Save data", Directory.Exists(Path.Combine(Rpcs3Dir, "dev_hdd0", "home", "00000001", "savedata")),
                Path.Combine(Rpcs3Dir, "dev_hdd0", "home", "00000001", "savedata"), "세이브 폴더가 없습니다. 게임을 한 번 저장하세요.");

            string p1 = ControllerHandler(1), p2 = ControllerHandler(2);
            Add("Controller 1", p1 != "없음" && p1 != "Null", p1, "RPCS3 GUI → 게임패드 설정에서 1P 패드를 설정하세요.");
            Add("Controller 2 (Local/Remote Co-op)", p2 != "없음" && p2 != "Null", p2,
                "2P 패드가 없습니다. Local/Remote Co-op 에는 2P 설정이 필요합니다.");

            // --- display / graphics (read from the *selected* profile, not hardcoded)
            Add("Display mode", true, DisplayMode, "");
            Add("Resolution profile", true, ResolutionLabel, "");
            string prof = ActiveProfile, profName = ActiveProfileName;
            string expectScale = ResolutionProfile == Res5K ? "400" : "300";
            string scale = ProfileValue(prof, "Resolution Scale") ?? "?";
            string aspect = ProfileValue(prof, "Aspect ratio") ?? "?";
            string stretch = ProfileValue(prof, "Stretch To Display Area") ?? "?";
            string af = ProfileValue(prof, "Anisotropic Filter Override") ?? "?";
            string vsync = ProfileValue(prof, "VSync Mode") ?? "?";
            string vblank = ProfileValue(prof, "Vblank Rate") ?? "?";
            string frameskip = ProfileValue(prof, "Enable Frame Skip") ?? "?";
            Add("Resolution Scale (" + profName + ")", scale == expectScale, scale + "%",
                $"{profName} 의 Resolution Scale 이 {expectScale} 가 아닙니다. TOOLS → Reset Graphics.");
            Add("Aspect ratio (" + profName + ")", aspect == "16:9", aspect, "16:9 로 복원하세요.");
            Add("Stretch To Display Area", stretch == "false", stretch, "Stretch Off 여야 좌표/화면비가 유지됩니다.");
            Add("Anisotropic Filter Override", af == "16", af, "Graphics A 기준 AF 16x.");
            Add("VSync Mode", vsync == "Full", vsync, "VSync Full (VBlank 60) 기준입니다.");
            Add("Vblank Rate", vblank == "60", vblank, "VBlank 60 고정 (120/240 금지).");
            Add("Enable Frame Skip", frameskip == "false", frameskip, "Frame Skip Off 기준입니다.");

            // --- ReShade
            Add("ReShade runtime", ReShadeVersion() != "없음", ReShadeVersion(), "reshade.me 에서 ReShade 6.8.0 Addon(Vulkan)을 설치하세요.");
            Add("ReShade preset (Pro Enhanced)", File.Exists(Path.Combine(PresetsDir, "DC_PRO_ENHANCED.ini")),
                Path.Combine(PresetsDir, "DC_PRO_ENHANCED.ini"), "프리셋 파일이 없습니다. TOOLS → Backup/Restore 로 복원하세요.");
            Add("ReShade shaders", File.Exists(Path.Combine(ShadersDir, "CAS.fx")) && File.Exists(Path.Combine(ShadersDir, "Deband.fx")),
                ShadersDir, "셰이더 파일이 없습니다(공식 reshade-shaders 에서 복사).");
            Add("ReShade Silent", File.Exists(Path.Combine(Rpcs3Dir, "ReShade.ini")), ReShadeSilent ? "ON" : "OFF",
                "ReShade.ini 가 없습니다.");

            // --- multiplayer
            Add("Sunshine (Remote Co-op)", SunshineRunning(), SunshineStatus(),
                "Sunshine을 설치/실행하세요(https://github.com/LizardByte/Sunshine). Moonlight로 접속합니다. (Parsec 등 대체 가능)");
            Add("Network adapter", true, "VPN/이중 NIC 주의", "유선만 사용하고 VPN을 끄면 RPCN 안정성이 올라갑니다.");
            Add("RPCN Configured", RpcnConfigured(), RpcnConfigured() ? "YES" : "NO",
                "RPCS3 → RPCN → Create Account 로 계정을 만들고 로그인하세요.");
            Add("RPCN Login", RpcnLoginStatus() != "Not verified", RpcnLoginStatus(),
                "로그인 여부는 자동 검증이 불가능합니다. RPCS3 GUI → RPCN 에서 확인하세요.");

            var cp = CheatPatchStatus();
            Add("Cheat entries found", !cp.CheatEntries, cp.CheatEntries ? "YES" : "NO",
                "치트 항목이 존재합니다. NETPLAY 전에 비활성화하세요.");
            Add("Enabled cheat", cp.CheatEnabled != "YES", cp.CheatEnabled,
                "활성화된 치트가 있습니다. NETPLAY 전에 끄세요.");
            Add("Patch entries found", !cp.PatchEntries, cp.PatchEntries ? "YES" : "NO",
                "패치 항목이 존재합니다. NETPLAY 전에 비활성화하세요.");
            Add("Enabled patch", cp.PatchEnabled != "YES", cp.PatchEnabled,
                "활성화된 패치가 있습니다. NETPLAY 전에 끄세요.");

            // --- profiles
            foreach (var profName2 in new[] { Profile4K, Profile5K, ProfileNetplay, ProfileNetplaySafe, ProfileCheat })
                Add("Profile " + profName2.Split('/').Last(), File.Exists(ProfilePath(profName2)), profName2,
                    "TOOLS → Backup/Restore 로 복원하세요.");

            Log("diagnostics: " + list.Count(i => i.Ok) + "/" + list.Count + " ok");
            return list;
        }

        // ------------------------------------------------------------ launch
        /// <summary>
        /// Starts RPCS3 with a generated runtime config.
        /// forceReShadeOff is a temporary runtime override (Netplay Safe): the Vulkan layer is
        /// disabled for this process only and the user's saved graphics preset is not modified.
        /// </summary>
        public Process Launch(string profileName, bool useKnownGood, bool borderless, bool forceReShadeOff = false)
        {
            string cfg = BuildRuntimeConfig(profileName);
            string exe = useKnownGood && File.Exists(KnownGoodExe) ? KnownGoodExe : Rpcs3Exe;
            if (GameExe == null || !File.Exists(GameExe))
                throw new FileNotFoundException(
                    "Dragon's Crown [BCAS20298] 경로를 찾을 수 없습니다.\n" +
                    "게임 폴더를 직접 선택하거나 RPCS3 게임 목록을 확인하세요.\n" +
                    "(game.txt / games.yml / ROOT\\Games 를 순서대로 탐색했습니다)");

            bool blockReShade = forceReShadeOff || !ReShadeEnabled;
            Log($"launch: profile={profileName} mode={DisplayMode} preset={GraphicsPreset} resolution={ResolutionProfile} " +
                $"vsync={(VSync ? "Full" : "Disabled")} reshade={(ReShadeEnabled ? "on" : "off")} " +
                $"forcedReShadeOff={forceReShadeOff} silent={ReShadeSilent} knownGood={useKnownGood}");
            Log($"  config: {cfg}");
            Log($"  build : {Rpcs3Build()} | game={TitleId} v{GameVersion()} | ppu={PpuHash()}");

            var psi = new ProcessStartInfo { FileName = exe, WorkingDirectory = Path.GetDirectoryName(exe), UseShellExecute = false, CreateNoWindow = true };
            psi.ArgumentList.Add("--config");
            psi.ArgumentList.Add(cfg);
            psi.ArgumentList.Add(GameExe);
            if (blockReShade) psi.Environment["DISABLE_VK_LAYER_reshade_1"] = "1";
            else psi.Environment.Remove("DISABLE_VK_LAYER_reshade_1");

            var proc = Process.Start(psi);
            if (borderless && DisplayMode == "Borderless4K")
            {
                try { File.Delete(BorderlessStopFlag); } catch { }   // fresh watcher session
                new BorderlessEngine(this, proc.Id).Start();
            }
            return proc;
        }
    }
}
