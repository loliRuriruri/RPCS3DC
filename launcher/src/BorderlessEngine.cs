using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace DragonCrownProEnhanced
{
    /// <summary>
    /// Borderless 4K engine (Win32 window style control of the *game render window* only).
    /// The RPCS3 GUI window is never touched; the game window is identified by the RPCS3
    /// "Window Title Format" (contains "FPS:" and "| &lt;renderer&gt; |").
    /// No keyboard input is injected (Alt+Enter is not used) and the original window
    /// style/rectangle is restored when the game window goes away.
    /// </summary>
    public sealed class BorderlessEngine
    {
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const long WS_CAPTION = 0x00C00000;
        private const long WS_THICKFRAME = 0x00040000;
        private const long WS_MINIMIZEBOX = 0x00020000;
        private const long WS_MAXIMIZEBOX = 0x00010000;
        private const long WS_SYSMENU = 0x00080000;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MONITORINFOEX
        {
            public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice;
        }

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        public delegate bool MonitorEnumProc(IntPtr hMon, IntPtr hdc, ref RECT rect, IntPtr data);

        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
        [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool GetMonitorInfo(IntPtr mon, ref MONITORINFOEX info);
        [DllImport("user32.dll")] public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc cb, IntPtr data);
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr ctx);

        private readonly Project _project;
        private readonly int _launchPid;
        private Thread _thread;
        private volatile bool _stop;

        public BorderlessEngine(Project project, int launchPid)
        {
            _project = project;
            _launchPid = launchPid;
        }

        public void Start()
        {
            _thread = new Thread(Loop) { IsBackground = true, Name = "borderless-4k" };
            _thread.Start();
        }

        public void Stop() => _stop = true;

        private class WinInfo
        {
            public IntPtr Hwnd; public uint Pid; public string Title; public string Cls;
            public bool Visible; public int Left, Top, Right, Bottom; public long Style, ExStyle;
        }

        private static List<WinInfo> Windows(uint[] pids)
        {
            var list = new List<WinInfo>();
            EnumWindows((h, l) =>
            {
                GetWindowThreadProcessId(h, out uint pid);
                if (!pids.Contains(pid)) return true;
                var t = new StringBuilder(512); GetWindowText(h, t, 512);
                var c = new StringBuilder(256); GetClassName(h, c, 256);
                GetWindowRect(h, out RECT r);
                list.Add(new WinInfo
                {
                    Hwnd = h, Pid = pid, Title = t.ToString(), Cls = c.ToString(), Visible = IsWindowVisible(h),
                    Left = r.Left, Top = r.Top, Right = r.Right, Bottom = r.Bottom,
                    Style = GetWindowLongPtr(h, GWL_STYLE).ToInt64(),
                    ExStyle = GetWindowLongPtr(h, GWL_EXSTYLE).ToInt64()
                });
                return true;
            }, IntPtr.Zero);
            return list;
        }

        private static uint[] Rpcs3Pids()
        {
            try
            {
                return Process.GetProcesses()
                    .Where(p => { try { return p.ProcessName.Equals("rpcs3", StringComparison.OrdinalIgnoreCase); } catch { return false; } })
                    .Select(p => (uint)p.Id).ToArray();
            }
            catch { return Array.Empty<uint>(); }
        }

        /// <summary>
        /// Locates the game render window. The window of the process we launched
        /// (launchPid) always wins; a fallback search across all rpcs3 processes is
        /// only used when that process is gone (e.g. relaunch / manual restore).
        /// </summary>
        private WinInfo FindGameWindow()
        {
            if (_launchPid > 0)
            {
                bool alive;
                try { using var pr = Process.GetProcessById(_launchPid); alive = !pr.HasExited; }
                catch { alive = false; }

                if (alive)
                {
                    // only the launched process: never touch another RPCS3 instance's window
                    var own = FindGameWindowIn(new[] { (uint)_launchPid });
                    if (own != null) return own;
                    return null; // launched process exists but has no game window yet -> keep waiting
                }
            }
            return FindGameWindowIn(Rpcs3Pids());
        }

        private static WinInfo FindGameWindowIn(uint[] pids)
        {
            if (pids.Length == 0) return null;
            var wins = Windows(pids);
            return wins.Where(w => w.Visible && w.Title.Contains("FPS:") && w.Title.Contains("| Vulkan |"))
                       .OrderByDescending(w => (long)(w.Right - w.Left) * (w.Bottom - w.Top))
                       .FirstOrDefault()
                   ?? wins.Where(w => w.Visible && w.Title.Contains("[BCAS20298]")).FirstOrDefault();
        }

        private static List<WinInfo> Titlebars(uint pid)
        {
            return Windows(new[] { pid }).Where(w => w.Cls == "_q_titlebar").ToList();
        }

        private static List<MONITORINFOEX> Monitors()
        {
            var list = new List<MONITORINFOEX>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMon, IntPtr hdc, ref RECT r, IntPtr d) =>
            {
                var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
                if (GetMonitorInfo(hMon, ref mi)) list.Add(mi);
                return true;
            }, IntPtr.Zero);
            return list;
        }

        private static MONITORINFOEX TargetMonitor(IntPtr hwnd)
        {
            var mons = Monitors();
            if (mons.Count == 0) return default;
            if (hwnd != IntPtr.Zero)
            {
                var h = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                foreach (var m in mons)
                {
                    var pt = new POINT { X = m.rcMonitor.Left + 1, Y = m.rcMonitor.Top + 1 };
                    if (MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST) == h) return m;
                }
            }
            foreach (var m in mons) if ((m.dwFlags & 1) != 0) return m;
            return mons[0];
        }

        [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);

        private string StatePath => Path.Combine(_project.LogDir, "borderless_state.json");
        private string StopFlagPath => Path.Combine(_project.LogDir, "borderless_stop.flag");

        private void Loop()
        {
            try
            {
                SetProcessDpiAwarenessContext(new IntPtr(-4)); // PER_MONITOR_AWARE_V2
                _project.Log("borderless: watcher start");

                WinInfo win = null;
                var deadline = DateTime.UtcNow.AddSeconds(300);
                while (!_stop && DateTime.UtcNow < deadline && win == null)
                {
                    if (File.Exists(StopFlagPath)) { _project.Log("borderless: stop requested before apply"); return; }
                    if (Rpcs3Pids().Length == 0) { Thread.Sleep(500); continue; }
                    win = FindGameWindow();
                    if (win == null) Thread.Sleep(700);
                }
                if (win == null) { _project.Log("borderless: game window not found (timeout)"); return; }

                Apply(win);
                IntPtr tracked = win.Hwnd;
                var lastCheck = DateTime.UtcNow;

                while (!_stop)
                {
                    Thread.Sleep(700);
                    if (File.Exists(StopFlagPath))
                    {
                        _project.Log("borderless: stop requested -> restoring and exiting watcher");
                        break;
                    }
                    if (Rpcs3Pids().Length == 0) break;
                    var cur = FindGameWindow();
                    if (cur == null)
                    {
                        Restore();
                        _project.Log("borderless: game window gone -> restored");
                        // a new game window may appear while the RPCS3 GUI is still running
                        var retryUntil = DateTime.UtcNow.AddSeconds(5);
                        WinInfo again = null;
                        while (!_stop && DateTime.UtcNow < retryUntil && again == null)
                        {
                            Thread.Sleep(500);
                            again = FindGameWindow();
                        }
                        if (again == null) break;
                        Apply(again);
                        tracked = again.Hwnd;
                        continue;
                    }
                    if ((DateTime.UtcNow - lastCheck).TotalSeconds >= 2)
                    {
                        lastCheck = DateTime.UtcNow;
                        if (Drifted(tracked))
                        {
                            _project.Log("borderless: drift detected -> re-applying");
                            Apply(cur);
                        }
                    }
                }
                Restore();
                _project.Log("borderless: watcher end");
            }
            catch (Exception ex)
            {
                _project.Log("borderless: error " + ex.Message);
            }
        }

        private bool Drifted(IntPtr hwnd)
        {
            var pids = Rpcs3Pids();
            if (pids.Length == 0) return false;
            var w = Windows(pids).FirstOrDefault(x => x.Hwnd == hwnd);
            if (w == null) return false;
            if ((w.Style & (WS_CAPTION | WS_THICKFRAME)) != 0) return true;
            var mon = TargetMonitor(hwnd);
            if (mon.szDevice == null) return false;
            return w.Left != mon.rcMonitor.Left || w.Top != mon.rcMonitor.Top ||
                   (w.Right - w.Left) != (mon.rcMonitor.Right - mon.rcMonitor.Left) ||
                   (w.Bottom - w.Top) != (mon.rcMonitor.Bottom - mon.rcMonitor.Top);
        }

        private void Apply(WinInfo win)
        {
            var mon = TargetMonitor(win.Hwnd);
            var titlebars = Titlebars(win.Pid);
            _project.Log($"borderless: target pid={win.Pid} hwnd=0x{win.Hwnd.ToInt64():X} title=\"{win.Title}\"");

            // capture the original state once per window (never overwrite it on re-apply)
            bool keep = false;
            try
            {
                if (File.Exists(StatePath))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(StatePath));
                    keep = doc.RootElement.GetProperty("hwnd").GetInt64() == win.Hwnd.ToInt64();
                }
            }
            catch { keep = false; }

            if (!keep)
            {
                var state = new Dictionary<string, object>
                {
                    ["saved_at"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["hwnd"] = win.Hwnd.ToInt64(),
                    ["pid"] = win.Pid,
                    ["title"] = win.Title,
                    ["style"] = win.Style,
                    ["exstyle"] = win.ExStyle,
                    ["rect"] = new Dictionary<string, int> { ["left"] = win.Left, ["top"] = win.Top, ["right"] = win.Right, ["bottom"] = win.Bottom },
                    ["monitor"] = new Dictionary<string, object>
                    {
                        ["device"] = mon.szDevice, ["left"] = mon.rcMonitor.Left, ["top"] = mon.rcMonitor.Top,
                        ["right"] = mon.rcMonitor.Right, ["bottom"] = mon.rcMonitor.Bottom
                    },
                    ["titlebars"] = titlebars.Select(t => new Dictionary<string, object> { ["hwnd"] = t.Hwnd.ToInt64(), ["visible"] = t.Visible }).ToList()
                };
                Directory.CreateDirectory(_project.LogDir);
                File.WriteAllText(StatePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
                _project.Log($"borderless: original state captured (hwnd=0x{win.Hwnd.ToInt64():X})");
            }
            else
            {
                _project.Log($"borderless: re-applying (original state preserved, hwnd=0x{win.Hwnd.ToInt64():X})");
            }

            long newStyle = win.Style & ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
            SetWindowLongPtr(win.Hwnd, GWL_STYLE, new IntPtr(newStyle));
            SetWindowPos(win.Hwnd, IntPtr.Zero, mon.rcMonitor.Left, mon.rcMonitor.Top,
                mon.rcMonitor.Right - mon.rcMonitor.Left, mon.rcMonitor.Bottom - mon.rcMonitor.Top,
                SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOZORDER);
            foreach (var tb in titlebars) if (tb.Visible) ShowWindow(tb.Hwnd, 0);

            Thread.Sleep(300);
            var after = Windows(Rpcs3Pids()).FirstOrDefault(x => x.Hwnd == win.Hwnd);
            if (after != null)
                _project.Log($"borderless: applied rect=({after.Left},{after.Top})-({after.Right},{after.Bottom}) " +
                             $"{after.Right - after.Left}x{after.Bottom - after.Top} monitor={mon.szDevice} " +
                             $"caption={(after.Style & WS_CAPTION) != 0} thickframe={(after.Style & WS_THICKFRAME) != 0}");
        }

        /// <summary>Restores the saved window style immediately (used when the launcher closes).</summary>
        public void RestoreNow()
        {
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch { }
            Restore();
        }

        /// <summary>
        /// Manual restore (--restore-window / TOOLS button): asks the live watcher to yield
        /// (flag file) and restores the saved style immediately. Without the flag the watcher
        /// would re-apply borderless within ~2 seconds.
        /// </summary>
        public void RequestStopAndRestore()
        {
            try
            {
                Directory.CreateDirectory(_project.LogDir);
                File.WriteAllText(StopFlagPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch { }
            RestoreNow();
        }

        private void Restore()
        {
            try
            {
                if (!File.Exists(StatePath)) return;
                using var doc = JsonDocument.Parse(File.ReadAllText(StatePath));
                var root = doc.RootElement;
                IntPtr hwnd = new IntPtr(root.GetProperty("hwnd").GetInt64());
                if (!IsWindow(hwnd)) { File.Delete(StatePath); return; }
                long style = root.GetProperty("style").GetInt64();
                long exstyle = root.GetProperty("exstyle").GetInt64();
                var rect = root.GetProperty("rect");
                SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(style));
                SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(exstyle));
                SetWindowPos(hwnd, IntPtr.Zero, rect.GetProperty("left").GetInt32(), rect.GetProperty("top").GetInt32(),
                    rect.GetProperty("right").GetInt32() - rect.GetProperty("left").GetInt32(),
                    rect.GetProperty("bottom").GetInt32() - rect.GetProperty("top").GetInt32(),
                    SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOZORDER);
                foreach (var tb in root.GetProperty("titlebars").EnumerateArray())
                {
                    IntPtr h = new IntPtr(tb.GetProperty("hwnd").GetInt64());
                    if (IsWindow(h) && tb.GetProperty("visible").GetBoolean()) ShowWindow(h, 5);
                }
                _project.Log($"borderless: restored hwnd=0x{hwnd.ToInt64():X} style=0x{style:X}");
                File.Delete(StatePath);
            }
            catch (Exception ex) { _project.Log("borderless: restore error " + ex.Message); }
        }
    }
}
