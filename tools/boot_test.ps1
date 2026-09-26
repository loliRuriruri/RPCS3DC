param(
    [Parameter(Mandatory=$true)][string]$Label,
    [Parameter(Mandatory=$true)][string]$Profile,
    [int]$Seconds = 75,
    [switch]$ClearLlmCache
)
$ErrorActionPreference = "Continue"
$rpcs3   = "E:\PS3\RPCS3\rpcs3.exe"
$rdir    = "E:\PS3\RPCS3"
$game    = "C:\Users\<user>\Downloads\Dragon's Crown (Asia,Zh,Ko)\x" # placeholder (not used)
$game    = "C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
$log     = "$rdir\log\RPCS3.log"
$tty     = "$rdir\log\TTY.log"
$outdir  = "E:\PS3\Logs"
$cacheD  = "$rdir\cache"

function Snapshot($tag) {
    $ppu = (Get-ChildItem "$cacheD" -Directory -Force -ErrorAction SilentlyContinue | Where-Object { $_.Name -like 'ppu-*' } | Measure-Object).Count
    $shaders = (Get-ChildItem "$cacheD\BCAS20298" -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object).Count
    $shaderBytes = (Get-ChildItem "$cacheD\BCAS20298" -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
    $hddCache = (Get-ChildItem "$rdir\dev_hdd0\cache" -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object).Count
    "$tag ppu_module_caches=$ppu shader_files=$shaders shader_bytes=$shaderBytes hdd0_cache_files=$hddCache"
}

if ($ClearLlmCache) {
    $bk = "E:\PS3\Backups\02_before_coldboot_test"
    New-Item -ItemType Directory -Path $bk -Force | Out-Null
    $moved = 0
    Get-ChildItem "$cacheD" -Directory -Force | Where-Object { $_.Name -like 'ppu-*' -or $_.Name -eq 'vsh' } | ForEach-Object {
        $dst = Join-Path $bk $_.Name
        if (Test-Path $dst) { Remove-Item $dst -Recurse -Force }
        Move-Item $_.FullName $dst -Force
        $moved++
    }
    "LLVM/PPU+SPU module cache moved to backup: $moved dirs -> $bk"
}

$logOffset = if (Test-Path $log) { (Get-Item $log).Length } else { 0 }
"=== $Label ==="
Snapshot "BEFORE"
"log offset: $logOffset"
"profile: $Profile"
"launch: rpcs3.exe --no-gui --config `"$Profile`" `"$game`""

$p = Start-Process -FilePath $rpcs3 -WorkingDirectory $rdir -ArgumentList '--no-gui', '--config', "`"$Profile`"", "`"$game`"" -PassThru
"PID: $($p.Id)"

$samples = @()
$cpu0 = $null
$sw = [Diagnostics.Stopwatch]::StartNew()
for ($i = 0; $i -lt $Seconds; $i += 5) {
    Start-Sleep -Seconds 5
    if ($p.HasExited) { "process exited early after $($sw.Elapsed.TotalSeconds)s (exit code $($p.ExitCode))"; break }
    $proc = Get-Process -Id $p.Id -ErrorAction SilentlyContinue
    $gpu = (& "C:\Windows\System32\nvidia-smi.exe" --query-gpu=utilization.gpu,memory.used,power.draw,temperature.gpu --format=csv,noheader,nounits 2>$null) -join ' '
    if ($proc) {
        if (-not $cpu0) { $cpu0 = $proc.TotalProcessorTime.TotalSeconds }
        $cpuPct = [Math]::Round((($proc.TotalProcessorTime.TotalSeconds - $cpu0) / 5.0) / [Environment]::ProcessorCount * 100, 1)
        $samples += [pscustomobject]@{ t=$sw.Elapsed.TotalSeconds; cpu_pct=$cpuPct; working_set_mb=[Math]::Round($proc.WorkingSet64/1MB,0); gpu=$gpu }
        "t=$([int]$sw.Elapsed.TotalSeconds)s cpu=$cpuPct% ws=$([Math]::Round($proc.WorkingSet64/1MB,0))MB gpu=[$gpu]"
    }
}
$csv = Join-Path $outdir "$Label`_perf.csv"
$samples | Export-Csv -Path $csv -NoTypeInformation -Encoding UTF8
"metrics -> $csv"
if ($samples.Count -gt 2) {
    $mid = $samples | Select-Object -Skip 2
    "AVG cpu={0}% ws={1}MB" -f ([Math]::Round(($mid | Measure-Object cpu_pct -Average).Average,1)), ([Math]::Round(($mid | Measure-Object working_set_mb -Average).Average,0))
    $gpuVals = $mid | ForEach-Object { ($_.gpu -split ',')[0].Trim() } | Where-Object { $_ -match '^\d+$' } | ForEach-Object { [int]$_ }
    $vramVals = $mid | ForEach-Object { ($_.gpu -split ',')[1].Trim() } | Where-Object { $_ -match '^\d+$' } | ForEach-Object { [int]$_ }
    if ($gpuVals) { "AVG GPU util={0}%  max={1}%" -f ([Math]::Round(($gpuVals | Measure-Object -Average).Average,1)), ($gpuVals | Measure-Object -Maximum).Maximum }
    if ($vramVals) { "AVG VRAM used={0} MiB  max={1} MiB" -f ([Math]::Round(($vramVals | Measure-Object -Average).Average,0)), ($vramVals | Measure-Object -Maximum).Maximum }
}

if (-not $p.HasExited) {
    "stopping RPCS3..."
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 4
}
"RPCS3 running after stop: $((Get-Process -Name rpcs3 -ErrorAction SilentlyContinue) -ne $null)"

Snapshot "AFTER"

# ---- collect the new part of the log ----
$destLog = Join-Path $outdir "$Label`_RPCS3.log"
if (Test-Path $log) {
    Copy-Item $log $destLog -Force
    "log copied -> $destLog  (size $((Get-Item $destLog).Length))"
}
$destTty = Join-Path $outdir "$Label`_TTY.log"
if (Test-Path $tty) { Copy-Item $tty $destTty -Force }

if (Test-Path $destLog) {
    $lines = Get-Content $destLog
    "=== log summary ==="
    "total lines: $($lines.Count)"
    $lines | Select-String -Pattern 'RPCS3 v0|Architecture:|Operating system|GPU|Vulkan|PPU: LLVM: Compiled|SPU: LLVM: Compiled|SELF|EBOOT|Title:' | Select-Object -First 25 | ForEach-Object { $_.Line }
    "=== warnings/errors (first 40) ==="
    $lines | Select-String -Pattern '\bE \b|\bF \b|error|Fatal|failed|Warning' | Select-Object -First 40 | ForEach-Object { $_.Line }
    "=== last 25 lines ==="
    $lines | Select-Object -Last 25
}
