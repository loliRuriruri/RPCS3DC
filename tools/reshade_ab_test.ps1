param(
    [int]$Seconds = 40,
    [string]$Label = "A_official"
)
# ReShade A/B test: boot Dragon's Crown once and capture what the loaded ReShade does.
$rpcs3  = "E:\PS3\RPCS3\rpcs3.exe"
$rdir   = "E:\PS3\RPCS3"
$game   = "C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
$rlog   = "$rdir\ReShade.log"
$outdir = "E:\PS3\Logs"

$dll = "C:\ProgramData\ReShade\ReShade64.dll"
"=== $Label ==="
"installed ReShade64.dll : $((Get-Item $dll).VersionInfo.FileVersion)  SHA256=$((Get-FileHash $dll -Algorithm SHA256).Hash.Substring(0,16))..."

$before = if (Test-Path $rlog) { (Get-Item $rlog).Length } else { 0 }

$p = Start-Process -FilePath $rpcs3 -WorkingDirectory $rdir -ArgumentList '--no-gui', '--config', '"E:\PS3\Profiles\DC_4K_TEST_WINDOWED\config.yml"', "`"$game`"" -PassThru
"PID: $($p.Id)  (running $Seconds s)"
Start-Sleep -Seconds $Seconds
$alive = -not $p.HasExited
"still alive after $Seconds s: $alive"
if ($alive) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue; Start-Sleep -Seconds 4 }

# capture new ReShade.log content
$dest = Join-Path $outdir "reshade_AB_$Label.log"
if (Test-Path $rlog) {
    $all = Get-Content $rlog -Raw
    $new = if ($all.Length -gt $before) { $all.Substring([int]$before) } else { $all }
    [System.IO.File]::WriteAllText($dest, $new, [System.Text.UTF8Encoding]::new($false))
    "new ReShade.log content -> $dest ($($new.Length) chars)"
    "--- 핵심 라인 ---"
    $new -split "`r?`n" | Where-Object { $_ -match 'Initializing crosire|ZERO-BANNER|Successfully compiled|Recreated runtime|Running on |Failed|ERROR|WARN' } |
        Select-Object -First 25 | ForEach-Object { "  $_" }
}

# RPCS3 side check
$plog = "$rdir\log\RPCS3.log"
$dest2 = Join-Path $outdir "AB_$Label`_RPCS3.log"
Copy-Item $plog $dest2 -Force -ErrorAction SilentlyContinue
if (Test-Path $dest2) {
    $lines = Get-Content $dest2
    "--- RPCS3 로그: fatal/에러 카운트 ---"
    "  total lines: $($lines.Count)"
    "  fatal/segv : " + (($lines | Select-String -Pattern 'Fatal|SIGSEGV|Access violation' | Measure-Object).Count)
    "  Title      : " + (($lines | Select-String -Pattern 'SYS: Title: ' | Select-Object -Last 1).Line)
}
