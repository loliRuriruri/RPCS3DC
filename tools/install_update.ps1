param(
    [string]$Rpcs3 = "E:\PS3\RPCS3\rpcs3.exe",
    [string]$Pkg   = "E:\PS3\Updates_DLC\BCAS20298_v1.09\HP5017-BCAS20298_00-DRAGONSCROWNDL00-A0109-V0100-PE.pkg",
    [int]$TimeoutSec = 300
)
$log = "E:\PS3\RPCS3\log\RPCS3.log"
$logStart = if (Test-Path $log) { (Get-Item $log).Length } else { 0 }

Write-Output "Launching: $Rpcs3 --installpkg `"$Pkg`""
$p = Start-Process -FilePath $Rpcs3 -ArgumentList '--installpkg', "`"$Pkg`"" -PassThru
Write-Output "PID: $($p.Id)"

$deadline = (Get-Date).AddSeconds($TimeoutSec)
$done = $false
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 5
    if (Test-Path $log) {
        $txt = Get-Content $log -Raw -ErrorAction SilentlyContinue
        if ($txt -and ($txt -match 'Successfully installed|Installation completed|installed successfully|Failed to install|error.*install')) {
            $done = $true
            break
        }
    }
    if ($p.HasExited) { $done = $true; break }
}
Write-Output "Loop finished (done=$done, exited=$($p.HasExited))"

# Show install-related log lines added since we started
if (Test-Path $log) {
    $all = Get-Content $log -Raw
    $tail = if ($all.Length -gt $logStart) { $all.Substring([int]$logStart) } else { $all }
    Write-Output "=== NEW LOG LINES (install-related) ==="
    $tail -split "`r?`n" | Where-Object { $_ -match 'PKG|install|Install|pkg|BCAS20298|Dragon' } | Select-Object -First 60
}
Write-Output "=== installed game data ==="
Get-ChildItem "E:\PS3\RPCS3\dev_hdd0\game" -Force -ErrorAction SilentlyContinue | Select-Object Name, LastWriteTime | Format-Table -AutoSize
if (Test-Path "E:\PS3\RPCS3\dev_hdd0\game\BCAS20298") {
    Get-ChildItem "E:\PS3\RPCS3\dev_hdd0\game\BCAS20298" -Recurse -Force -ErrorAction SilentlyContinue | Select-Object FullName, Length | Format-Table -AutoSize | Out-String -Width 200
}
if (-not $p.HasExited) {
    Write-Output "Terminating RPCS3 (PID $($p.Id))..."
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
}
Write-Output "RPCS3 still running: $((Get-Process -Name rpcs3 -ErrorAction SilentlyContinue) -ne $null)"
