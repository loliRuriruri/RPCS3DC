using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DragonCrownProEnhanced
{
    public enum Rpcs3SettingsKind { MainWindow, Controller, Rpcn, General }

    /// <summary>
    /// RPCS3 UI bridge.
    /// - Never starts a second instance while the project RPCS3 is running.
    /// - Reuses the existing process (path-matched to &lt;ROOT&gt;\RPCS3\rpcs3.exe).
    /// - Activates the main window (restore / foreground / top) and invokes toolbar actions
    ///   through UI Automation (no coordinate clicking).
    /// - Thread-safe: rapid clicks can never launch two RPCS3 processes.
    /// </summary>
    public static class Rpcs3UiBridge
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);

        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }

        public sealed class Instance
        {
            public Process Process;
            public IntPtr Hwnd;
            public bool StartedByUs;
            public bool GameRunning;
        }

        public static string Rpcs3ExePath(Project p) => Path.Combine(p.Rpcs3Dir, "rpcs3.exe");

        /// <summary>True when the process executable is this project's rpcs3.exe.</summary>
        public static bool IsProjectRpcs3(Project p, Process proc)
        {
            try
            {
                string expected = Path.GetFullPath(Rpcs3ExePath(p));
                string actual = proc.MainModule?.FileName;
                if (!string.IsNullOrEmpty(actual))
                    return string.Equals(Path.GetFullPath(actual), expected, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                // access denied (e.g. different bitness / protected process): fall back and log
                p.Log("rpcs3 bridge: MainModule access failed (" + ex.GetType().Name + ") - using window-class fallback");
            }
            try
            {
                if (!proc.ProcessName.Equals("rpcs3", StringComparison.OrdinalIgnoreCase)) return false;
                IntPtr h = MainWindowOf(proc.Id);
                if (h == IntPtr.Zero) return false;
                var t = new StringBuilder(512);
                GetWindowText(h, t, 512);
                return t.ToString().StartsWith("RPCS3", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public static List<Process> ProjectProcesses(Project p)
        {
            var list = new List<Process>();
            foreach (var proc in Process.GetProcessesByName("rpcs3"))
            {
                try { if (IsProjectRpcs3(p, proc)) list.Add(proc); } catch { }
            }
            return list;
        }

        public static Process FindProjectProcess(Project p) =>
            ProjectProcesses(p)
                .OrderByDescending(proc => MainWindowOf(proc.Id) != IntPtr.Zero ? 1 : 0)
                .FirstOrDefault();

        /// <summary>Visible top-level main window for a pid (Process.MainWindowHandle + largest-window fallback).</summary>
        public static IntPtr MainWindowOf(int pid)
        {
            try
            {
                using var proc = Process.GetProcessById(pid);
                if (proc.MainWindowHandle != IntPtr.Zero) return proc.MainWindowHandle;
            }
            catch { }

            IntPtr best = IntPtr.Zero;
            long bestArea = -1;
            EnumWindows((h, l) =>
            {
                GetWindowThreadProcessId(h, out uint p);
                if (p != (uint)pid || !IsWindowVisible(h)) return true;
                var t = new StringBuilder(512);
                GetWindowText(h, t, 512);
                if (t.Length == 0) return true;
                GetWindowRect(h, out RECT r);
                long area = (long)(r.Right - r.Left) * (r.Bottom - r.Top);
                if (area > bestArea) { bestArea = area; best = h; }
                return true;
            }, IntPtr.Zero);
            return best;
        }

        /// <summary>True when the project RPCS3 has a running game window (FPS/Vulkan title).</summary>
        public static bool IsGameRunning(Project p)
        {
            var proc = FindProjectProcess(p);
            if (proc == null) return false;
            bool found = false;
            EnumWindows((h, l) =>
            {
                GetWindowThreadProcessId(h, out uint pid);
                if (pid != (uint)proc.Id) return true;
                if (!IsWindowVisible(h)) return true;
                var t = new StringBuilder(512);
                GetWindowText(h, t, 512);
                string title = t.ToString();
                if (title.Contains("FPS:") && title.Contains("| Vulkan |")) { found = true; return false; }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        public static void Activate(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            if (IsIconic(hwnd)) ShowWindow(hwnd, 9);   // SW_RESTORE
            ShowWindow(hwnd, 5);                        // SW_SHOW
            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);
        }

        /// <summary>
        /// Returns the project RPCS3 main window, starting RPCS3 exactly once when absent.
        /// Never starts a second instance while one is running.
        /// </summary>
        public static Instance EnsureMainWindow(Project p, int timeoutMs = 30000)
        {
            Gate.Wait();
            try
            {
                var proc = FindProjectProcess(p);
                if (proc != null)
                {
                    IntPtr h = MainWindowOf(proc.Id);
                    if (h == IntPtr.Zero)
                    {
                        // alive but still creating its window - wait briefly, never relaunch
                        var until = DateTime.UtcNow.AddMilliseconds(Math.Min(timeoutMs, 15000));
                        while (DateTime.UtcNow < until && h == IntPtr.Zero)
                        {
                            Thread.Sleep(400);
                            h = MainWindowOf(proc.Id);
                        }
                    }
                    Activate(h);
                    bool game = IsGameRunning(p);
                    p.Log($"rpcs3 bridge: reuse pid={proc.Id} hwnd=0x{h.ToInt64():X} gameRunning={game}");
                    return new Instance { Process = proc, Hwnd = h, StartedByUs = false, GameRunning = game };
                }

                string exe = Rpcs3ExePath(p);
                if (!File.Exists(exe)) throw new FileNotFoundException("rpcs3.exe not found: " + exe);
                var started = Process.Start(new ProcessStartInfo(exe)
                { WorkingDirectory = Path.GetDirectoryName(exe), UseShellExecute = true });
                p.Log("rpcs3 bridge: started single new instance pid=" + started.Id);

                IntPtr handle = IntPtr.Zero;
                var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                while (DateTime.UtcNow < deadline && handle == IntPtr.Zero)
                {
                    Thread.Sleep(500);
                    handle = MainWindowOf(started.Id);
                }
                Activate(handle);
                return new Instance { Process = started, Hwnd = handle, StartedByUs = true, GameRunning = false };
            }
            finally { Gate.Release(); }
        }

        /// <summary>Opens a settings dialog on the existing instance (or the one we just started).</summary>
        public static (bool ok, string message) OpenSettings(Project p, Rpcs3SettingsKind kind)
        {
            try
            {
                var inst = EnsureMainWindow(p);
                string running = inst.StartedByUs ? "RPCS3 started." : "RPCS3 is already running.";
                string gameNote = inst.GameRunning ? "\n\nNote: some settings apply after the game is restarted." : "";

                if (kind == Rpcs3SettingsKind.MainWindow)
                    return (true, running + " Main window brought to the front." + gameNote);

                var (ok, detail) = Rpcs3Automation.InvokeToolbar(p, inst.Process.Id, kind);
                if (ok)
                    return (true, $"{running}\n{detail}{gameNote}");

                string manual = kind == Rpcs3SettingsKind.Controller ? "Pads"
                              : kind == Rpcs3SettingsKind.Rpcn ? "RPCN" : "Config";
                if (inst.GameRunning)
                {
                    return (false,
                        $"{running}\n" +
                        "RPCS3 hides its toolbar while a game is running, so the settings dialog could not be opened automatically.\n" +
                        "The existing RPCS3 window has been brought to the front (no second instance was started).\n" +
                        $"Stop the game first, then use the toolbar button: {manual}\n" +
                        "Note: some settings apply after the game is restarted.");
                }
                return (false,
                    $"{running}\n" +
                    "Could not automatically open the requested settings.\n" +
                    "The existing RPCS3 window has been brought to the front (no second instance was started). Use the toolbar button:\n" +
                    $"  {manual}" + gameNote);
            }
            catch (Exception ex)
            {
                p.Log("rpcs3 bridge error: " + ex.Message);
                return (false, "RPCS3 bridge error: " + ex.Message);
            }
        }

        public static string StatusLine(Project p)
        {
            try
            {
                var procs = ProjectProcesses(p);
                if (procs.Count == 0) return "○ not running";
                string game = IsGameRunning(p) ? " · GAME RUNNING" : "";
                return $"● running (pid {procs[0].Id}){game}";
            }
            catch { return "확인 실패"; }
        }
    }
}
