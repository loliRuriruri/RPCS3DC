using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DragonCrownProEnhanced
{
    public partial class MainWindow : Window
    {
        private Project _p;
        private Process _game;

        public MainWindow()
        {
            InitializeComponent();
            _p = Project.Discover();
            _p.Log("launcher start (root=" + _p.Root + ", preset=" + _p.GraphicsPreset + ")");
            try { _p.ApplyReShadeSilent(_p.ReShadeSilent); } catch { }
            RefreshStatus();
            TxtRoot.Text = "ROOT: " + _p.Root + "   ·   게임: " + (_p.GameExe ?? "(미탐지 — RPCS3 게임 목록에 등록 필요)");
            Append("Dragon's Crown PC Edition 준비 완료 (Phase 1)");
        }

        // ------------------------------------------------------------- helpers
        private void Append(string text)
        {
            TxtLog.AppendText(DateTime.Now.ToString("HH:mm:ss ") + text + Environment.NewLine);
            TxtLog.ScrollToEnd();
        }

        private void RefreshStatus()
        {
            string ck(bool ok) => ok ? "✓" : "✗";
            string dot(bool on) => on ? "●" : "○";
            string p1 = _p.ControllerHandler(1), p2 = _p.ControllerHandler(2);
            TxtStatus.Text = string.Join(Environment.NewLine, new[]
            {
                $"GAME      {ck(_p.GameExe != null && File.Exists(_p.GameExe))} Dragon's Crown [{_p.TitleId}]  v{_p.GameVersion()}",
                $"RPCS3     {ck(File.Exists(_p.Rpcs3Exe))} Current ({_p.Rpcs3Build()})   KnownGood {ck(File.Exists(_p.KnownGoodExe))}",
                $"FIRMWARE  {ck(_p.FirmwareVersion() != "미확인")} {_p.FirmwareVersion()}    PPU {_p.PpuHash()}",
                $"GRAPHICS  {ck(true)} {_p.GraphicsPreset} · 4K 300% · AF 16x · MSAA Auto · Stretch Off",
                $"DISPLAY   {ck(true)} {_p.DisplayMode} · VSync {( _p.VSync ? "Full" : "Off")} · VBlank 60 · Frame Skip OFF",
                $"RESHADE   {ck(_p.ReShadeVersion() != "없음")} {_p.ReShadeVersion()} · {( _p.ReShadeEnabled ? "ON (Pro Enhanced)" : "OFF (Standard)")} · Silent {(_p.ReShadeSilent ? "ON" : "OFF")}",
                $"PAD 1/2   {ck(p1 != "Null" && p1 != "없음")} {p1}  /  {ck(p2 != "Null" && p2 != "없음")} {p2}",
                $"NETWORK   {dot(_p.RpcnConfigured())} RPCN {(_p.RpcnConfigured() ? "Configured" : "미설정")} · Sunshine {_p.SunshineStatus()}",
                $"CHEATS    {dot(_p.CheatsOrPatchesEnabled())} {(_p.CheatsOrPatchesEnabled() ? "항목 있음" : "Disabled")}",
            });
            TxtStandardSub.Text = "4K 300% · AF 16x · ReShade 없음";
            TxtProSub.Text = "4K 300% + ReShade (선명도/색감)";
        }

        private void Preflight(string profile, bool needNetplay = false, bool needSecondPad = false)
        {
            if (!File.Exists(_p.Rpcs3Exe)) throw new Exception("rpcs3.exe를 찾을 수 없습니다: " + _p.Rpcs3Exe);
            if (_p.GameExe == null || !File.Exists(_p.GameExe)) throw new Exception("게임 덤프를 찾을 수 없습니다. RPCS3 게임 목록에 Dragon's Crown을 추가하세요.");
            if (!File.Exists(_p.ProfilePath(profile))) throw new Exception("프로필이 없습니다: " + profile + "\nTOOLS → Reset RPCS3 Settings 로 복원할 수 있습니다.");
            if (needNetplay && !_p.RpcnConfigured()) throw new Exception("RPCN 계정이 설정되지 않았습니다.\nRPCS3 → RPCN → Create Account / Log In 후 다시 시도하세요.");
            if (needNetplay && _p.CheatsOrPatchesEnabled()) throw new Exception("치트/패치 항목이 있습니다. NETPLAY 전에 모두 비활성화하세요.");
            if (needSecondPad)
            {
                string p2 = _p.ControllerHandler(2);
                if (p2 == "Null" || p2 == "없음")
                    Append("주의: 2P 패드가 설정되어 있지 않습니다 (SETTINGS → Controller).");
            }
        }

        private void Launch(string profile, string label, bool knownGood = false, bool borderless = true)
        {
            try
            {
                if (_game != null && !_game.HasExited) { Append("이미 게임이 실행 중입니다."); return; }
                _p.SaveSettings();
                var proc = _p.Launch(profile, knownGood, borderless);
                _game = proc;
                Append($"{label}: RPCS3 PID {proc.Id} · {profile} · {_p.DisplayMode} · {_p.GraphicsPreset}");
                if (borderless && _p.DisplayMode == "Borderless4K")
                    Append("게임 창이 뜨면 자동으로 테두리 없이 모니터 전체로 전환됩니다 (종료 시 원복).");
                proc.EnableRaisingEvents = true;
                proc.Exited += (s, e) => Dispatcher.Invoke(() =>
                {
                    Append($"{label}: 게임 종료 (exit {proc.ExitCode})");
                    _p.Log($"game exit code={proc.ExitCode}");
                    RefreshStatus();
                });
                RefreshStatus();
            }
            catch (Exception ex)
            {
                Append("오류: " + ex.Message);
                MessageBox.Show(ex.Message, label, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ------------------------------------------------------------- PLAY
        private void BtnStandard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Preflight(Project.Profile4K);
                _p.ApplyGraphicsPreset(Project.PresetStandard);
                Append("Graphics A (Standard): RPCS3 자체 화질 · AF 16x · ReShade OFF");
                Launch(Project.Profile4K, "PLAY · Standard");
            }
            catch (Exception ex) { Append("PLAY: " + ex.Message); MessageBox.Show(ex.Message, "PLAY · Standard", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void BtnProEnhanced_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Preflight(Project.Profile4K);
                _p.ApplyGraphicsPreset(Project.PresetProEnhanced);
                Append("Graphics B (Pro Enhanced): ReShade ON (Deband + CAS + Levels + Vibrance + SMAA, Silent)");
                Launch(Project.Profile4K, "PLAY · Pro Enhanced");
            }
            catch (Exception ex) { Append("PLAY: " + ex.Message); MessageBox.Show(ex.Message, "PLAY · Pro Enhanced", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        // ------------------------------------------------------------- MULTIPLAYER
        private void BtnLocal_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("MULTIPLAYER — Local (1P / 2P 같은 PC)", 420);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info(
                "한 PC에서 2인 플레이 (게임의 로컬 협동)\n\n" +
                "1) 2P 패드를 PC에 연결하고 RPCS3 → 게임패드 설정에서 Player 2 핸들러를 지정하세요.\n" +
                "2) 게임에서 캐릭터 선택 화면 또는 Tavern(주점)에서 2P 패드의 Start 를 누르면 합류합니다.\n" +
                "3) 로컬 협동은 트로피/세이브가 Player 1 기준으로 기록됩니다.\n\n" +
                "현재 1P: " + _p.ControllerHandler(1) + " / 2P: " + _p.ControllerHandler(2)));
            panel.Children.Add(PanelButton("RPCS3 게임패드 설정 열기", () =>
            {
                Process.Start(new ProcessStartInfo(_p.Rpcs3Exe) { WorkingDirectory = _p.Rpcs3Dir, UseShellExecute = true });
                Append("RPCS3 GUI → 게임패드 아이콘에서 1P/2P 를 설정하세요.");
            }));
            panel.Children.Add(PanelButton("Local 2P 로 실행 (4K)", () =>
            {
                Preflight(Project.Profile4K, false, true);
                Launch(Project.Profile4K, "Local 2P");
                w.Close();
            }));
            w.Content = panel;
            w.ShowDialog();
        }

        private void BtnRemote_Click(object sender, RoutedEventArgs e)
        {
            string sun = _p.SunshineStatus();
            string p2 = _p.ControllerHandler(2);
            var w = MakePanel("MULTIPLAYER — Remote Co-op (Sunshine + Moonlight)", 520);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info(
                "한 PC에서 로컬 2P를 실행하고, 친구가 원격으로 2P 패드를 조작합니다.\n" +
                "(RPCN/PSN 불필요 · 같은 세션 · 그래픽 MOD와 충돌 가능성 낮음)\n\n" +
                "· 게임 실행 PC = HOST, 친구 패드 = HOST의 2P 입력\n" +
                "· 친구는 자기 RPCS3 세이브/캐릭터를 쓰는 구조가 아닙니다(문서 참고).\n\n" +
                "진단:\n" +
                $"  Sunshine        : {sun}\n" +
                $"  Controller 2    : {p2}\n" +
                $"  RPCS3 / Game    : {(File.Exists(_p.Rpcs3Exe) ? "OK" : "없음")} / {(_p.GameExe != null ? "OK" : "없음")}\n\n" +
                "절차:\n" +
                "1) HOST: Sunshine 실행 → Moonlight 페어링(PIN)\n" +
                "2) HOST: 2P 패드(또는 가상 패드) 설정 후 이 런처로 게임 실행\n" +
                "3) 친구: Moonlight 접속 → 패드 입력 전달\n" +
                "4) 게임 내에서 2P Start 로 합류"));
            panel.Children.Add(PanelButton("Sunshine 다운로드 페이지 열기", () =>
                Process.Start(new ProcessStartInfo("https://github.com/LizardByte/Sunshine") { UseShellExecute = true })));
            panel.Children.Add(PanelButton("진단 다시 실행", () => { RefreshStatus(); MessageBox.Show("Sunshine: " + _p.SunshineStatus() + "\n2P: " + _p.ControllerHandler(2)); }));
            panel.Children.Add(PanelButton("Remote Co-op 으로 실행 (4K)", () =>
            {
                Preflight(Project.Profile4K, false, true);
                Launch(Project.Profile4K, "Remote Co-op");
                w.Close();
            }));
            w.Content = panel;
            w.ShowDialog();
        }

        private void BtnRpcn_Click(object sender, RoutedEventArgs e)
        {
            bool safe = false;
            var w = MakePanel("MULTIPLAYER — RPCN Online", 520);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info(
                "각자 자기 RPCS3 + 게임 + 캐릭터 + 세이브 + 화면을 가진 상태로 온라인 플레이.\n\n" +
                "READY 검사:\n" +
                $"  RPCN configured : {(_p.RpcnConfigured() ? "✓" : "✗  (RPCS3 → RPCN → Create Account)")}\n" +
                $"  Cheats/patches  : {(_p.CheatsOrPatchesEnabled() ? "✗ 항목 있음" : "✓ OFF")}\n" +
                $"  Game version    : {_p.GameVersion()}   PPU {_p.PpuHash()}\n" +
                $"  Sunshine(선택)  : {_p.SunshineStatus()}\n\n" +
                "두 PC 모두 같은 TITLE_ID(BCAS20298) · 같은 APP_VER(01.09) 여야 합니다.\n" +
                "첫 연결 테스트에서는 ReShade를 끄는 것을 권장합니다."));
            var cbSafe = new CheckBox { Content = "Netplay Safe 프로필 사용 (실험/네트워크 영향 옵션 전부 OFF)", Margin = new Thickness(4, 8, 4, 4), IsChecked = false };
            panel.Children.Add(cbSafe);
            panel.Children.Add(PanelButton("RPCS3 GUI 열기 (RPCN 설정)", () =>
                Process.Start(new ProcessStartInfo(_p.Rpcs3Exe) { WorkingDirectory = _p.Rpcs3Dir, UseShellExecute = true })));
            panel.Children.Add(PanelButton("RPCN Online 실행", () =>
            {
                safe = cbSafe.IsChecked == true;
                string prof = safe ? Project.ProfileNetplaySafe : Project.ProfileNetplay;
                Preflight(prof, true);
                if (safe) Append("Netplay Safe 프로필 사용 (실험 설정 비활성)");
                Launch(prof, safe ? "RPCN Online (Safe)" : "RPCN Online");
                w.Close();
            }));
            w.Content = panel;
            w.ShowDialog();
        }

        // ------------------------------------------------------------- SETTINGS
        private void BtnGraphics_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("SETTINGS — Graphics", 560);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(new TextBlock { Text = "Graphics preset", FontWeight = FontWeights.Bold, Margin = new Thickness(4, 6, 4, 2) });
            var presetBox = new ComboBox { Margin = new Thickness(4), Width = 260, HorizontalAlignment = HorizontalAlignment.Left };
            presetBox.Items.Add("Standard (RPCS3 자체 화질 · ReShade 없음)");
            presetBox.Items.Add("Pro Enhanced (ReShade: Deband + CAS + Levels + Vibrance + SMAA)");
            presetBox.SelectedIndex = _p.GraphicsPreset == Project.PresetProEnhanced ? 1 : 0;
            panel.Children.Add(presetBox);

            panel.Children.Add(new TextBlock { Text = "Display Mode", FontWeight = FontWeights.Bold, Margin = new Thickness(4, 10, 4, 2) });
            var modeBox = new ComboBox { Margin = new Thickness(4), Width = 260, HorizontalAlignment = HorizontalAlignment.Left };
            foreach (var m in new[] { "Borderless4K", "Fullscreen", "Windowed" }) modeBox.Items.Add(m);
            modeBox.SelectedItem = _p.DisplayMode;
            panel.Children.Add(modeBox);

            panel.Children.Add(new TextBlock { Text = "Resolution Scale (PRO)", FontWeight = FontWeights.Bold, Margin = new Thickness(4, 10, 4, 2) });
            var resBox = new ComboBox { Margin = new Thickness(4), Width = 260, HorizontalAlignment = HorizontalAlignment.Left };
            resBox.Items.Add("4K 300% (DC_PRO_4K)");
            resBox.Items.Add("5K 400% (DC_PRO_MAX_5K)");
            resBox.SelectedIndex = 0;
            panel.Children.Add(resBox);

            var cbVsync = new CheckBox { Content = "VSync Full (권장)", IsChecked = _p.VSync, Margin = new Thickness(4, 10, 4, 2) };
            var cbSilent = new CheckBox { Content = "ReShade Silent (메뉴/OSD 숨김 · Home 으로 호출)", IsChecked = _p.ReShadeSilent, Margin = new Thickness(4, 4, 4, 2) };
            panel.Children.Add(cbVsync);
            panel.Children.Add(cbSilent);
            panel.Children.Add(Info("AF 16x · MSAA Auto · Stretch Off · 16:9 · VBlank 60 · Frame Skip OFF 는 고정입니다.\n" +
                                    "UI 깨짐/글자 가독성/화면비는 이 값들로 유지됩니다."));

            panel.Children.Add(PanelButton("APPLY", () =>
            {
                _p.DisplayMode = modeBox.SelectedItem.ToString();
                _p.VSync = cbVsync.IsChecked == true;
                _p.ReShadeSilent = cbSilent.IsChecked == true;
                _p.ApplyGraphicsPreset(presetBox.SelectedIndex == 1 ? Project.PresetProEnhanced : Project.PresetStandard);
                Append($"GRAPHICS: preset={_p.GraphicsPreset} mode={_p.DisplayMode} vsync={_p.VSync} silent={_p.ReShadeSilent}");
                RefreshStatus();
                w.Close();
            }));
            w.Content = panel;
            w.ShowDialog();
        }

        private void BtnController_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("SETTINGS — Controller", 420);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info("1P: " + _p.ControllerHandler(1) + "\n2P: " + _p.ControllerHandler(2) +
                "\n\nLocal / Remote Co-op 에는 2P 패드 설정이 필요합니다.\nRPCS3 GUI → 게임패드 아이콘에서 Player 2 를 지정하세요.\n" +
                "패드 설정은 RPCS3 가 관리하며 이 런처는 값을 수정하지 않습니다."));
            panel.Children.Add(PanelButton("RPCS3 게임패드 설정 열기", () =>
                Process.Start(new ProcessStartInfo(_p.Rpcs3Exe) { WorkingDirectory = _p.Rpcs3Dir, UseShellExecute = true })));
            panel.Children.Add(PanelButton("입력 설정 폴더 열기", () =>
                OpenPath(Path.Combine(_p.Rpcs3Dir, "config", "input_configs"))));
            panel.Children.Add(PanelButton("새로 고침", () => { RefreshStatus(); MessageBox.Show("1P: " + _p.ControllerHandler(1) + "\n2P: " + _p.ControllerHandler(2)); }));
            w.Content = panel;
            w.ShowDialog();
        }

        private void BtnNetwork_Click(object sender, RoutedEventArgs e)
        {
            bool fwPriv = false, fwPub = false;
            try
            {
                var psi = new ProcessStartInfo("powershell", "-NoProfile -Command \"(Get-NetFirewallRule -DisplayName 'RPCS3 (*' | Measure-Object).Count\"")
                { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using var p = Process.Start(psi);
                string o = p.StandardOutput.ReadToEnd().Trim();
                int n = 0; int.TryParse(o, out n);
                fwPriv = n > 0; fwPub = n > 0;
            }
            catch { }

            var w = MakePanel("SETTINGS — Network", 460);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info(
                $"RPCN configuration : {(_p.RpcnConfigured() ? "✓ 설정됨 (RPCS3 관리)" : "✗ 미설정 — RPCS3 → RPCN → Create Account")}\n" +
                $"Firewall rule      : {(fwPriv ? "✓ RPCS3 인바운드 허용 규칙 있음" : "✗ 규칙 없음 — Host 시 연결 실패 가능")}\n" +
                $"Sunshine           : {_p.SunshineStatus()}\n\n" +
                "권장:\n" +
                "· 유선 LAN만 사용(Wi-Fi 비활성) — 이중 NIC 환경에서 소스 인터페이스 혼선 방지\n" +
                "· VPN(NordVPN 등) 완전 종료 — split tunneling 충돌 사례 있음\n" +
                "· UPNP Off 유지, 공유기에서 필요 시 UDP 3658 포워딩\n" +
                "· RPCN 서버: np.rpcs3.net:31313"));
            panel.Children.Add(PanelButton("방화벽 규칙 추가/확인 (관리자 필요)", () =>
                Process.Start(new ProcessStartInfo("cmd.exe", "/c \"E:\\PS3\\Tools\\firewall_rpcs3_allow.cmd\"") { UseShellExecute = true })));
            panel.Children.Add(PanelButton("RPCS3 GUI 열기 (RPCN 설정)", () =>
                Process.Start(new ProcessStartInfo(_p.Rpcs3Exe) { WorkingDirectory = _p.Rpcs3Dir, UseShellExecute = true })));
            panel.Children.Add(PanelButton("네트워크 진단 (RPCN 서버 도달성)", () =>
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"$r = Test-NetConnection -ComputerName np.rpcs3.net -Port 31313 -WarningAction SilentlyContinue; 'RPCN np.rpcs3.net:31313 -> ' + $r.TcpTestSucceeded\"")
                { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using var p = Process.Start(psi);
                MessageBox.Show(p.StandardOutput.ReadToEnd(), "Network");
            }));
            w.Content = panel;
            w.ShowDialog();
        }

        private void BtnAdvanced_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("SETTINGS — Advanced", 520);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info(
                "ROOT           : " + _p.Root + "\n" +
                "RPCS3 Current  : " + _p.Rpcs3Dir + "\n" +
                "RPCS3 KnownGood: " + Path.Combine(_p.Rpcs3Dir, "KnownGood") + " (" + (File.Exists(_p.KnownGoodExe) ? "있음" : "없음") + ")\n" +
                "Game           : " + (_p.GameDir ?? "(미탐지)") + "\n" +
                "TITLE ID / VER : " + _p.TitleId + " / " + _p.GameVersion() + "\n" +
                "PPU HASH       : " + _p.PpuHash() + "\n" +
                "ReShade        : " + _p.ReShadeVersion() + " · preset=" + _p.GraphicsPreset + " · silent=" + _p.ReShadeSilent + "\n" +
                "Profiles       : " + _p.ProfilesDir + "\n" +
                "Runtime configs: " + _p.RuntimeDir));
            panel.Children.Add(PanelButton("런타임 설정 폴더 열기", () => OpenPath(_p.RuntimeDir)));
            panel.Children.Add(PanelButton("프로필 폴더 열기", () => OpenPath(_p.ProfilesDir)));
            panel.Children.Add(PanelButton("RPCS3 config 폴더 열기", () => OpenPath(Path.Combine(_p.Rpcs3Dir, "config"))));
            panel.Children.Add(Info("고급 설정은 RPCS3 자체 GUI에서 다루는 것을 권장합니다.\n이 런처는 그래픽/디스플레이/멀티플레이 프리셋만 관리합니다."));
            w.Content = panel;
            w.ShowDialog();
        }

        // ------------------------------------------------------------- TOOLS
        private void BtnDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            var items = _p.RunDiagnostics();
            var w = MakePanel("TOOLS — Diagnostics", 560);
            var panel = new StackPanel { Margin = new Thickness(14) };
            var text = new TextBlock
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Text = string.Join(Environment.NewLine, items.Select(i =>
                    (i.Ok ? "✓ " : "✗ ") + i.Name.PadRight(34) + i.Detail + (i.Ok || i.Hint.Length == 0 ? "" : Environment.NewLine + "     → " + i.Hint)))
            };
            panel.Children.Add(new ScrollViewer { Content = text, Height = 360, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            panel.Children.Add(PanelButton("진단 결과를 Logs\\diagnostics.txt 로 저장", () =>
            {
                string outFile = Path.Combine(_p.LogDir, "diagnostics.txt");
                Directory.CreateDirectory(_p.LogDir);
                File.WriteAllLines(outFile, items.Select(i => (i.Ok ? "[OK]  " : "[NG]  ") + i.Name.PadRight(34) + i.Detail + (i.Hint.Length > 0 ? "  -> " + i.Hint : "")));
                MessageBox.Show("저장됨: " + outFile);
            }));
            panel.Children.Add(PanelButton("닫기", () => w.Close()));
            w.Content = panel;
            w.ShowDialog();
            Append($"진단: {items.Count(i => i.Ok)}/{items.Count} 정상");
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            var r = MessageBox.Show(
                "RPCS3 설정을 기본값으로 되돌립니다.\n\n" +
                "· 그래픽: AF 16x · MSAA Auto · Stretch Off · 16:9 · VSync Full · VBlank 60 · Frame Skip OFF\n" +
                "· 멀티플레이: RPCN On · UPNP Off · Clans Off · Bind 0.0.0.0\n" +
                "· 그래픽 프리셋: Standard\n\n" +
                "세이브/트로피는 절대 건드리지 않습니다. 계속할까요?",
                "Reset RPCS3 Settings", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (r != MessageBoxResult.OK) return;
            string backup = _p.BackupSettings("before_reset");
            Append("설정 백업: " + Path.GetFileName(backup));
            _p.ResetGraphics();
            _p.ResetMultiplayer();
            Append("Reset 완료 (그래픽/멀티플레이 기본값). 세이브는 변경되지 않았습니다.");
            RefreshStatus();
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("TOOLS — Backup / Restore (설정)", 500);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info("RPCS3 config · custom config · ReShade.ini · CurrentSettings.ini · 프로필 · 입력설정을 백업/복원합니다.\n" +
                                     "세이브/트로피는 이 기능의 대상이 아니며 삭제되지 않습니다.\n(세이브 백업은 CHEAT OFFLINE 실행 시 자동 + SAVES 메뉴)"));
            var list = new ListBox { Height = 220, Margin = new Thickness(4) };
            void Reload()
            {
                list.Items.Clear();
                foreach (var b in _p.ListSettingsBackups()) list.Items.Add(Path.GetFileName(b));
                if (list.Items.Count > 0) list.SelectedIndex = 0;
            }
            Reload();
            panel.Children.Add(list);
            panel.Children.Add(PanelButton("지금 백업 만들기", () =>
            {
                string b = _p.BackupSettings("manual");
                MessageBox.Show("백업 완료: " + b);
                Reload();
            }));
            panel.Children.Add(PanelButton("선택한 백업으로 복원", () =>
            {
                var backups = _p.ListSettingsBackups();
                if (list.SelectedIndex < 0 || list.SelectedIndex >= backups.Count) return;
                if (MessageBox.Show("선택한 백업으로 설정을 복원할까요?\n복원 전 현재 설정도 자동 백업됩니다.", "Restore", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
                _p.BackupSettings("before_restore");
                _p.RestoreSettingsBackup(backups[list.SelectedIndex]);
                MessageBox.Show("복원 완료. 세이브는 변경되지 않았습니다.");
                RefreshStatus();
            }));
            panel.Children.Add(PanelButton("SAVES 폴더 열기 (세이브 백업/복원)", () => OpenPath(_p.SavesDir)));
            w.Content = panel;
            w.ShowDialog();
        }

        private void BtnMaint_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("TOOLS — KnownGood / Logs", 460);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info("KnownGood = 검증 완료된 RPCS3 fallback 스냅샷.\n업데이트 직후에는 삭제하지 말고, 문제가 생기면 여기서 실행하세요."));
            panel.Children.Add(PanelButton("KNOWN GOOD RPCS3 실행 (fallback)", () =>
            {
                if (!File.Exists(_p.KnownGoodExe)) { MessageBox.Show("KnownGood 스냅샷이 없습니다."); return; }
                Launch(Project.Profile4K, "KNOWN GOOD", knownGood: true);
                w.Close();
            }));
            panel.Children.Add(PanelButton("세이브 동기화 (Current → KnownGood)", () =>
            {
                string src = Path.Combine(_p.Rpcs3Dir, "dev_hdd0", "home", "00000001");
                string dst = Path.Combine(_p.Rpcs3Dir, "KnownGood", "dev_hdd0", "home", "00000001");
                if (!Directory.Exists(dst)) { MessageBox.Show("KnownGood dev_hdd0가 없습니다."); return; }
                foreach (var sub in new[] { "savedata", "trophy" })
                {
                    string s = Path.Combine(src, sub), d = Path.Combine(dst, sub);
                    if (!Directory.Exists(s)) continue;
                    foreach (var f in Directory.GetFiles(s, "*", SearchOption.AllDirectories))
                    {
                        string t = Path.Combine(d, f.Substring(s.Length).TrimStart(Path.DirectorySeparatorChar));
                        Directory.CreateDirectory(Path.GetDirectoryName(t));
                        File.Copy(f, t, true);
                    }
                }
                MessageBox.Show("세이브/트로피를 KnownGood로 동기화했습니다.");
            }));
            panel.Children.Add(PanelButton("창 스타일 복원 (Borderless 해제)", () =>
            {
                new BorderlessEngine(_p, 0).RestoreNow();
                MessageBox.Show("게임 창이 실행 중이면 원래 창 스타일로 복원했습니다.");
            }));
            panel.Children.Add(PanelButton("LOGS 폴더 열기", () => OpenPath(_p.LogDir)));
            panel.Children.Add(PanelButton("BACKUPS 폴더 열기", () => OpenPath(_p.BackupDir)));
            w.Content = panel;
            w.ShowDialog();
        }

        // ------------------------------------------------------------- ui helpers
        private static Window MakePanel(string title, int height)
        {
            return new Window
            {
                Title = title,
                Width = 620,
                Height = height,
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x21, 0x26)),
                Foreground = Brushes.Gainsboro,
            };
        }

        private static TextBlock Info(string text) => new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Silver,
            Margin = new Thickness(4, 4, 4, 8),
            FontSize = 12,
        };

        private static Button PanelButton(string text, Action onClick)
        {
            var b = new Button { Content = text, Margin = new Thickness(4), Padding = new Thickness(10, 8, 10, 8), FontSize = 13, Cursor = System.Windows.Input.Cursors.Hand };
            b.Click += (s, e) => { try { onClick(); } catch (Exception ex) { MessageBox.Show(ex.Message); } };
            return b;
        }

        private static void OpenPath(string path)
        {
            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"") { UseShellExecute = true });
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
