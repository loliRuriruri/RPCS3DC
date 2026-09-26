using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DragonCrownRemoteCoop
{
    public partial class MainWindow : Window
    {
        private readonly string _role;
        private bool _busy;

        public MainWindow() : this("host") { }

        public MainWindow(string role)
        {
            InitializeComponent();
            _role = (role ?? "host").ToLowerInvariant();
            AppEnv.Discover();
            HostSetup.CaptureBaseline();
            AppEnv.Log($"remote coop setup start (root={AppEnv.Root}, role={_role})");
            ShowRole(_role);
            RefreshAll();
            Append("Dragon's Crown Remote Co-op Setup v1 준비 완료");
            Append("HOST: Sunshine + 가상패드(XInput) + RPCS3 Player 2 · GUEST: Moonlight + 패드 (RPCS3 불필요)");
        }

        // ---------------------------------------------------------------- ui helpers
        private void Append(string text)
        {
            TxtLog.AppendText(DateTime.Now.ToString("HH:mm:ss ") + text + Environment.NewLine);
            TxtLog.ScrollToEnd();
        }

        private void ShowRole(string role)
        {
            bool host = role != "guest";
            PanelHost.Visibility = host ? Visibility.Visible : Visibility.Collapsed;
            PanelGuest.Visibility = host ? Visibility.Collapsed : Visibility.Visible;
            // Dragon's Crown palette: crimson = HOST (active), forest = GUEST (active), stone = inactive
            BtnRoleHost.Background = new SolidColorBrush(host ? Color.FromRgb(0x5A, 0x2A, 0x2A) : Color.FromRgb(0x1B, 0x20, 0x29));
            BtnRoleGuest.Background = new SolidColorBrush(host ? Color.FromRgb(0x1B, 0x20, 0x29) : Color.FromRgb(0x22, 0x40, 0x2F));
        }

        private void RefreshAll()
        {
            RefreshHost();
            RefreshGuest();
            TxtRoot.Text = "ROOT: " + AppEnv.Root + "   ·   로그: Logs\\remote_coop_helper.log   ·   백업: Backups\\RemoteCoop";
        }

        private void RefreshHost()
        {
            var s = HostSetup.GetStatus();
            string ck(bool ok) => ok ? "✓" : "✗";
            TxtHostStatus.Text = string.Join(Environment.NewLine, new[]
            {
                $"Sunshine          {ck(s.SunshineInstalled)} {(s.SunshineInstalled ? (s.SunshineVersion.Length > 0 ? s.SunshineVersion : "installed") : "Not Installed")}" +
                    $"    Service {ck(s.ServiceRunning)} {(s.ServiceExists ? s.ServiceState : "not found")}    WebUI {ck(s.WebUiReachable)}",
                $"Gamepad Backend   {ck(s.BackendReadyForDc)} {(s.Backend.FreeReady ? "FREE: " + s.Backend.FreeLabel : s.Backend.Label)}" +
                    (s.Backend.VirtualHidInstalled ? "   (premium: Virtual HID)" : ""),
                $"Controller Input  {ck(s.Policy.ControllerEnabled)} {(s.Policy.ControllerEnabled ? "ON" : "OFF")}    Keyboard/Mouse {(s.Policy.ControllerOnly ? "OFF (controller-only)" : "ON")}    gamepad={s.Policy.Gamepad}",
                $"Virtual Pad       {ck(s.VirtualPadReady)} {s.VirtualPadState}  (xinput={s.XInputCount})",
                $"RPCS3             {ck(s.Rpcs3Found)} {(s.Rpcs3Found ? Rpcs3Integration.Rpcs3Exe : "not found")}",
                $"Player 1          {ck(s.P1.Length > 0 && s.P1 != "없음")} {s.P1}  (never modified)",
                $"Player 2          {ck(Rpcs3Integration.Player2IsXInput)} {s.P2}" +
                    (Rpcs3Integration.Player2IsXInput ? "" : "   → [SET P2 TO XINPUT]"),
                $"Guest             {ck(s.GuestConnected)} {(s.GuestConnected ? "Connected" : "Waiting")}",
                $"HOST READY        {ck(s.Ready)} {(s.Ready ? "READY" : "NOT READY")}",
            });

            TxtHostBackend.Text = string.Join(Environment.NewLine, new[]
            {
                "GAMEPAD BACKEND",
                "  [ FREE — Recommended for Dragon's Crown ]",
                $"    ViGEmBus       {(s.Backend.ViGEmBusInstalled ? "✓ " + s.Backend.FreeLabel : "✗ not installed")}",
                "                   Xbox 360 / XInput emulation · Free · Legacy / EOL · Enough for Dragon's Crown Remote 2P",
                "  [ PREMIUM — Optional ]",
                $"    Virtual HID    {(s.Backend.VirtualHidInstalled ? "✓ " + s.Backend.PremiumLabel : "✗ not installed (optional)")}",
                "                   Current Sunshine advanced driver · Paid license required · NOT REQUIRED for Dragon's Crown",
                $"  Dragon's Crown Ready: {(s.BackendReadyForDc ? "✓ YES" : "✗ NO")}    gamepad={(s.Policy.Gamepad.Length > 0 ? s.Policy.Gamepad : "auto(default)")}",
            });

            var steps = HostSetup.Steps();
            TxtHostSteps.Text = string.Join("   ", steps.Select(t => t.step + (t.ok ? " ✓" : " ✗"))) +
                                "   → " + (steps.All(t => t.ok) ? "READY" : "INCOMPLETE");
        }

        private void RefreshGuest()
        {
            var s = GuestSetup.GetStatus();
            string ck(bool ok) => ok ? "✓" : "✗";
            TxtGuestStatus.Text = string.Join(Environment.NewLine, new[]
            {
                $"Moonlight         {ck(s.MoonlightInstalled)} {(s.MoonlightInstalled ? (s.MoonlightVersion.Length > 0 ? s.MoonlightVersion : "installed") : "Not Installed")}",
                $"Controller        {ck(s.ControllerCount > 0)} {s.ControllerState}" +
                    (s.ControllerNames.Length > 0 ? "  (" + string.Join(", ", s.ControllerNames.Take(2)) + ")" : ""),
                $"Host              {ck(s.HostConfigured)} {(s.HostConfigured ? s.HostAddress : "Not Paired")}",
                $"Connection        {ck(s.MoonlightRunning)} {(s.MoonlightRunning ? "Moonlight running" : "Not connected")}    Pairing: {s.PairingStatus}",
                $"Stream Ready      {ck(s.StreamingCapable)} {(s.StreamingCapable ? "YES" : "NO")}",
            });
            var steps = GuestSetup.Steps();
            TxtGuestSteps.Text = string.Join("   ", steps.Select(t => t.step + (t.ok ? " ✓" : " ✗"))) +
                                 "   → " + (steps.All(t => t.ok) ? "READY" : "INCOMPLETE");
            if (!string.IsNullOrEmpty(s.HostAddress) && string.IsNullOrEmpty(TxtHostAddress.Text))
                TxtHostAddress.Text = s.HostAddress;
        }

        private async void RunTask(string label, Func<Action<string>, (bool ok, string message)> work)
        {
            if (_busy) { Append("이미 작업이 진행 중입니다."); return; }
            _busy = true;
            Append(label + " ...");
            try
            {
                var result = await Task.Run(() => work(msg => Dispatcher.Invoke(() => Append("  " + msg))));
                Append((result.ok ? "OK: " : "FAILED: ") + result.message);
                if (!result.ok)
                    MessageBox.Show(result.message, label, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                Append("오류: " + ex.Message);
                AppEnv.Log(label + " error: " + ex.Message);
                MessageBox.Show(ex.Message, label, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _busy = false;
                RefreshAll();
            }
        }

        // ---------------------------------------------------------------- role
        private void BtnRoleHost_Click(object sender, RoutedEventArgs e) { ShowRole("host"); RefreshHost(); }
        private void BtnRoleGuest_Click(object sender, RoutedEventArgs e) { ShowRole("guest"); RefreshGuest(); }
        private void BtnRefreshHost_Click(object sender, RoutedEventArgs e) { RefreshHost(); Append("HOST 상태 새로 고침"); }
        private void BtnRefreshGuest_Click(object sender, RoutedEventArgs e) { RefreshGuest(); Append("GUEST 상태 새로 고침"); }

        // ---------------------------------------------------------------- host actions
        private void BtnInstallHost_Click(object sender, RoutedEventArgs e)
        {
            bool controllerOnly = CbControllerOnly.IsChecked == true;
            RunTask("HOST INSTALL / REPAIR", progress =>
            {
                if (!SunshineManager.IsInstalled)
                {
                    var (ok, msg) = HostSetup.InstallSunshine(progress);
                    if (!ok) return (false, msg);
                }
                else progress("Sunshine already installed - skipping installer");

                progress("Applying controller-only policy (controller=enabled, gamepad=x360, keyboard/mouse=disabled) ...");
                if (controllerOnly)
                {
                    bool policyOk = SunshineManager.ApplyControllerOnly(true, out string policyErr);
                    if (!policyOk) progress("WARNING: policy not applied: " + policyErr);
                }

                if (!HostSetup.GetStatus().BackendReadyForDc)
                    progress("Gamepad backend missing — use [INSTALL FREE GAMEPAD DRIVER (ViGEmBus)] (free, recommended). " +
                             "Virtual HID Driver (paid) is optional and NOT required for Dragon's Crown.");

                progress("Starting Sunshine ...");
                var (started, startMsg) = HostSetup.StartSunshine();
                progress(startMsg);

                progress("Opening Sunshine Web UI (create your own account; the helper never stores passwords) ...");
                if (SunshineManager.WebUiReachable()) SunshineManager.OpenWebUi();

                return (true, "HOST setup finished. Next: pair Moonlight (guest) and run [TEST REMOTE PAD].");
            });
        }

        private void BtnStartSunshine_Click(object sender, RoutedEventArgs e)
        {
            RunTask("START SUNSHINE", _ =>
            {
                var (ok, msg) = HostSetup.StartSunshine();
                return (ok, msg);
            });
        }

        private void BtnOpenSunshine_Click(object sender, RoutedEventArgs e)
        {
            if (!SunshineManager.IsInstalled) { MessageBox.Show("Sunshine 이 설치되어 있지 않습니다. [INSTALL / REPAIR HOST] 를 먼저 실행하세요."); return; }
            SunshineManager.OpenExe();
            Append("Sunshine 실행");
        }

        private void BtnOpenWebUi_Click(object sender, RoutedEventArgs e)
        {
            SunshineManager.OpenWebUi();
            Append("Sunshine Web UI 열기: https://localhost:47990 (계정/PIN 은 사용자가 직접 입력)");
        }

        private void BtnOpenSunCfg_Click(object sender, RoutedEventArgs e)
        {
            SunshineManager.OpenConfigFolder();
            Append("Sunshine config 폴더 열기");
        }

        private void BtnTestPad_Click(object sender, RoutedEventArgs e)
        {
            RunTask("TEST REMOTE PAD", progress =>
            {
                var r = HostSetup.TestRemotePad(8, progress);
                string msg = string.Join(Environment.NewLine, r.Lines);
                return (r.InputReceived, msg);
            });
        }

        private void BtnSetP2_Click(object sender, RoutedEventArgs e)
        {
            if (Rpcs3Integration.Player2IsXInput) { MessageBox.Show("Player 2 는 이미 XInput 입니다. 변경하지 않습니다."); return; }
            var r = MessageBox.Show(
                "RPCS3 Player 2 를 XInput 으로 설정합니다.\n\n" +
                "· Player 1 설정은 절대 변경하지 않습니다.\n" +
                "· 적용 전 input config 를 Backups\\RemoteCoop 에 백업합니다.\n" +
                "· RPCS3 가 실행 중이면 먼저 종료해야 합니다.\n\n계속할까요?",
                "SET P2 TO XINPUT", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (r != MessageBoxResult.OK) return;
            bool ok = Rpcs3Integration.SetPlayer2ToXInput(out string error);
            if (ok) { Append("Player 2 → XInput 적용 완료 (백업됨)"); MessageBox.Show("Player 2 를 XInput 으로 설정했습니다."); }
            else { Append("Player 2 설정 실패: " + error); MessageBox.Show(error, "SET P2 TO XINPUT", MessageBoxButton.OK, MessageBoxImage.Warning); }
            RefreshHost();
        }

        private void BtnOpenPadSettings_Click(object sender, RoutedEventArgs e)
        {
            Rpcs3Integration.OpenRpcs3();
            Append("RPCS3 GUI → 게임패드 설정에서 Player 2 를 XInput 으로 지정할 수 있습니다.");
        }

        private void BtnStartRemote_Click(object sender, RoutedEventArgs e)
        {
            var s = HostSetup.GetStatus();
            if (!s.Ready)
            {
                var r = MessageBox.Show(
                    "HOST READY 조건이 모두 충족되지 않았습니다.\n\n" +
                    $"Sunshine: {(s.SunshineInstalled ? "OK" : "missing")} / service: {(s.ServiceRunning ? "running" : "stopped")}\n" +
                    $"Gamepad backend: {s.Backend.Label}\n" +
                    $"Controller-only: {(s.Policy.ControllerOnly ? "ON" : "OFF")}\n" +
                    $"Player 2: {s.P2}\n\n계속할까요?",
                    "START REMOTE CO-OP", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;
            }
            if (Rpcs3Integration.StartRemoteCoop(out string error))
                Append("메인 런처 실행 → MULTIPLAYER → Remote Co-op 에서 게임을 시작하세요 (P1=로컬, P2=가상 XInput).");
            else MessageBox.Show(error, "START REMOTE CO-OP", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void BtnViGEm_Click(object sender, RoutedEventArgs e)
        {
            var r = MessageBox.Show(
                "ViGEmBus — FREE gamepad driver (recommended for Dragon's Crown)\n" +
                "· Xbox 360 / XInput 가상패드 (RPCS3 Player 2 와 일치)\n" +
                "· 무료 · legacy/EOL (저장소 archived 2023-11)\n" +
                "· 공식 nefarius/ViGEmBus GitHub release 에서만 다운로드\n" +
                "· Virtual HID Driver(유료)는 선택 사항이며 이 프로젝트에는 필요하지 않습니다.\n\n설치할까요?",
                "INSTALL FREE GAMEPAD DRIVER (ViGEmBus)", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (r != MessageBoxResult.OK) return;
            RunTask("INSTALL ViGEmBus (FREE)", progress => HostSetup.InstallViGEmBus(progress));
        }

        private void BtnRestoreSun_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("최신 Sunshine config 백업으로 복원할까요? (복원 전 현재 설정도 백업됩니다)", "RESTORE SUNSHINE CONFIG",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            bool ok = BackupManager.RestoreSunshine(null, out string error);
            MessageBox.Show(ok ? "Sunshine config 를 복원했습니다." : "복원 실패: " + error);
            Append(ok ? "Sunshine config 복원" : "Sunshine 복원 실패: " + error);
        }

        private void BtnRestoreP2_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("최신 RPCS3 input config 백업으로 복원할까요?\n(RPCS3 가 실행 중이면 실패합니다)", "RESTORE RPCS3 INPUT",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            bool ok = BackupManager.RestoreRpcs3Input(null, out string error);
            MessageBox.Show(ok ? "RPCS3 input config 를 복원했습니다." : "복원 실패: " + error);
            Append(ok ? "RPCS3 input 복원" : "RPCS3 복원 실패: " + error);
        }

        // ---------------------------------------------------------------- guest actions
        private void BtnInstallGuest_Click(object sender, RoutedEventArgs e)
        {
            RunTask("GUEST INSTALL MOONLIGHT", progress => GuestSetup.InstallMoonlight(progress));
        }

        private void BtnTestController_Click(object sender, RoutedEventArgs e)
        {
            RunTask("TEST CONTROLLER", progress =>
            {
                var r = GuestSetup.TestController(8, progress);
                return (r.InputReceived, string.Join(Environment.NewLine, r.Lines));
            });
        }

        private void BtnPairHost_Click(object sender, RoutedEventArgs e)
        {
            string host = TxtHostAddress.Text.Trim();
            var (okSet, msgSet) = GuestSetup.SetHostAddress(host);
            if (!okSet) { MessageBox.Show(msgSet, "ADD / PAIR HOST", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Append(msgSet);
            var (ok, msg) = MoonlightManager.Pair(host);
            MessageBox.Show(msg, "ADD / PAIR HOST", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
            RefreshGuest();
        }

        private void BtnStreamDesktop_Click(object sender, RoutedEventArgs e)
        {
            string host = TxtHostAddress.Text.Trim();
            var (ok, msg) = MoonlightManager.StreamDesktop(host);
            MessageBox.Show(msg, "START DESKTOP STREAM", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }

        private void BtnOpenMoonlight_Click(object sender, RoutedEventArgs e)
        {
            if (!MoonlightManager.IsInstalled) { MessageBox.Show("Moonlight 이 설치되어 있지 않습니다."); return; }
            MoonlightManager.Open();
            Append("Moonlight 실행");
        }

        private void BtnOpenJoyCpl_Click(object sender, RoutedEventArgs e)
        {
            MoonlightManager.OpenWindowsGameControllers();
            Append("Windows 게임 컨트롤러(joy.cpl) 열기");
        }
    }
}
