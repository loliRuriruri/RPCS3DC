param(
    [string]$Label = "manual",
    [switch]$Quiet
)
# Dragon's Crown save backup (timestamped, verified, never overwrites ORIGINAL)
$ErrorActionPreference = "Continue"
$src  = "E:\PS3\RPCS3\dev_hdd0\home\00000001\savedata\BCAS20298-AUTO_0-"
$root = "E:\PS3\Saves\Dragons_Crown"
$auto = Join-Path $root "AUTO_BACKUP"
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$dst  = Join-Path $auto "DC_SAVE_${stamp}_$Label"

if (-not (Test-Path $src)) { Write-Output "[ERROR] save folder not found: $src"; exit 1 }
New-Item -ItemType Directory -Path $auto -Force | Out-Null

# 1) copy
Copy-Item $src $dst -Recurse -Force

# 2) verify every file byte-for-byte
$srcFiles = Get-ChildItem $src -Recurse -File
$ok = $true
$manifest = @()
foreach ($f in $srcFiles) {
    $rel = $f.FullName.Substring($src.Length).TrimStart('\')
    $target = Join-Path $dst $rel
    if (-not (Test-Path $target)) { $ok = $false; $manifest += "MISSING $rel"; continue }
    $h1 = (Get-FileHash $f.FullName -Algorithm SHA256).Hash
    $h2 = (Get-FileHash $target   -Algorithm SHA256).Hash
    if ($h1 -ne $h2) { $ok = $false; $manifest += "MISMATCH $rel" }
    else { $manifest += "$rel  $h1" }
}

# 3) manifest + result
$meta = @(
    "backup: DC_SAVE_${stamp}_$Label"
    "created: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    "source: $src"
    "verified: $ok"
    "files:"
) + ($manifest | ForEach-Object { "  $_" })
[System.IO.File]::WriteAllLines((Join-Path $dst "BACKUP_MANIFEST.txt"), $meta)

if (-not $Quiet) {
    if ($ok) { Write-Output "[OK] save backup verified -> $dst" }
    else     { Write-Output "[FAIL] backup verification failed -> $dst" }
}

# 4) retention: keep the newest 20 AUTO_BACKUP entries (ORIGINAL is never touched)
$all = Get-ChildItem $auto -Directory | Sort-Object Name -Descending
if ($all.Count -gt 20) {
    $all | Select-Object -Skip 20 | ForEach-Object {
        Remove-Item $_.FullName -Recurse -Force
        if (-not $Quiet) { Write-Output "[retention] removed old backup $($_.Name)" }
    }
}

if ($ok) { exit 0 } else { exit 2 }
