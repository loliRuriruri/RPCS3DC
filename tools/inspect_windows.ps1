param(
    [string]$LogPath = "E:\PS3\Logs\window_inspect.log"
)
# Inspect every top-level window of any rpcs3.exe process: class, style, rect, parent.
Add-Type @"
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class WinInspect {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW")] public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr ctx);
    public static bool MakeDpiAware() { try { return SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch { return false; } }

    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }

    public class Info {
        public IntPtr hwnd; public uint pid; public string title; public string cls;
        public bool visible; public IntPtr parent; public int left, top, right, bottom;
        public long style; public long exstyle;
    }

    public static List<Info> All(uint[] pids) {
        var res = new List<Info>();
        EnumWindows((h, l) => {
            uint pid; GetWindowThreadProcessId(h, out pid);
            bool match = false;
            foreach (var p in pids) if (p == pid) { match = true; break; }
            if (!match) return true;
            var t = new StringBuilder(512); GetWindowText(h, t, 512);
            var c = new StringBuilder(256); GetClassName(h, c, 256);
            RECT r; GetWindowRect(h, out r);
            res.Add(new Info {
                hwnd = h, pid = pid, title = t.ToString(), cls = c.ToString(),
                visible = IsWindowVisible(h), parent = GetParent(h),
                left = r.Left, top = r.Top, right = r.Right, bottom = r.Bottom,
                style = GetWindowLongPtr(h, -16).ToInt64(), exstyle = GetWindowLongPtr(h, -20).ToInt64()
            });
            return true;
        }, IntPtr.Zero);
        return res;
    }
}
"@

[void][WinInspect]::MakeDpiAware()   # physical pixel coordinates
$pids = @(Get-Process -Name rpcs3 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
if ($pids.Count -eq 0) { "rpcs3 프로세스 없음"; exit 1 }
"rpcs3 PIDs: $($pids -join ', ')"
$rows = [WinInspect]::All([uint32[]]$pids)
"총 최상위 창: $($rows.Count)"
$out = @()
foreach ($w in $rows | Sort-Object { $_.title }) {
    $s = $w.style; $e = $w.exstyle
    $flags = @()
    if ($s -band 0x00C00000) { $flags += 'WS_CAPTION' }
    if ($s -band 0x00040000) { $flags += 'WS_THICKFRAME' }
    if ($s -band 0x10000000) { $flags += 'WS_VISIBLE' }
    if ($s -band 0x80000000) { $flags += 'WS_POPUP' }
    if ($s -band 0x00C00000) { } 
    if ($e -band 0x00080000) { $flags += 'WS_EX_LAYERED' }
    if ($e -band 0x00000008) { $flags += 'WS_EX_TOPMOST' }
    if ($e -band 0x00040000) { $flags += 'WS_EX_APPWINDOW' }
    $line = "pid={0,-6} hwnd=0x{1:X8} vis={2,-5} parent=0x{3:X8} rect=({4},{5})-({6},{7}) {8}x{9} class='{10}' style=0x{11:X8}[{12}] title='{13}'" -f `
        $w.pid, [int64]$w.hwnd, $w.visible, [int64]$w.parent, $w.left, $w.top, $w.right, $w.bottom,
        ($w.right - $w.left), ($w.bottom - $w.top), $w.cls, $s, ($flags -join ','), $w.title
    $out += $line
}
$out | ForEach-Object { $_ }
$out | Set-Content -Path $LogPath -Encoding utf8
"-> $LogPath"