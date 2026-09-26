<#
Dragon's Crown GUI Launcher (RPCS3)

Display modes:
  * Borderless 4K (default) - RPCS3 stays "Vulkan Windowed"; the *game render window* is
    switched to a borderless full-monitor window by Tools\borderless_4k.ps1 (Win32 style
    control, no Alt+Enter injection, original style restored on exit).
  * Fullscreen  - RPCS3's own fullscreen (kept as fallback).
  * Windowed    - plain windowed mode.

Nothing in this launcher modifies ReShade (Vulkan layer), RPCN, input configuration or the
Resolution Scale of the selected profile: only "Start games in fullscreen mode" is derived
from the display mode, in a generated runtime config (the source profiles stay untouched).
#>
param(
    [ValidateSet('Borderless4K', 'Fullscreen', 'Windowed')][string]$DefaultMode = 'Borderless4K',
    [string]$DefaultProfile = 'DC_4K_ULTRA'
)

$ErrorActionPreference = 'Continue'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$RPCS3_DIR  = 'E:\PS3\RPCS3'
$RPCS3_EXE  = Join-Path $RPCS3_DIR 'rpcs3.exe'
$GAME       = "C:\Users\<user>\Downloads\Dragon's Crown (Asia) (Zh,Ko)\Dragon's Crown (Asia) (Zh,Ko)\PS3_GAME\USRDIR\EBOOT.BIN"
$PROFILE_DIR = 'E:\PS3\Profiles'
$RUNTIME_DIR = Join-Path $PROFILE_DIR '_runtime'
$BORDERLESS  = 'E:\PS3\Tools\borderless_4k.ps1'
$LAUNCH_LOG  = 'E:\PS3\Logs\launcher.log'

$PROFILES = @(
    @{ name = 'DC_4K_ULTRA';        label = 'DC_4K_ULTRA - 4K 300% (일상)' },
    @{ name = 'DC_5K_SSAA';         label = 'DC_5K_SSAA - 5K 400% SSAA' },
    @{ name = 'DC_4K_ULTRA_AF16';   label = 'DC_4K_ULTRA_AF16 - AF 16x A/B' },
    @{ name = 'DC_SAFE';            label = 'DC_SAFE - 100% 호환성 우선' },
    @{ name = 'CLEAN';              label = 'CLEAN - 100% 기준선' },
    @{ name = 'DC_8K_SCREENSHOT';   label = 'DC_8K_SCREENSHOT - 600% (실험)' },
    @{ name = 'DC_CHEAT_OFFLINE';   label = 'DC_CHEAT_OFFLINE - 치트(오프라인)' },
    @{ name = 'DC_NETPLAY_SAFE';    label = 'DC_NETPLAY_SAFE - RPCN 넷플레이' }
)

function Write-LauncherLog([string]$msg) {
    $line = "{0} {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $msg
    try { Add-Content -LiteralPath $LAUNCH_LOG -Value $line -Encoding utf8 } catch { }
    if ($script:txtLog) {
        try {
            $script:txtLog.AppendText($line + "`r`n")
            $script:txtLog.SelectionStart = $script:txtLog.Text.Length
            $script:txtLog.ScrollToCaret()
            [System.Windows.Forms.Application]::DoEvents()
        } catch { }
    }
}

function Get-MonitorJson {
    try {
        $json = & powershell -NoProfile -ExecutionPolicy Bypass -File $BORDERLESS -Action Info 2>$null
        return ($json | Out-String | ConvertFrom-Json)
    } catch { return $null }
}

function New-RuntimeConfig([string]$profileName, [string]$mode) {
    # The source profile is copied and only "Start games in fullscreen mode" is derived
    # from the display mode. Nothing else is touched (ReShade/RPCN/input/scale stay as-is).
    $src = Join-Path (Join-Path $PROFILE_DIR $profileName) 'config.yml'
    if (-not (Test-Path -LiteralPath $src)) { throw "profile not found: $src" }
    New-Item -ItemType Directory -Path $RUNTIME_DIR -Force | Out-Null
    $dst = Join-Path $RUNTIME_DIR ("{0}__{1}.yml" -f $profileName, $mode)
    $text = [System.IO.File]::ReadAllText($src, [System.Text.Encoding]::UTF8)
    $wantFullscreen = if ($mode -eq 'Fullscreen') { 'true' } else { 'false' }
    $repl = '$1' + $wantFullscreen
    $text = [regex]::Replace($text, '(?m)^(\s*Start games in fullscreen mode:\s*).*$', $repl)
    [System.IO.File]::WriteAllText($dst, $text, [System.Text.UTF8Encoding]::new($false))
    return $dst
}

function Stop-Rpcs3 {
    $procs = @(Get-Process -Name rpcs3 -ErrorAction SilentlyContinue)
    if ($procs.Count -eq 0) { Write-LauncherLog '종료: 실행 중인 rpcs3 없음'; return }
    foreach ($p in $procs) { try { [void]$p.CloseMainWindow() } catch { } }
    Start-Sleep -Seconds 2
    Get-Process -Name rpcs3 -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-LauncherLog ("종료: rpcs3 {0}개 프로세스 정리" -f $procs.Count)
}

# ---------------------------------------------------------------- build the form
$form = New-Object System.Windows.Forms.Form
$form.Text = "Dragon's Crown Launcher  -  RPCS3 / BCAS20298 v1.09"
$form.Size = New-Object System.Drawing.Size(720, 660)
$form.StartPosition = 'CenterScreen'
$form.Font = New-Object System.Drawing.Font('Malgun Gothic', 9)
$form.BackColor = [System.Drawing.Color]::FromArgb(32, 34, 38)
$form.ForeColor = [System.Drawing.Color]::Gainsboro

function New-Group([string]$title, [int]$x, [int]$y, [int]$w, [int]$h) {
    $g = New-Object System.Windows.Forms.GroupBox
    $g.Text = $title; $g.Location = New-Object System.Drawing.Point($x, $y)
    $g.Size = New-Object System.Drawing.Size($w, $h)
    $g.ForeColor = [System.Drawing.Color]::FromArgb(150, 200, 255)
    return $g
}

# --- display mode group
$grpMode = New-Group '디스플레이 모드' 12 10 680 96
$rbBorderless = New-Object System.Windows.Forms.RadioButton
$rbBorderless.Text = 'Borderless 4K  (기본) - RPCS3는 Vulkan Windowed 유지, 게임 창만 테두리 없이 모니터 전체(3840x2160)'
$rbBorderless.Location = New-Object System.Drawing.Point(14, 22); $rbBorderless.Size = New-Object System.Drawing.Size(650, 20)
$rbFullscreen = New-Object System.Windows.Forms.RadioButton
$rbFullscreen.Text = 'Fullscreen  (RPCS3 자체 전체화면 - fallback)'
$rbFullscreen.Location = New-Object System.Drawing.Point(14, 46); $rbFullscreen.Size = New-Object System.Drawing.Size(650, 20)
$rbWindowed = New-Object System.Windows.Forms.RadioButton
$rbWindowed.Text = 'Windowed  (창모드)'
$rbWindowed.Location = New-Object System.Drawing.Point(14, 70); $rbWindowed.Size = New-Object System.Drawing.Size(650, 20)
switch ($DefaultMode) {
    'Fullscreen' { $rbFullscreen.Checked = $true }
    'Windowed'   { $rbWindowed.Checked = $true }
    default      { $rbBorderless.Checked = $true }
}
$grpMode.Controls.AddRange(@($rbBorderless, $rbFullscreen, $rbWindowed))

# --- profile group
$grpProfile = New-Group '프로필 (해상도/옵션)' 12 112 680 62
$cmbProfile = New-Object System.Windows.Forms.ComboBox
$cmbProfile.DropDownStyle = 'DropDownList'
$cmbProfile.Location = New-Object System.Drawing.Point(14, 24); $cmbProfile.Size = New-Object System.Drawing.Size(650, 24)
foreach ($p in $PROFILES) { [void]$cmbProfile.Items.Add($p.label) }
$idx = 0
for ($i = 0; $i -lt $PROFILES.Count; $i++) { if ($PROFILES[$i].name -eq $DefaultProfile) { $idx = $i } }
$cmbProfile.SelectedIndex = $idx
$grpProfile.Controls.Add($cmbProfile)

# --- info group
$grpInfo = New-Group '환경 (자동 탐지)' 12 180 680 118
$lblInfo = New-Object System.Windows.Forms.Label
$lblInfo.Location = New-Object System.Drawing.Point(14, 22); $lblInfo.Size = New-Object System.Drawing.Size(650, 88)
$lblInfo.ForeColor = [System.Drawing.Color]::FromArgb(200, 200, 200)
$grpInfo.Controls.Add($lblInfo)

# --- buttons
$btnLaunch = New-Object System.Windows.Forms.Button
$btnLaunch.Text = '실행'; $btnLaunch.Location = New-Object System.Drawing.Point(12, 306)
$btnLaunch.Size = New-Object System.Drawing.Size(120, 34)
$btnLaunch.BackColor = [System.Drawing.Color]::FromArgb(46, 120, 60); $btnLaunch.ForeColor = [System.Drawing.Color]::White
$btnStop = New-Object System.Windows.Forms.Button
$btnStop.Text = '게임 종료'; $btnStop.Location = New-Object System.Drawing.Point(140, 306)
$btnStop.Size = New-Object System.Drawing.Size(120, 34)
$btnRestore = New-Object System.Windows.Forms.Button
$btnRestore.Text = '창 스타일 복원'; $btnRestore.Location = New-Object System.Drawing.Point(268, 306)
$btnRestore.Size = New-Object System.Drawing.Size(130, 34)
$btnLogs = New-Object System.Windows.Forms.Button
$btnLogs.Text = '로그 열기'; $btnLogs.Location = New-Object System.Drawing.Point(406, 306)
$btnLogs.Size = New-Object System.Drawing.Size(100, 34)
$btnClose = New-Object System.Windows.Forms.Button
$btnClose.Text = '닫기'; $btnClose.Location = New-Object System.Drawing.Point(592, 306)
$btnClose.Size = New-Object System.Drawing.Size(100, 34)

# --- log box
$txtLog = New-Object System.Windows.Forms.TextBox
$txtLog.Multiline = $true; $txtLog.ScrollBars = 'Vertical'; $txtLog.ReadOnly = $true
$txtLog.Location = New-Object System.Drawing.Point(12, 348); $txtLog.Size = New-Object System.Drawing.Size(680, 262)
$txtLog.BackColor = [System.Drawing.Color]::FromArgb(20, 22, 26); $txtLog.ForeColor = [System.Drawing.Color]::FromArgb(180, 230, 180)
$txtLog.Font = New-Object System.Drawing.Font('Consolas', 8.5)
$script:txtLog = $txtLog

$form.Controls.AddRange(@($grpMode, $grpProfile, $grpInfo, $btnLaunch, $btnStop, $btnRestore, $btnLogs, $btnClose, $txtLog))

# ---------------------------------------------------------------- info refresh
$monJson = Get-MonitorJson
$reShadeDll = 'C:\ProgramData\ReShade\ReShade64.dll'
$rsVer = if (Test-Path $reShadeDll) { (Get-Item $reShadeDll).VersionInfo.FileVersion } else { 'n/a' }
$monText = '모니터: (탐지 실패)'
if ($monJson -and $monJson.monitors) {
    $lines = @()
    foreach ($m in $monJson.monitors) {
        $lines += ("  [{0}] {1}  {2}x{3} (모니터 전체)  작업영역 {4}x{5}{6}" -f `
            $m.index, $m.device, $m.width, $m.height, $m.work_width, $m.work_height, $(if ($m.primary) { ' [기본]' } else { '' }))
    }
    $monText = ($lines -join "`r`n")
}
$lblInfo.Text = @"
$monText
  대상 모니터(기본): 현재 RPCS3 게임 창이 있는 모니터 (자동)
  게임: $(if (Test-Path -LiteralPath $GAME) { 'BCAS20298 v1.09  OK' } else { '없음 (경로 확인 필요)' })
  ReShade: $rsVer   |   Borderless: E:\PS3\Tools\borderless_4k.ps1
"@
Write-LauncherLog "Launcher 시작 (기본 모드=$DefaultMode, 기본 프로필=$DefaultProfile)"

# ---------------------------------------------------------------- events
$btnLaunch.Add_Click({
    try {
        $mode = if ($rbBorderless.Checked) { 'Borderless4K' } elseif ($rbFullscreen.Checked) { 'Fullscreen' } else { 'Windowed' }
        $profileName = $PROFILES[$cmbProfile.SelectedIndex].name
        if (-not (Test-Path -LiteralPath $RPCS3_EXE)) { throw "rpcs3.exe 없음: $RPCS3_EXE" }
        if (-not (Test-Path -LiteralPath $GAME)) { throw "게임 덤프 없음: $GAME" }

        Write-LauncherLog ("실행 요청: mode={0} profile={1}" -f $mode, $profileName)
        $out = & powershell -NoProfile -ExecutionPolicy Bypass -File $LAUNCH_HELPER -Profile $profileName -Mode $mode 2>&1
        foreach ($line in @($out)) { Write-LauncherLog ("  " + $line) }
        if ($mode -eq 'Borderless4K') { Write-LauncherLog '  (게임 창이 뜨면 자동으로 테두리 없이 4K 전체화면 적용, 종료 시 원복)' }
        elseif ($mode -eq 'Fullscreen') { Write-LauncherLog '  (RPCS3 자체 전체화면 - fallback)' }
        else { Write-LauncherLog '  (창모드 - 외부 창 제어 없음)' }
    } catch {
        Write-LauncherLog ("실행 오류: {0}" -f $_.Exception.Message)
    }
})

$btnStop.Add_Click({ Stop-Rpcs3 })

$btnRestore.Add_Click({
    try {
        $out = & powershell -NoProfile -ExecutionPolicy Bypass -File $BORDERLESS -Action Restore 2>&1
        Write-LauncherLog ("창 스타일 복원: {0}" -f ($out -join ' / '))
    } catch { Write-LauncherLog ("복원 오류: {0}" -f $_.Exception.Message) }
})

$btnLogs.Add_Click({
    try { Start-Process explorer.exe 'E:\PS3\Logs' } catch { }
})

$btnClose.Add_Click({ $form.Close() })

$form.Add_FormClosing({
    # Do not kill the game on window close; just leave a note. (Watcher keeps running.)
    Write-LauncherLog 'Launcher 종료'
})

[void]$form.ShowDialog()
