param(
    [int]$SettleSec = 22,
    [int]$SampleSec = 12
)
Add-Type @"
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public class Win {
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    public static List<string> TitlesForPid(uint target) {
        var res = new List<string>();
        EnumWindows((h, l) => {
            uint pid; GetWindowThreadProcessId(h, out pid);
            if (pid == target && IsWindowVisible(h)) {
                var sb = new StringBuilder(512);
                GetWindowText(h, sb, 512);
                if (sb.Length > 0) res.Add(sb.ToString());
            }
            return true;
        }, IntPtr.Zero);
        return res;
    }
}
"@

$rpcs3 = "E:\PS3\RPCS3\rpcs3.exe"
$rdir  = "E:\PS3\RPCS3"
$game  = "C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
$results = @()

$cases = @(
    @{N='CLEAN_100'; P='E:\PS3\Profiles\DC_TEST_WINDOWED\config.yml'},
    @{N='4K_300';    P='E:\PS3\Profiles\DC_4K_TEST_WINDOWED\config.yml'},
    @{N='5K_400';    P='E:\PS3\Profiles\DC_5K_SSAA\config.yml'},
    @{N='6K_500';    P='E:\PS3\Profiles\DC_6K_TEST\config.yml'},
    @{N='8K_600';    P='E:\PS3\Profiles\DC_8K_SCREENSHOT\config.yml'}
)

foreach ($c in $cases) {
    $p = Start-Process -FilePath $rpcs3 -WorkingDirectory $rdir -ArgumentList '--no-gui', '--config', "`"$($c.P)`"", "`"$game`"" -PassThru
    Start-Sleep -Seconds $SettleSec
    $titles = @()
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $SampleSec) {
        $t = [Win]::TitlesForPid([uint32]$p.Id)
        if ($t.Count -gt 0) { $titles += $t[0] }
        Start-Sleep -Milliseconds 1500
    }
    $fps = @()
    foreach ($t in $titles) {
        if ($t -match 'FPS:\s*([\d\.]+)') { $fps += [double]$Matches[1] }
    }
    $stat = if ($fps.Count -gt 0) {
        "n=$($fps.Count) avg={0:N1} min={1:N1} max={2:N1}" -f (($fps | Measure-Object -Average).Average), (($fps | Measure-Object -Minimum).Minimum), (($fps | Measure-Object -Maximum).Maximum)
    } else { "no FPS found in window title; titles=" + ($titles | Select-Object -Unique | Select-Object -First 3 | Join-String -Separator ' / ') }
    "CASE $($c.N): $stat"
    $results += [pscustomobject]@{ case=$c.N; fps=$stat; raw_titles=($titles | Select-Object -Unique | Select-Object -First 3) -join ' | ' }
    if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 4
}
$results | Export-Csv "E:\PS3\Logs\fps_by_scale.csv" -NoTypeInformation -Encoding UTF8
$results | Format-List
