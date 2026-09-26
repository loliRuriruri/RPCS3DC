using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DragonCrownRemoteCoop
{
    /// <summary>
    /// Backups for everything the helper may change: Sunshine config, RPCS3 input configs and
    /// the helper's own settings. Game savedata / trophy are never touched by this program.
    /// </summary>
    public static class BackupManager
    {
        public static string BackupRoot => Path.Combine(AppEnv.Root, "Backups", "RemoteCoop");

        public static string NewStamp() => DateTime.Now.ToString("yyyyMMdd_HHmmss");

        private static string StampDir(string stamp) => Path.Combine(BackupRoot, stamp);

        // ------------------------------------------------------------------ backups
        public static string BackupSunshine()
        {
            try
            {
                var files = new[] { SunshineManager.ConfFile, SunshineManager.AppsFile }.Where(File.Exists).ToArray();
                if (files.Length == 0) return null;
                string dst = Path.Combine(StampDir(NewStamp()), "Sunshine");
                Directory.CreateDirectory(dst);
                foreach (var f in files) File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
                AppEnv.Log("backup sunshine -> " + dst + " (" + files.Length + " files)");
                return dst;
            }
            catch (Exception ex) { AppEnv.Log("backup sunshine failed: " + ex.Message); return null; }
        }

        public static string BackupRpcs3Input()
        {
            try
            {
                string src = Path.Combine(Rpcs3Integration.Rpcs3Dir, "config", "input_configs");
                if (!Directory.Exists(src)) return null;
                string dst = Path.Combine(StampDir(NewStamp()), "RPCS3", "input_configs");
                CopyDir(src, dst);
                AppEnv.Log("backup rpcs3 input -> " + dst);
                return dst;
            }
            catch (Exception ex) { AppEnv.Log("backup rpcs3 input failed: " + ex.Message); return null; }
        }

        public static string BackupHelperSettings()
        {
            try
            {
                if (!File.Exists(AppEnv.SettingsFile)) return null;
                string dst = Path.Combine(StampDir(NewStamp()), "helper");
                Directory.CreateDirectory(dst);
                File.Copy(AppEnv.SettingsFile, Path.Combine(dst, "settings.json"), true);
                AppEnv.Log("backup helper settings -> " + dst);
                return dst;
            }
            catch (Exception ex) { AppEnv.Log("backup helper settings failed: " + ex.Message); return null; }
        }

        // ------------------------------------------------------------------ listing / restore
        public static List<string> ListBackups()
        {
            try
            {
                if (!Directory.Exists(BackupRoot)) return new List<string>();
                return Directory.GetDirectories(BackupRoot).OrderByDescending(d => d).ToList();
            }
            catch { return new List<string>(); }
        }

        private static string LatestWith(string subPath)
        {
            return ListBackups().FirstOrDefault(d => Directory.Exists(Path.Combine(d, subPath)));
        }

        public static bool RestoreSunshine(string stamp, out string error)
        {
            error = null;
            try
            {
                string dir = stamp == null
                    ? LatestWith("Sunshine")
                    : Path.Combine(StampDir(stamp), "Sunshine");
                if (dir == null || !Directory.Exists(dir)) { error = "no Sunshine backup found"; return false; }
                string cfgDir = SunshineManager.ConfigDir;
                if (cfgDir == null || !Directory.Exists(cfgDir)) { error = "Sunshine config dir not found"; return false; }
                BackupSunshine();  // pre-restore backup
                foreach (var f in Directory.GetFiles(dir))
                    File.Copy(f, Path.Combine(cfgDir, Path.GetFileName(f)), true);
                AppEnv.Log("restored sunshine config from " + dir);
                return true;
            }
            catch (Exception ex) { error = ex.Message; AppEnv.Log("restore sunshine failed: " + ex.Message); return false; }
        }

        public static bool RestoreRpcs3Input(string stamp, out string error)
        {
            error = null;
            try
            {
                string dir = stamp == null
                    ? LatestWith(Path.Combine("RPCS3", "input_configs"))
                    : Path.Combine(StampDir(stamp), "RPCS3", "input_configs");
                if (dir == null || !Directory.Exists(dir)) { error = "no RPCS3 input backup found"; return false; }
                string dst = Path.Combine(Rpcs3Integration.Rpcs3Dir, "config", "input_configs");
                if (!Directory.Exists(Path.GetDirectoryName(dst))) { error = "RPCS3 config dir not found"; return false; }
                if (Rpcs3Integration.Rpcs3Running()) { error = "RPCS3 is running - close it first"; return false; }
                BackupRpcs3Input();  // pre-restore backup
                CopyDir(dir, dst);
                AppEnv.Log("restored rpcs3 input from " + dir);
                return true;
            }
            catch (Exception ex) { error = ex.Message; AppEnv.Log("restore rpcs3 input failed: " + ex.Message); return false; }
        }

        // ------------------------------------------------------------------ helpers
        private static void CopyDir(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src)) File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(src)) CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
        }
    }
}
