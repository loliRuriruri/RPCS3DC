param(
    [int]$Minutes = 30,
    [string]$Label = "RUN7_4K_POSTFX_30min"
)
$rpcs3 = "E:\PS3\RPCS3\rpcs3.exe"
$rdir  = "E:\PS3\RPCS3"
$game  = "C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
$log   = "$rdir\log\RPCS3.log"
$out   = "E:\PS3\Logs\$Label"

$p = Start-Process -FilePath $rpcs3 -WorkingDirectory $rdir -ArgumentList '--no-gui', '--config', '"E:\PS3\Profiles\DC_4K_TEST_WINDOWED\config.yml"', "`"$game`"" -PassThru
"started PID $($p.Id)  duration $Minutes min"
$rows = @()
$end = (Get-Date).AddMinutes($Minutes)
$i = 0
while ((Get-Date) -lt $end) {
    Start-Sleep -Seconds 30
    $i++
    if ($p.HasExited) { "!! PROCESS EXITED at sample $i (exit=$($p.ExitCode))"; break }
    $proc = Get-Process -Id $p.Id -ErrorAction SilentlyContinue
    $gpu = (& "C:\Windows\System32\nvidia-smi.exe" --query-gpu=utilization.gpu,memory.used,power.draw,temperature.gpu --format=csv,noheader,nounits 2>$null) -join ' '
    if ($proc) {
        $rows += [pscustomobject]@{ minute=[Math]::Round($i*0.5,1); ws_mb=[Math]::Round($proc.WorkingSet64/1MB,0); cpu_s=[Math]::Round($proc.TotalProcessorTime.TotalSeconds,0); gpu=$gpu }
    }
    if ($i % 10 -eq 0) { "t=$([int]($i*0.5))min ws=$([Math]::Round($proc.WorkingSet64/1MB,0))MB cpu=$([Math]::Round($proc.TotalProcessorTime.TotalSeconds,0))s gpu=[$gpu]" }
}
$crashed = $p.HasExited
if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue; Start-Sleep -Seconds 4 }
$rows | Export-Csv "$out`_perf.csv" -NoTypeInformation -Encoding UTF8
Copy-Item $log "$out`_RPCS3.log" -Force -ErrorAction SilentlyContinue
$lines = Get-Content "$out`_RPCS3.log" -ErrorAction SilentlyContinue
"crashed_or_exited_early: $crashed"
"log lines: $($lines.Count)"
"fatal/segv: " + (($lines | Select-String -Pattern 'Fatal|SIGSEGV|Access violation|terminate called' | Measure-Object).Count)
"E-level lines: " + (($lines | Select-String -Pattern ' E ' | Measure-Object).Count)
"ws first/last: " + ($rows | Select-Object -First 1).ws_mb + " / " + ($rows | Select-Object -Last 1).ws_mb + " MB"
"gpu max: " + (($rows | ForEach-Object { ($_.gpu -split ',')[0].Trim() } | Where-Object { $_ -match '^\d+$' } | ForEach-Object { [int]$_ } | Measure-Object -Maximum).Maximum) + " %"
