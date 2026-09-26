<#
Borderless 4K engine for the RPCS3 game render window (Dragon's Crown launcher).

Design rules (per project spec):
  * RPCS3 itself keeps rendering "Vulkan Windowed" - only the game render window is
    restyled/resized externally with Win32 window style calls.
  * The RPCS3 main GUI window is never touched; the game render window is identified by
    its title (RPCS3 "Window Title Format" -> contains "FPS:" and "| <renderer> |").
  * No keyboard input is injected (Alt+Enter is NOT used).
  * Monitor resolution and work area are detected automatically (DPI aware),
    the default target monitor is the one the game window currently sits on.
  * The original window style and rectangle are stored and restored on exit.
  * Nothing here touches ReShade (Vulkan layer), RPCN, input handling or Resolution Scale.

Actions:
  Info     - print monitor information as JSON (DPI aware)
  Apply    - find the game window and switch it to borderless full-monitor
  Restore  - restore the previously saved style/rectangle
  Watch    - wait for the game window, apply, then restore when it goes away
#>
param(
    [ValidateSet('Info', 'Apply', 'Restore', 'Watch')][string]$Action = 'Watch',
    [int]$MonitorIndex = -1,
    [int]$WaitTimeoutSec = 180,
    [string]$StatePath = 'E:\PS3\Logs\borderless_state.json',
    [string]$LogPath = 'E:\PS3\Logs\borderless.log'
)

$ErrorActionPreference = 'Continue'

Add-Type @"
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class BL {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW")] public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);
    [DllImport("user32.dll", EntryPoint="SetWindowLongPtrW")] public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(POINT pt, uint flags);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern bool GetMonitorInfo(IntPtr mon, ref MONITORINFOEX info);
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr ctx);
    [DllImport("user32.dll")] public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc cb, IntPtr data);

    public delegate bool MonitorEnumProc(IntPtr hMon, IntPtr hdc, ref RECT rect, IntPtr data);

    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct MONITORINFOEX {
        public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string szDevice;
    }

    public static bool MakeDpiAware() {
        // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4
        try { return SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch { return false; }
    }

    public class WinInfo {
        public IntPtr hwnd; public uint pid; public string title; public string cls;
        public bool visible; public int left, top, right, bottom; public long style, exstyle;
    }

    public static List<WinInfo> Windows(uint[] pids) {
        var res = new List<WinInfo>();
        EnumWindows((h, l) => {
            uint pid; GetWindowThreadProcessId(h, out pid);
            bool match = false;
            foreach (var p in pids) if (p == pid) { match = true; break; }
            if (!match) return true;
            var t = new StringBuilder(512); GetWindowText(h, t, 512);
            var c = new StringBuilder(256); GetClassName(h, c, 256);
            RECT r; GetWindowRect(h, out r);
            res.Add(new WinInfo { hwnd = h, pid = pid, title = t.ToString(), cls = c.ToString(),
                visible = IsWindowVisible(h), left = r.Left, top = r.Top, right = r.Right, bottom = r.Bottom,
                style = GetWindowLongPtr(h, -16).ToInt64(), exstyle = GetWindowLongPtr(h, -20).ToInt64() });
            return true;
        }, IntPtr.Zero);
        return res;
    }

    public class MonInfo {
        public int index; public string device; public int left, top, right, bottom;
        public int workLeft, workTop, workRight, workBottom; public bool primary;
    }

    private static List<MonInfo> _monList;
    private static int _monIndex;

    private static bool MonitorEnumCallback(IntPtr hMon, IntPtr hdc, ref RECT r, IntPtr data) {
        var mi = new MONITORINFOEX();
        mi.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
        if (GetMonitorInfo(hMon, ref mi)) {
            _monList.Add(new MonInfo {
                index = _monIndex++, device = mi.szDevice,
                left = mi.rcMonitor.Left, top = mi.rcMonitor.Top, right = mi.rcMonitor.Right, bottom = mi.rcMonitor.Bottom,
                workLeft = mi.rcWork.Left, workTop = mi.rcWork.Top, workRight = mi.rcWork.Right, workBottom = mi.rcWork.Bottom,
                primary = (mi.dwFlags & 1) != 0
            });
        }
        return true;
    }

    public static List<MonInfo> Monitors() {
        _monList = new List<MonInfo>();
        _monIndex = 0;
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumCallback, IntPtr.Zero);
        return _monList;
    }
}
"@

# ---------------------------------------------------------------- helpers
function Write-Log([string]$msg) {
    $line = "{0} {1}" -f (Get-Date -Format 'HH:mm:ss.fff'), $msg
    try { Add-Content -LiteralPath $LogPath -Value $line -Encoding utf8 } catch { }
    Write-Host $line
}

$WS_CAPTION     = 0x00C00000
$WS_THICKFRAME  = 0x00040000
$WS_MINIMIZEBOX = 0x00020000
$WS_MAXIMIZEBOX = 0x00010000
$WS_SYSMENU     = 0x00080000
$SWP_FRAMECHANGED = 0x0020
$SWP_SHOWWINDOW   = 0x0040
$SWP_NOZORDER     = 0x0004

function Get-Rpcs3Pids {
    @(Get-Process -Name rpcs3 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
}

function Find-GameWindow {
    $pids = Get-Rpcs3Pids
    if ($pids.Count -eq 0) { return $null }
    $wins = [BL]::Windows([uint32[]]$pids)
    # The game render window carries the RPCS3 "Window Title Format" (default: "FPS: %F | %R | ...")
    $cand = $wins | Where-Object {
        $_.visible -and $_.title -match 'FPS:' -and $_.title -match '\|\s*Vulkan\s*\|'
    } | Sort-Object { ($_.right - $_.left) * ($_.bottom - $_.top) } -Descending
    if ($cand) { return $cand[0] }
    # fallback: any visible window whose title contains the configured title id
    $cand = $wins | Where-Object { $_.visible -and $_.title -match '\[BCAS20298\]' }
    if ($cand) { return $cand[0] }
    return $null
}

function Find-TitlebarWindows {
    $pids = Get-Rpcs3Pids
    if ($pids.Count -eq 0) { return @() }
    [BL]::Windows([uint32[]]$pids) | Where-Object { $_.cls -eq '_q_titlebar' }
}

function Get-TargetMonitor([IntPtr]$hwnd) {
    $mons = [BL]::Monitors()
    if ($MonitorIndex -ge 0 -and $MonitorIndex -lt $mons.Count) { return $mons[$MonitorIndex] }
    if ($hwnd -ne [IntPtr]::Zero) {
        $h = [BL]::MonitorFromWindow($hwnd, 2)  # MONITOR_DEFAULTTONEAREST
        foreach ($m in $mons) {
            $pt = [BL+POINT]::new()
            $pt.X = $m.left + 1; $pt.Y = $m.top + 1
            if ([BL]::MonitorFromPoint($pt, 2) -eq $h) { return $m }
        }
    }
    return ($mons | Where-Object { $_.primary } | Select-Object -First 1)
}

function Save-State($w, $monitor, $titlebars) {
    $state = [ordered]@{
        saved_at   = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
        hwnd       = [int64]$w.hwnd
        pid        = $w.pid
        title      = $w.title
        style      = $w.style
        exstyle    = $w.exstyle
        rect       = @{ left = $w.left; top = $w.top; right = $w.right; bottom = $w.bottom }
        monitor    = @{ device = $monitor.device; left = $monitor.left; top = $monitor.top; right = $monitor.right; bottom = $monitor.bottom }
        titlebars  = @($titlebars | ForEach-Object { @{ hwnd = [int64]$_.hwnd; visible = $_.visible } })
    }
    $state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $StatePath -Encoding utf8
}

function Apply-Borderless {
    $w = Find-GameWindow
    if (-not $w) { return $null }
    $mon = Get-TargetMonitor $w.hwnd
    $titlebars = @(Find-TitlebarWindows)

    # Only capture the original state once per window. Re-applying must never
    # overwrite the original style/rect, otherwise Restore would keep the borderless style.
    $keepExisting = $false
    if (Test-Path -LiteralPath $StatePath) {
        try {
            $prev = Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
            if ([int64]$prev.hwnd -eq [int64]$w.hwnd) { $keepExisting = $true }
        } catch { $keepExisting = $false }
    }
    if ($keepExisting) {
        Write-Log "APPLY: re-applying to 0x$('{0:X}' -f [int64]$w.hwnd) (original state preserved)"
    } else {
        Save-State $w $mon $titlebars
        Write-Log "APPLY: original state captured for 0x$('{0:X}' -f [int64]$w.hwnd)"
    }

    $newStyle = $w.style -band (-bnot ($WS_CAPTION -bor $WS_THICKFRAME -bor $WS_MINIMIZEBOX -bor $WS_MAXIMIZEBOX -bor $WS_SYSMENU))
    [void][BL]::SetWindowLongPtr($w.hwnd, -16, [IntPtr]$newStyle)
    $ok = [BL]::SetWindowPos($w.hwnd, [IntPtr]::Zero, $mon.left, $mon.top,
                             ($mon.right - $mon.left), ($mon.bottom - $mon.top),
                             ($SWP_FRAMECHANGED -bor $SWP_SHOWWINDOW -bor $SWP_NOZORDER))

    foreach ($tb in $titlebars) { if ($tb.visible) { [void][BL]::ShowWindow($tb.hwnd, 0) } }

    Start-Sleep -Milliseconds 300
    $after = [BL]::Windows([uint32[]](Get-Rpcs3Pids)) | Where-Object { $_.hwnd -eq $w.hwnd } | Select-Object -First 1
    Write-Log ("APPLIED hwnd=0x{0:X} style 0x{1:X8} -> 0x{2:X8} target rect ({3},{4})-({5},{6}) monitor={7}" -f `
        [int64]$w.hwnd, $w.style, $newStyle, $mon.left, $mon.top, $mon.right, $mon.bottom, $mon.device)
    if ($after) {
        Write-Log ("VERIFY  rect=({0},{1})-({2},{3}) {4}x{5} caption={6} thickframe={7}" -f `
            $after.left, $after.top, $after.right, $after.bottom, ($after.right - $after.left), ($after.bottom - $after.top),
            [bool]($after.style -band $WS_CAPTION), [bool]($after.style -band $WS_THICKFRAME))
    }
    return $w
}

function Test-BorderlessDrift([IntPtr]$hwnd, $mon) {
    # returns $true when the window no longer matches the borderless target (needs re-apply)
    $wins = [BL]::Windows([uint32[]](Get-Rpcs3Pids))
    $w = $wins | Where-Object { $_.hwnd -eq $hwnd } | Select-Object -First 1
    if (-not $w) { return $false }
    if ($w.style -band ($WS_CAPTION -bor $WS_THICKFRAME)) { return $true }
    if ($w.left -ne $mon.left -or $w.top -ne $mon.top -or
        ($w.right - $w.left) -ne ($mon.right - $mon.left) -or ($w.bottom - $w.top) -ne ($mon.bottom - $mon.top)) { return $true }
    return $false
}

function Restore-Window {
    if (-not (Test-Path -LiteralPath $StatePath)) { Write-Log "RESTORE: no state file"; return $false }
    $st = Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
    $hwnd = [IntPtr][int64]$st.hwnd
    if (-not [BL]::IsWindow($hwnd)) {
        Write-Log "RESTORE: window 0x$('{0:X}' -f [int64]$st.hwnd) no longer exists (nothing to restore)"
        Remove-Item -LiteralPath $StatePath -Force -ErrorAction SilentlyContinue
        return $false
    }
    [void][BL]::SetWindowLongPtr($hwnd, -16, [IntPtr][int64]$st.style)
    [void][BL]::SetWindowLongPtr($hwnd, -20, [IntPtr][int64]$st.exstyle)
    [void][BL]::SetWindowPos($hwnd, [IntPtr]::Zero, [int]$st.rect.left, [int]$st.rect.top,
                             ([int]$st.rect.right - [int]$st.rect.left), ([int]$st.rect.bottom - [int]$st.rect.top),
                             ($SWP_FRAMECHANGED -bor $SWP_SHOWWINDOW -bor $SWP_NOZORDER))
    foreach ($tb in $st.titlebars) {
        $h = [IntPtr][int64]$tb.hwnd
        if ([BL]::IsWindow($h) -and $tb.visible) { [void][BL]::ShowWindow($h, 5) }
    }
    Write-Log ("RESTORED hwnd=0x{0:X} style=0x{1:X8} rect=({2},{3})-({4},{5})" -f `
        [int64]$st.hwnd, [int64]$st.style, [int]$st.rect.left, [int]$st.rect.top, [int]$st.rect.right, [int]$st.rect.bottom)
    Remove-Item -LiteralPath $StatePath -Force -ErrorAction SilentlyContinue
    return $true
}

# ---------------------------------------------------------------- main
switch ($Action) {
    'Info' {
        [void][BL]::MakeDpiAware()
        $mons = [BL]::Monitors()
        $out = @()
        foreach ($m in $mons) {
            $out += [ordered]@{
                index = $m.index; device = $m.device; primary = $m.primary
                width = $m.right - $m.left; height = $m.bottom - $m.top
                monitor_rect = "$($m.left),$($m.top),$($m.right),$($m.bottom)"
                work_width = $m.workRight - $m.workLeft; work_height = $m.workBottom - $m.workTop
                work_rect = "$($m.workLeft),$($m.workTop),$($m.workRight),$($m.workBottom)"
            }
        }
        $w = Find-GameWindow
        $target = if ($w) { (Get-TargetMonitor $w.hwnd) } else { $null }
        [ordered]@{
            dpi_aware = $true
            monitors = $out
            game_window = if ($w) { "0x$('{0:X}' -f [int64]$w.hwnd) $($w.title)" } else { $null }
            target_monitor = if ($target) { "$($target.index):$($target.device) $($target.right - $target.left)x$($target.bottom - $target.top)" } else { $null }
        } | ConvertTo-Json -Depth 5
    }
    'Apply' {
        [void][BL]::MakeDpiAware()
        $w = Apply-Borderless
        if (-not $w) { Write-Log "APPLY: game window not found (RPCS3 running?)"; exit 1 }
        exit 0
    }
    'Restore' {
        [void][BL]::MakeDpiAware()
        if (Restore-Window) { exit 0 } else { exit 1 }
    }
    'Watch' {
        [void][BL]::MakeDpiAware()
        Write-Log "WATCH start (waiting for the game render window, timeout ${WaitTimeoutSec}s)"
        $deadline = (Get-Date).AddSeconds($WaitTimeoutSec)
        $applied = $false
        while ((Get-Date) -lt $deadline -and -not $applied) {
            if ((Get-Rpcs3Pids).Count -eq 0) { Start-Sleep -Milliseconds 500; continue }
            if (Find-GameWindow) { $applied = [bool](Apply-Borderless) }
            if (-not $applied) { Start-Sleep -Milliseconds 700 }
        }
        if (-not $applied) { Write-Log "WATCH: game window not found within timeout"; exit 1 }
        Write-Log "WATCH: borderless active - monitoring until the game window disappears"
        $w = Find-GameWindow
        $mon = if ($w) { Get-TargetMonitor $w.hwnd } else { $null }
        $hwnd = if ($w) { $w.hwnd } else { [IntPtr]::Zero }
        $lastCheck = Get-Date
        while ($true) {
            Start-Sleep -Milliseconds 700
            if ((Get-Rpcs3Pids).Count -eq 0) { Write-Log "WATCH: rpcs3 exited"; break }
            $w = Find-GameWindow
            if (-not $w) {
                Write-Log "WATCH: game window gone -> restoring"
                [void](Restore-Window)
                # a new game window may appear (RPCS3 GUI still running) - keep watching briefly
                $reapplied = $false
                $t2 = (Get-Date).AddSeconds(5)
                while ((Get-Date) -lt $t2) {
                    Start-Sleep -Milliseconds 500
                    if (Find-GameWindow) {
                        $reapplied = [bool](Apply-Borderless)
                        if ($reapplied) { $w = Find-GameWindow; $mon = Get-TargetMonitor $w.hwnd; $hwnd = $w.hwnd }
                        break
                    }
                }
                if (-not $reapplied) { break }
                continue
            }
            # re-assert the borderless geometry if something changed it (cheap, no flicker when unchanged)
            if (((Get-Date) - $lastCheck).TotalSeconds -ge 2) {
                $lastCheck = Get-Date
                if ($hwnd -ne [IntPtr]::Zero -and (Test-BorderlessDrift $hwnd $mon)) {
                    Write-Log "WATCH: drift detected - re-applying borderless geometry"
                    [void](Apply-Borderless)
                    $w = Find-GameWindow
                    if ($w) { $hwnd = $w.hwnd; $mon = Get-TargetMonitor $w.hwnd }
                }
            }
        }
        [void](Restore-Window)
        Write-Log "WATCH end"
        exit 0
    }
}
