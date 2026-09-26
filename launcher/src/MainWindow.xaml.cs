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
            _p.Log("launcher start (root=" + _p.Root + ", preset=" + _p.GraphicsPreset + ", resolution=" + _p.ResolutionProfile + ")");
            try { _p.EnsureReShadePaths(); } catch { }
            try { _p.ApplyReShadeSilent(_p.ReShadeSilent); } catch { }
            RefreshStatus();
            TxtRoot.Text = "ROOT: " + _p.Root + "   ·   게임: " + (_p.GameExe ?? "(미탐지 — RPCS3 게임 목록에 등록 필요)");
            Append("Dragon's Crown PC Edition (Phase 1 RC) 준비 완료");
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
            var cp = _p.CheatPatchStatus();
            string enabledState = cp.CheatEnabled == "YES" || cp.PatchEnabled == "YES" ? "Enabled!"
                : (cp.CheatEnabled == "UNKNOWN" || cp.PatchEnabled == "UNKNOWN") ? "UNKNOWN" : "Disabled";
            TxtStatus.Text = string.Join(Environment.NewLine, new[]
            {
                $"GAME      {ck(_p.GameExe != null && File.Exists(_p.GameExe))} Dragon's Crown [{_p.TitleId}]  v{_p.GameVersion()}",
                $"RPCS3     {ck(File.Exists(_p.Rpcs3Exe))} Current ({_p.Rpcs3Build()})   KnownGood {ck(File.Exists(_p.KnownGoodExe))}",
                $"FIRMWARE  {ck(_p.FirmwareVersion() != "미확인")} {_p.FirmwareVersion()}    PPU {_p.PpuHash()}",
                $"GRAPHICS  {ck(true)} {_p.GraphicsSummary} · AF 16x · MSAA Auto · Stretch Off",
                $"DISPLAY   {ck(true)} {_p.DisplayMode} · VSync {( _p.VSync ? "Full" : "Off")} · VBlank 60 · Frame Skip OFF",
                $"RESHADE   {ck(_p.ReShadeVersion() != "없음")} {_p.ReShadeVersion()} · {(_p.ReShadeEnabled ? "ON (Pro Enhanced)" : "OFF (Standard)")} · Silent {(_p.ReShadeSilent ? "ON" : "OFF")}",
                $"PAD 1/2   {ck(p1 != "Null" && p1 != "없음")} {p1}  /  {ck(p2 != "Null" && p2 != "없음")} {p2}",
                $"NETWORK   {dot(_p.RpcnConfigured())} RPCN Configured {(_p.RpcnConfigured() ? "YES" : "NO")} · Login {_p.RpcnLoginStatus()} · Sunshine {_p.SunshineStatus()}",
                $"RPCS3 GUI {Rpcs3UiBridge.StatusLine(_p)}",
                $"CHEATS    {dot(cp.CheatEnabled == "YES" || cp.PatchEnabled == "YES")} {enabledState} (cheats {cp.CheatEnabled} / patches {cp.PatchEnabled})",
            });
            string resShort = _p.ResolutionShort;
            TxtStandardSub.Text = resShort + " · AF 16x · ReShade 없음";
            TxtProSub.Text = resShort + " + ReShade (선명도/색감)";
        }

        /// <summary>Preflight checks. RPCN / Netplay Safe enforce BCAS20298 v01.09 and clean runtime.</summary>
        private void Preflight(string profile, bool needNetplay = false, bool needSecondPad = false)
        {
            if (!File.Exists(_p.Rpcs3Exe)) throw new Exception("rpcs3.exe를 찾을 수 없습니다: " + _p.Rpcs3Exe);
            if (_p.GameExe == null || !File.Exists(_p.GameExe)) throw new Exception("게임 덤프를 찾을 수 없습니다. RPCS3 게임 목록에 Dragon's Crown을 추가하세요.");
            if (!File.Exists(_p.ProfilePath(profile))) throw new Exception("프로필이 없습니다: " + profile + "\nTOOLS → Backup/Restore 로 복원할 수 있습니다.");
            if (needNetplay)
            {
                if (_p.GameVersion() != Project.NetplayRequiredVersion)
                    throw new Exception(
                        "RPCN Online requires Dragon's Crown BCAS20298 v" + Project.NetplayRequiredVersion + ".\n\n" +
                        "Detected:\nBCAS20298 v" + _p.GameVersion() + "\n\n" +
                        "Apply the official v" + Project.NetplayRequiredVersion + " update before continuing.");
                if (!_p.RpcnConfigured())
                    throw new Exception("RPCN 계정이 설정되지 않았습니다.\nRPCS3 → RPCN → Create Account / Log In 후 다시 시도하세요.");
                bool? enabled = _p.EnabledCheatsOrPatches();
                if (enabled == true)
                    throw new Exception("활성화된 치트/패치가 있습니다. NETPLAY 전에 모두 비활성화하세요.");
                if (enabled == null)
                    Append("주의: 치트/패치 활성화 여부를 자동 판별할 수 없습니다 (UNKNOWN). RPCS3에서 직접 확인하세요.");
            }
            if (needSecondPad)
            {
                string p2 = _p.ControllerHandler(2);
                if (p2 == "Null" || p2 == "없음")
                    Append("주의: 2P 패드가 설정되어 있지 않습니다 (SETTINGS → Controller).");
            }
        }

        private void Launch(string profile, string label, bool knownGood = false, bool borderless = true, bool forceReShadeOff = false)
        {
            try
            {
                if (_game != null && !_game.HasExited) { Append("이미 게임이 실행 중입니다."); return; }

                // duplicate-instance guard: RPCS3 GUI already running -> warn (never kill/restart it)
                var running = Rpcs3UiBridge.ProjectProcesses(_p);
                if (running.Count > 0)
                {
                    Append($"주의: RPCS3 가 이미 실행 중입니다 (pid {string.Join(", ", running.Select(x => x.Id))}). " +
                           "두 번째 인스턴스는 설정/저장 충돌을 일으킬 수 있습니다.");
                    if (App.CliMode)
                    {
                        _p.Log("launch: duplicate instance warning (already running) - CLI continues");
                    }
                    else
                    {
                        var r = MessageBox.Show(
                            "RPCS3 가 이미 실행 중입니다.\n\n" +
                            "두 번째 인스턴스를 실행하면 설정/저장 충돌이 발생할 수 있습니다.\n" +
                            "기존 RPCS3 창을 종료한 뒤 실행하는 것을 권장합니다.\n\n" +
                            "계속할까요? (기존 RPCS3 는 종료/재시작하지 않습니다)",
                            label + " — RPCS3 already running", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                        if (r != MessageBoxResult.Yes) { Append("실행 취소 (기존 RPCS3 유지)"); return; }
                    }
                }

                _p.SaveSettings();
                var proc = _p.Launch(profile, knownGood, borderless, forceReShadeOff);
                _game = proc;
                Append($"{label}: RPCS3 PID {proc.Id} · {profile} · {_p.DisplayMode} · {_p.GraphicsSummary}" +
                       (forceReShadeOff ? " · ReShade 강제 OFF (runtime)" : ""));
                if (borderless && _p.DisplayMode == "Borderless4K")
                    Append("게임 창이 뜨면 자동으로 테두리 없이 모니터 전체로 전환됩니다 (종료 시 원복).");
                proc.EnableRaisingEvents = true;
                proc.Exited += (s, e) => Dispatcher.Invoke(() =>
                {
                    Append($"{label}: 게임 종료 (exit {proc.ExitCode})");
                    _p.Log($"game exit code={proc.ExitCode}");
                    if (forceReShadeOff) Append("Netplay Safe 종료: 사용자 그래픽 설정은 변경되지 않았습니다 (" + _p.GraphicsSummary + ").");
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
                Preflight(_p.ActiveProfile);
                _p.ApplyGraphicsPreset(Project.PresetStandard);
                Append("Graphics A (Standard): RPCS3 자체 화질 · AF 16x · ReShade OFF · " + _p.ResolutionShort);
                Launch(_p.ActiveProfile, "PLAY · Standard");
            }
            catch (Exception ex) { Append("PLAY: " + ex.Message); MessageBox.Show(ex.Message, "PLAY · Standard", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void BtnProEnhanced_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Preflight(_p.ActiveProfile);
                _p.ApplyGraphicsPreset(Project.PresetProEnhanced);
                Append("Graphics B (Pro Enhanced): ReShade ON (Deband + CAS + Levels + Vibrance + SMAA, Silent) · " + _p.ResolutionShort);
                Launch(_p.ActiveProfile, "PLAY · Pro Enhanced");
            }
            catch (Exception ex) { Append("PLAY: " + ex.Message); MessageBox.Show(ex.Message, "PLAY · Pro Enhanced", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        // ------------------------------------------------------------- MULTIPLAYER
        private void BtnLocal_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("MULTIPLAYER — Local (1P / 2P 같은 PC)", 440);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info(
                "한 PC에서 2인 플레이 (게임의 로컬 협동)\n" +
                "Status: IMPLEMENTED / HARDWARE TEST PENDING (2P 패드 필요)\n\n" +
                "1) 2P 패드를 PC에 연결하고 RPCS3 → 게임패드 설정에서 Player 2 핸들러를 지정하세요.\n" +
                "2) 게임에서 캐릭터 선택 화면 또는 Tavern(주점)에서 2P 패드의 Start 를 누르면 합류합니다.\n" +
                "3) 로컬 협동은 트로피/세이브가 Player 1 기준으로 기록됩니다.\n\n" +
                "현재 1P: " + _p.ControllerHandler(1) + " / 2P: " + _p.ControllerHandler(2)));
            panel.Children.Add(PanelButton("RPCS3 게임패드 설정 열기", () =>
            {
                OpenRpcs3Settings(Rpcs3SettingsKind.Controller);
                Append("RPCS3 GUI → 패드 설정에서 1P/2P 를 지정하세요 (기존 RPCS3 창을 재사용합니다).");
            }));
            panel.Children.Add(PanelButton("Local 2P 로 실행 (" + _p.ResolutionShort + ")", () =>
            {
                Preflight(_p.ActiveProfile, false, true);
                Launch(_p.ActiveProfile, "Local 2P");
                w.Close();
            }));
            w.Content = panel;
            w.ShowDialog();
        }

        // ------------------------------------------------------------- MULTIPLAYER — Remote Co-op Helper
        private string HelperExe => Path.Combine(_p.Root, "Launcher", "DragonCrownRemoteCoopSetup.exe");

        private Dictionary<string, string> ReadHelperStatus()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(HelperExe)) return d;
                string outFile = Path.Combine(_p.LogDir, "remote_coop_status.txt");
                var psi = new ProcessStartInfo(HelperExe, "--status \"" + outFile + "\"")
                { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(HelperExe) };
                using var proc = Process.Start(psi);
                proc.WaitForExit(9000);
                if (!File.Exists(outFile)) return d;
                foreach (var line in File.ReadAllLines(outFile))
                {
                    var kv = line.Split(new[] { '=' }, 2);
                    if (kv.Length == 2) d[kv[0].Trim()] = kv[1].Trim();
                }
            }
            catch { }
            return d;
        }

        private void RunHelper(string arguments)
        {
            try
            {
                if (!File.Exists(HelperExe)) { MessageBox.Show("Remote Co-op Helper 가 없습니다:\n" + HelperExe); return; }
                Process.Start(new ProcessStartInfo(HelperExe, arguments)
                { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(HelperExe) });
                Append("Remote Co-op Helper 실행: " + arguments);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnRemote_Click(object sender, RoutedEventArgs e)
        {
            var st = ReadHelperStatus();
            string val(string key, string fallback) => st.TryGetValue(key, out var v) ? v : fallback;
            string sun = val("HOST_SUNSHINE_INSTALLED", "NO") == "YES"
                ? "✓ " + val("HOST_SUNSHINE_VERSION", "installed") + " / service " + val("HOST_SUNSHINE_SERVICE", "?")
                : "✗ 미설치 (Helper 로 설치)";
            string backend = val("HOST_BACKEND", _p.SunshineStatus());
            string p2 = val("HOST_P2", _p.ControllerHandler(2));
            string pad = val("HOST_VIRTUAL_PAD", "WAITING FOR GUEST");
            string ready = val("HOST_READY", "NO");
            string guest = val("HOST_GUEST", "WAITING");
            string rpcs3 = Rpcs3UiBridge.StatusLine(_p);
            bool p2ok = _p.ControllerHandler(2).Equals("XInput", StringComparison.OrdinalIgnoreCase);

            var w = MakePanel("MULTIPLAYER — Remote Co-op (Sunshine + Moonlight)", 600);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info(
                "한 PC에서 로컬 2P를 실행하고, 친구가 원격으로 2P 패드를 조작합니다.\n" +
                "(RPCN/PSN 불필요 · 같은 세션 · 그래픽 MOD와 충돌 가능성 낮음)\n" +
                "Status: IMPLEMENTED / E2E TEST PENDING (2대 PC 필요)\n\n" +
                "Helper 상태 (DragonCrownRemoteCoopSetup.exe):\n" +
                $"  Sunshine      : {sun}\n" +
                $"  Gamepad       : {backend}   (FREE 경로: ViGEmBus + gamepad=x360 — Virtual HID 유료는 선택)\n" +
                $"  RPCS3         : {rpcs3}\n" +
                $"  Player 1      : {_p.ControllerHandler(1)}\n" +
                $"  Player 2      : {p2}\n" +
                $"  Guest Pad     : {pad}   Guest: {guest}\n" +
                $"  HOST READY    : {ready}\n\n" +
                "절차:\n" +
                "1) HOST: [SETUP HOST] → Sunshine 설치/실행 + Controller-only + (무료) ViGEmBus + P2 XInput\n" +
                "2) GUEST(친구 PC): [GUEST SETUP] → Moonlight 설치 + 패드 + HOST 페어링\n" +
                "3) 친구가 Moonlight Desktop 스트림 접속 → [TEST REMOTE PAD]\n" +
                "4) [START REMOTE CO-OP] → 게임 실행 → 2P Start 로 합류"));
            panel.Children.Add(PanelButton("SETUP HOST (DragonCrownRemoteCoopSetup.exe --host)", () => RunHelper("--host")));
            panel.Children.Add(PanelButton("RPCS3 CONTROLLER SETTINGS (기존 RPCS3 재사용)", () => OpenRpcs3Settings(Rpcs3SettingsKind.Controller)));
            if (!p2ok)
            {
                panel.Children.Add(PanelButton("FIX P2 TO XINPUT (백업 후 적용)", () =>
                {
                    try
                    {
                        if (!File.Exists(HelperExe)) { MessageBox.Show("Helper 가 없습니다."); return; }
                        string outFile = Path.Combine(_p.LogDir, "remote_coop_fixp2.txt");
                        var psi = new ProcessStartInfo(HelperExe, "--fix-p2 \"" + outFile + "\"")
                        { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(HelperExe) };
                        using var proc = Process.Start(psi);
                        proc.WaitForExit(20000);
                        MessageBox.Show(File.Exists(outFile) ? File.ReadAllText(outFile) : "(결과 없음 — Logs\\remote_coop_helper.log 확인)", "FIX P2 TO XINPUT");
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message); }
                    RefreshStatus();
                }));
            }
            panel.Children.Add(PanelButton("GUEST SETUP 안내 (친구 PC용 --guest)", () => RunHelper("--guest")));
            panel.Children.Add(PanelButton("TEST REMOTE PAD (8초 입력 테스트)", () =>
            {
                try
                {
                    if (!File.Exists(HelperExe)) { MessageBox.Show("Helper 가 없습니다."); return; }
                    string outFile = Path.Combine(_p.LogDir, "remote_coop_padtest.txt");
                    var psi = new ProcessStartInfo(HelperExe, "--test-pad \"" + outFile + "\"")
                    { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(HelperExe) };
                    using var proc = Process.Start(psi);
                    proc.WaitForExit(20000);
                    MessageBox.Show(File.Exists(outFile) ? File.ReadAllText(outFile) : "결과 파일이 없습니다.", "TEST REMOTE PAD");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }));
            panel.Children.Add(PanelButton("진단 다시 실행 (Helper 상태 새로 고침)", () =>
            {
                var st2 = ReadHelperStatus();
                string line(string k) => st2.TryGetValue(k, out var v) ? k + " = " + v : k + " = (없음)";
                MessageBox.Show(string.Join("\n", new[]
                {
                    line("HOST_SUNSHINE_INSTALLED"), line("HOST_SUNSHINE_SERVICE"), line("HOST_SUNSHINE_WEBUI"),
                    line("HOST_BACKEND"), line("HOST_CONTROLLER_ONLY"), line("HOST_P2_XINPUT"),
                    line("HOST_VIRTUAL_PAD"), line("HOST_GUEST"), line("HOST_READY"),
                    line("GUEST_MOONLIGHT_INSTALLED"), line("GUEST_CONTROLLER"),
                }), "Remote Co-op Helper 상태");
                Append("Helper 상태 새로 고침");
            }));
            panel.Children.Add(PanelButton("Sunshine 다운로드 페이지 열기", () =>
                Process.Start(new ProcessStartInfo("https://github.com/LizardByte/Sunshine") { UseShellExecute = true })));
            panel.Children.Add(PanelButton("Remote Co-op 으로 실행 (" + _p.ResolutionShort + ")", () =>
            {
                Preflight(_p.ActiveProfile, false, true);
                if (!_p.SunshineRunning())
                {
                    var r = MessageBox.Show(
                        "Sunshine is not running.\nRemote controller forwarding may not work.\n\nContinue anyway?",
                        "Remote Co-op", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (r != MessageBoxResult.Yes) return;
                }
                Launch(_p.ActiveProfile, "Remote Co-op");
                w.Close();
            }));
            w.Content = panel;
            w.ShowDialog();
        }

        private void BtnRpcn_Click(object sender, RoutedEventArgs e)
        {
            string ver = _p.GameVersion();
            bool verOk = ver == Project.NetplayRequiredVersion;
            bool configured = _p.RpcnConfigured();
            var cp = _p.CheatPatchStatus();
            string cheatState = cp.CheatEnabled == "YES" || cp.PatchEnabled == "YES" ? "✗ Enabled cheat/patch"
                : (cp.CheatEnabled == "UNKNOWN" || cp.PatchEnabled == "UNKNOWN") ? "? UNKNOWN (check RPCS3)"
                : "✓ No enabled cheats";
            var w = MakePanel("MULTIPLAYER — RPCN Native Online", 620);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info(
                "RPCN Native Online\n" +
                "Status: Partial / requires real 2-PC validation\n\n" +
                "Game:\n" +
                $"  {(verOk ? "✓" : "✗")} {_p.TitleId}\n" +
                $"  {(verOk ? "✓" : "✗")} v{ver}" + (verOk ? "" : "  (v" + Project.NetplayRequiredVersion + " 필요 — RPCN 실행 차단)") + "\n\n" +
                "RPCN:\n" +
                $"  {(configured ? "✓" : "✗")} Configured\n" +
                $"  ? Login status not verified (RPCS3 GUI에서 확인)\n\n" +
                "Safety:\n" +
                $"  {cheatState}\n" +
                "  ✓ Netplay Safe available (기본 ON 권장)\n\n" +
                "Compatibility:\n" +
                "  PARTIAL — friend/direct stage join 권장\n" +
                "  Random matchmaking 은 가정하지 않음\n" +
                "  Connecting 에서 멈추는 사례 보고 있음 (Issue #15283)\n\n" +
                "첫 테스트는 Standard + Netplay Safe + ReShade OFF + 유선 LAN + VPN OFF 를 권장합니다."));
            var cbSafe = new CheckBox
            {
                Content = "Netplay Safe 사용 (Standard 강제 · ReShade runtime OFF · 실험/네트워크 옵션 보수값)",
                Margin = new Thickness(4, 8, 4, 4),
                IsChecked = true
            };
            panel.Children.Add(cbSafe);
            panel.Children.Add(PanelButton("RPCN ACCOUNT SETTINGS (RPCS3 재사용)", () => OpenRpcs3Settings(Rpcs3SettingsKind.Rpcn)));
            panel.Children.Add(PanelButton("RPCN Online 실행", () =>
            {
                bool safe = cbSafe.IsChecked == true;
                string prof = safe ? Project.ProfileNetplaySafe : Project.ProfileNetplay;
                Preflight(prof, true);
                if (safe)
                    Append("Netplay Safe: Standard 강제 + ReShade runtime OFF (사용자 설정은 변경되지 않음)");
                Launch(prof, safe ? "RPCN Online (Safe)" : "RPCN Online", forceReShadeOff: safe);
                w.Close();
            }));
            w.Content = panel;
            w.ShowDialog();
        }

        // ------------------------------------------------------------- RPCS3 설정 (bridge)
        private bool _rpcs3Busy;

        private async void OpenRpcs3Settings(Rpcs3SettingsKind kind)
        {
            if (_rpcs3Busy) { Append("RPCS3 설정을 여는 중입니다..."); return; }
            _rpcs3Busy = true;
            Append("RPCS3 설정 여는 중: " + kind + " (기존 인스턴스 재사용)");
            try
            {
                var result = await System.Threading.Tasks.Task.Run(() => Rpcs3UiBridge.OpenSettings(_p, kind));
                string flat = result.message.Replace("\r\n", " | ").Replace("\n", " | ");
                Append("RPCS3: " + flat);
                if (!result.ok) MessageBox.Show(result.message, "RPCS3 설정", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Append("RPCS3 설정 오류: " + ex.Message);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                _rpcs3Busy = false;
                RefreshStatus();
            }
        }

        private void BtnRpcs3Controller_Click(object sender, RoutedEventArgs e) => OpenRpcs3Settings(Rpcs3SettingsKind.Controller);
        private void BtnRpcs3Rpcn_Click(object sender, RoutedEventArgs e) => OpenRpcs3Settings(Rpcs3SettingsKind.Rpcn);
        private void BtnRpcs3General_Click(object sender, RoutedEventArgs e) => OpenRpcs3Settings(Rpcs3SettingsKind.General);
        private void BtnRpcs3Open_Click(object sender, RoutedEventArgs e) => OpenRpcs3Settings(Rpcs3SettingsKind.MainWindow);

        private void BtnRpcs3Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshStatus();
            var procs = Rpcs3UiBridge.ProjectProcesses(_p);
            string detail = procs.Count == 0
                ? "RPCS3 가 실행 중이 아닙니다."
                : $"실행 중: pid {string.Join(", ", procs.Select(x => x.Id))}" +
                  (Rpcs3UiBridge.IsGameRunning(_p) ? " · 게임 실행 중 (설정 일부는 게임 재실행 후 적용)" : "");
            Append("RPCS3 상태: " + detail);
            MessageBox.Show(detail, "RPCS3 상태");
        }

        // ------------------------------------------------------------- SETTINGS
        private void BtnGraphics_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("SETTINGS — Graphics", 600);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(new TextBlock { Text = "Graphics preset", FontWeight = FontWeights.Bold, Margin = new Thickness(4, 6, 4, 2) });
            var presetBox = new ComboBox { Margin = new Thickness(4), Width = 320, HorizontalAlignment = HorizontalAlignment.Left };
            presetBox.Items.Add("Standard (RPCS3 자체 화질 · ReShade 없음)");
            presetBox.Items.Add("Pro Enhanced (ReShade: Deband + CAS + Levels + Vibrance + SMAA)");
            presetBox.SelectedIndex = _p.GraphicsPreset == Project.PresetProEnhanced ? 1 : 0;
            panel.Children.Add(presetBox);

            panel.Children.Add(new TextBlock { Text = "Resolution Scale", FontWeight = FontWeights.Bold, Margin = new Thickness(4, 10, 4, 2) });
            var resBox = new ComboBox { Margin = new Thickness(4), Width = 320, HorizontalAlignment = HorizontalAlignment.Left };
            resBox.Items.Add("4K / 300% — Recommended (DC_PRO_4K)");
            resBox.Items.Add("5K / 400% — Super Sampling (DC_PRO_MAX_5K)");
            resBox.SelectedIndex = _p.ResolutionProfile == Project.Res5K ? 1 : 0;
            panel.Children.Add(resBox);

            panel.Children.Add(new TextBlock { Text = "Display Mode", FontWeight = FontWeights.Bold, Margin = new Thickness(4, 10, 4, 2) });
            var modeBox = new ComboBox { Margin = new Thickness(4), Width = 320, HorizontalAlignment = HorizontalAlignment.Left };
            foreach (var m in new[] { "Borderless4K", "Fullscreen", "Windowed" }) modeBox.Items.Add(m);
            modeBox.SelectedItem = _p.DisplayMode;
            panel.Children.Add(modeBox);

            var cbVsync = new CheckBox { Content = "VSync Full (권장)", IsChecked = _p.VSync, Margin = new Thickness(4, 10, 4, 2) };
            var cbSilent = new CheckBox { Content = "ReShade Silent (메뉴/OSD 숨김 · Home 으로 호출)", IsChecked = _p.ReShadeSilent, Margin = new Thickness(4, 4, 4, 2) };
            panel.Children.Add(cbVsync);
            panel.Children.Add(cbSilent);
            panel.Children.Add(Info("AF 16x · MSAA Auto · Stretch Off · 16:9 · VBlank 60 · Frame Skip OFF 는 고정입니다.\n" +
                                    "Standard / Pro Enhanced 는 프로젝트 자체 프리셋 이름이며 PS4 리소스와 무관합니다.\n" +
                                    "Netplay Safe 는 이 설정을 변경하지 않고 실행 시에만 ReShade 를 차단합니다."));

            panel.Children.Add(PanelButton("APPLY", () =>
            {
                _p.DisplayMode = modeBox.SelectedItem.ToString();
                _p.VSync = cbVsync.IsChecked == true;
                _p.ReShadeSilent = cbSilent.IsChecked == true;
                _p.SetResolution(resBox.SelectedIndex == 1 ? Project.Res5K : Project.Res4K);
                _p.ApplyGraphicsPreset(presetBox.SelectedIndex == 1 ? Project.PresetProEnhanced : Project.PresetStandard);
                Append($"GRAPHICS: preset={_p.GraphicsPreset} resolution={_p.ResolutionShort} mode={_p.DisplayMode} vsync={_p.VSync} silent={_p.ReShadeSilent}");
                RefreshStatus();
                w.Close();
            }));
            w.Content = panel;
            w.ShowDialog();
        }

        private void BtnController_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("SETTINGS — Controller", 440);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info("1P: " + _p.ControllerHandler(1) + "\n2P: " + _p.ControllerHandler(2) +
                "\n\nLocal / Remote Co-op 에는 2P 패드 설정이 필요합니다.\nRPCS3 GUI → 게임패드 아이콘에서 Player 2 를 지정하세요.\n" +
                "패드 설정은 RPCS3 가 관리하며 이 런처는 값을 수정하지 않습니다."));
            panel.Children.Add(PanelButton("RPCS3 게임패드 설정 열기", () => OpenRpcs3Settings(Rpcs3SettingsKind.Controller)));
            panel.Children.Add(PanelButton("입력 설정 폴더 열기", () =>
                OpenPath(Path.Combine(_p.Rpcs3Dir, "config", "input_configs"))));
            panel.Children.Add(PanelButton("새로 고침", () => { RefreshStatus(); MessageBox.Show("1P: " + _p.ControllerHandler(1) + "\n2P: " + _p.ControllerHandler(2)); }));
            w.Content = panel;
            w.ShowDialog();
        }

        private void BtnNetwork_Click(object sender, RoutedEventArgs e)
        {
            bool fw = false;
            try
            {
                var psi = new ProcessStartInfo("powershell", "-NoProfile -Command \"(Get-NetFirewallRule -DisplayName 'RPCS3 (*' | Measure-Object).Count\"")
                { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using var p = Process.Start(psi);
                string o = p.StandardOutput.ReadToEnd().Trim();
                int n = 0; int.TryParse(o, out n);
                fw = n > 0;
            }
            catch { }

            var w = MakePanel("SETTINGS — Network", 480);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info(
                $"RPCN Configured : {(_p.RpcnConfigured() ? "YES" : "NO — RPCS3 → RPCN → Create Account")}\n" +
                $"RPCN Login      : {_p.RpcnLoginStatus()}  (자동 검증 불가 — RPCS3 GUI 확인)\n" +
                $"Firewall rule   : {(fw ? "✓ RPCS3 인바운드 허용 규칙 있음" : "✗ 규칙 없음 — Host 시 연결 실패 가능")}\n" +
                $"Sunshine        : {_p.SunshineStatus()} (Remote Co-op 권장, 필수 아님)\n\n" +
                "권장:\n" +
                "· 유선 LAN만 사용(Wi-Fi 비활성) — 이중 NIC 환경에서 소스 인터페이스 혼선 방지\n" +
                "· VPN(NordVPN 등) 완전 종료 — split tunneling 충돌 사례 있음\n" +
                "· UPNP Off 유지, 공유기에서 필요 시 UDP 3658 포워딩\n" +
                "· RPCN 서버: np.rpcs3.net:31313\n\n" +
                "RPCN 실패 시 순서: Netplay Safe → 네트워크 점검 → local RPCN 진단 → Remote Co-op (문서 참고)"));
            panel.Children.Add(PanelButton("방화벽 규칙 추가/확인 (관리자 필요)", () =>
            {
                string script = _p.FirewallScript;
                if (!File.Exists(script)) { MessageBox.Show("스크립트가 없습니다: " + script); return; }
                Process.Start(new ProcessStartInfo("cmd.exe", "/c \"" + script + "\"") { UseShellExecute = true });
            }));
            panel.Children.Add(PanelButton("RPCN ACCOUNT SETTINGS (RPCS3 재사용)", () => OpenRpcs3Settings(Rpcs3SettingsKind.Rpcn)));
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
            var w = MakePanel("SETTINGS — Advanced", 560);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info(
                "ROOT           : " + _p.Root + "\n" +
                "RPCS3 Current  : " + _p.Rpcs3Dir + "\n" +
                "RPCS3 KnownGood: " + Path.Combine(_p.Rpcs3Dir, "KnownGood") + " (" + (File.Exists(_p.KnownGoodExe) ? "있음" : "없음") + ")\n" +
                "Game           : " + (_p.GameDir ?? "(미탐지)") + "\n" +
                "TITLE ID / VER : " + _p.TitleId + " / " + _p.GameVersion() + "\n" +
                "PPU HASH       : " + _p.PpuHash() + "\n" +
                "Graphics       : " + _p.GraphicsSummary + "  (profile=" + _p.ActiveProfileName + ")\n" +
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
            var w = MakePanel("TOOLS — Diagnostics", 600);
            var panel = new StackPanel { Margin = new Thickness(14) };
            var text = new TextBlock
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Text = string.Join(Environment.NewLine, items.Select(i =>
                    (i.Ok ? "✓ " : "✗ ") + i.Name.PadRight(38) + i.Detail + (i.Ok || i.Hint.Length == 0 ? "" : Environment.NewLine + "     → " + i.Hint)))
            };
            panel.Children.Add(new ScrollViewer { Content = text, Height = 380, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            panel.Children.Add(PanelButton("진단 결과를 Logs\\diagnostics.txt 로 저장", () =>
            {
                string outFile = Path.Combine(_p.LogDir, "diagnostics.txt");
                Directory.CreateDirectory(_p.LogDir);
                File.WriteAllLines(outFile, items.Select(i => (i.Ok ? "[OK]  " : "[NG]  ") + i.Name.PadRight(38) + i.Detail + (i.Hint.Length > 0 ? "  -> " + i.Hint : "")));
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
                "· 그래픽: 4K 300% · AF 16x · MSAA Auto · Stretch Off · 16:9 · VSync Full · VBlank 60 · Frame Skip OFF\n" +
                "· 멀티플레이: RPCN On · UPNP Off · Clans Off · Bind 0.0.0.0\n" +
                "· 그래픽 프리셋: Standard\n\n" +
                "Live savedata 와 trophy 는 Reset 대상이 아닙니다 (변경/삭제되지 않음).\n" +
                "자동 백업 사본(AutoBackup)은 최신 " + Project.SaveBackupRetention + "세대만 보존됩니다.\n\n계속할까요?",
                "Reset RPCS3 Settings", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (r != MessageBoxResult.OK) return;
            string backup = _p.BackupSettings("before_reset");
            Append("설정 백업: " + Path.GetFileName(backup));
            _p.ResetGraphics();
            _p.ResetMultiplayer();
            Append("Reset 완료 (그래픽/멀티플레이 기본값). Live savedata/trophy 는 변경되지 않았습니다.");
            RefreshStatus();
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("TOOLS — Backup / Restore (설정)", 540);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info("RPCS3 config · custom config · ReShade.ini · CurrentSettings.ini · 프로필 · 입력설정을 백업/복원합니다.\n" +
                                     "Live savedata/trophy 는 이 기능의 대상이 아니며 변경/삭제되지 않습니다.\n" +
                                     "(세이브 백업은 CHEAT OFFLINE 실행 시 자동 생성되며, AutoBackup 사본은 최신 " + Project.SaveBackupRetention + "세대 보존)"));
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
                if (MessageBox.Show("선택한 백업으로 설정을 복원할까요?\n복원 전 현재 설정도 자동 백업됩니다.\n(프로젝트 ROOT 밖으로 나가는 경로는 건너뜁니다)", "Restore", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
                _p.BackupSettings("before_restore");
                _p.RestoreSettingsBackup(backups[list.SelectedIndex]);
                MessageBox.Show("복원 완료. Live savedata/trophy 는 변경되지 않았습니다.");
                RefreshStatus();
            }));
            panel.Children.Add(PanelButton("SAVES 폴더 열기 (세이브 백업)", () => OpenPath(_p.SavesDir)));
            w.Content = panel;
            w.ShowDialog();
        }

        private void BtnMaint_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("TOOLS — KnownGood / Logs", 500);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(Info("KnownGood = 검증 완료된 RPCS3 fallback 스냅샷.\n업데이트 직후에는 삭제하지 말고, 문제가 생기면 여기서 실행하세요."));
            panel.Children.Add(PanelButton("KNOWN GOOD RPCS3 실행 (fallback · " + _p.ResolutionShort + ")", () =>
            {
                if (!File.Exists(_p.KnownGoodExe)) { MessageBox.Show("KnownGood 스냅샷이 없습니다."); return; }
                Launch(_p.ActiveProfile, "KNOWN GOOD", knownGood: true);
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
                new BorderlessEngine(_p, 0).RequestStopAndRestore();
                MessageBox.Show("게임 창이 실행 중이면 원래 창 스타일로 복원했습니다.\n(다음 실행 시 Borderless가 다시 적용됩니다)");
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
                Width = 640,
                Height = height,
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.FromRgb(0x14, 0x18, 0x1F)),
                Foreground = new SolidColorBrush(Color.FromRgb(0xED, 0xE3, 0xCC)),
                Icon = Application.Current.MainWindow?.Icon,
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
