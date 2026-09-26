<#
Launch RPCS3 for Dragon's Crown with an explicit display mode.

Modes:
  Borderless4K : RPCS3 runs "Vulkan Windowed"; Tools\borderless_4k.ps1 switches only the
                 *game render window* to a borderless full-monitor window (Win32 styles,
                 no Alt+Enter). Original style is restored when the game window goes away.
  Fullscreen   : RPCS3's own fullscreen (fallback).
  Windowed     : plain windowed mode (no external window control).

The selected profile is copied to Profiles\_runtime\<profile>__<mode>.yml and only
"Start games in fullscreen mode" is derived from the mode. Source profiles, ReShade,
RPCN, input config and Resolution Scale are left untouched.
#>
param(
    [Parameter(Mandatory = $true)][string]$Profile,
    [ValidateSet('Borderless4K', 'Fullscreen', 'Windowed')][string]$Mode = 'Borderless4K',
    [switch]$NoWatcher,
    [switch]$WaitForExit
)

$ErrorActionPreference = 'Continue'

$RPCS3_DIR   = 'E:\PS3\RPCS3'
$RPCS3_EXE   = Join-Path $RPCS3_DIR 'rpcs3.exe'
$GAME        = "C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
$PROFILE_DIR = 'E:\PS3\Profiles'
$RUNTIME_DIR = Join-Path $PROFILE_DIR '_runtime'
$BORDERLESS  = 'E:\PS3\Tools\borderless_4k.ps1'
$LAUNCH_LOG  = 'E:\PS3\Logs\launcher.log'

function Write-LauncherLog([string]$msg) {
    $line = "{0} {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $msg
    try { Add-Content -LiteralPath $LAUNCH_LOG -Value $line -Encoding utf8 } catch { }
    Write-Host $line
}

if (-not (Test-Path -LiteralPath $RPCS3_EXE)) { Write-LauncherLog "[ERROR] rpcs3.exe 없음: $RPCS3_EXE"; exit 1 }
if (-not (Test-Path -LiteralPath $GAME))      { Write-LauncherLog "[ERROR] 게임 덤프 없음: $GAME"; exit 1 }

$src = Join-Path (Join-Path $PROFILE_DIR $Profile) 'config.yml'
if (-not (Test-Path -LiteralPath $src)) { Write-LauncherLog "[ERROR] 프로필 없음: $src"; exit 1 }

New-Item -ItemType Directory -Path $RUNTIME_DIR -Force | Out-Null
$cfg = Join-Path $RUNTIME_DIR ("{0}__{1}.yml" -f $Profile, $Mode)
$text = [System.IO.File]::ReadAllText($src, [System.Text.Encoding]::UTF8)
$wantFullscreen = if ($Mode -eq 'Fullscreen') { 'true' } else { 'false' }
$repl = '$1' + $wantFullscreen
$text = [regex]::Replace($text, '(?m)^(\s*Start games in fullscreen mode:\s*).*$', $repl)
[System.IO.File]::WriteAllText($cfg, $text, [System.Text.UTF8Encoding]::new($false))

$check = [regex]::Match([System.IO.File]::ReadAllText($cfg), '(?m)^\s*Start games in fullscreen mode:\s*(\S+)').Groups[1].Value
Write-LauncherLog ("실행: mode={0} profile={1} fullscreen={2}" -f $Mode, $Profile, $check)
Write-LauncherLog ("  runtime config: {0}" -f $cfg)

$p = Start-Process -FilePath $RPCS3_EXE -WorkingDirectory $RPCS3_DIR -ArgumentList @('--config', "`"$cfg`"", "`"$GAME`"") -PassThru
Write-LauncherLog ("  RPCS3 PID: {0}" -f $p.Id)

if ($Mode -eq 'Borderless4K' -and -not $NoWatcher) {
    $wp = Start-Process -FilePath 'powershell' -WindowStyle Hidden -PassThru -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$BORDERLESS`"",
        '-Action', 'Watch', '-WaitTimeoutSec', '300')
    Write-LauncherLog ("  Borderless 4K watcher PID: {0}" -f $wp.Id)
}

if ($WaitForExit) { $p.WaitForExit() }
exit 0
