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
            _p.Log("launcher start (root=" + _p.Root + ")");
            if (_p.ReShadeSilent) { try { _p.ApplyReShadeSilent(true); } catch { } }
            RefreshStatus();
            TxtRoot.Text = "ROOT: " + _p.Root + "   ·   게임: " + (_p.GameExe ?? "(미탐지 — RPCS3 게임 목록에 등록 필요)");
            Append("Dragon's Crown PRO Enhanced 준비 완료");
        }

        // ------------------------------------------------------------- helpers
        private void Append(string text)
        {
            TxtLog.AppendText(DateTime.Now.ToString("HH:mm:ss ") + text + Environment.NewLine);
            TxtLog.ScrollToEnd();
        }

        private void RefreshStatus()
        {
            string check(bool ok) => ok ? "✓" : "✗";
            string dot(bool on) => on ? "●" : "○";
            var lines = new List<string>
            {
                $"GAME     {check(_p.GameExe != null && File.Exists(_p.GameExe))} Dragon's Crown [{_p.TitleId}]",
                $"VERSION  {check(_p.GameVersion().StartsWith("01."))} {_p.GameVersion()}",
                $"RPCS3    {check(File.Exists(_p.Rpcs3Exe))} Current  ({_p.Rpcs3Build()})",
                $"GPU      {check(true)} Vulkan (RPCS3 설정 사용)",
                $"DISPLAY  {check(true)} {_p.DisplayMode}  {( _p.DisplayMode == "Borderless4K" ? "· 모니터 전체" : "")}",
                $"ReSHade  {check(_p.ReShadeVersion() != "없음")} {_p.ReShadeVersion()} · Silent {(_p.ReShadeSilent ? "ON" : "OFF")} · {(_p.ReShadeEnabled ? "Enabled" : "Disabled")}",
                $"RPCN     {dot(_p.RpcnConfigured())} {(_p.RpcnConfigured() ? "Configured" : "Offline / 미설정")}",
                $"CHEATS   {dot(_p.CheatsOrPatchesEnabled())} {(_p.CheatsOrPatchesEnabled() ? "항목 있음" : "Disabled")}",
                $"VSYNC    {check(_p.VSync)} {(_p.VSync ? "Full (ON)" : "Disabled")} · VBlank 60 · Frame Skip OFF",
                $"PPU HASH {_p.PpuHash()}",
            };
            TxtStatus.Text = string.Join(Environment.NewLine, lines);
        }

        private void Preflight(string profile, bool needCheat, bool needNetplay)
        {
            if (!File.Exists(_p.Rpcs3Exe)) throw new Exception("rpcs3.exe를 찾을 수 없습니다: " + _p.Rpcs3Exe);
            if (_p.GameExe == null || !File.Exists(_p.GameExe)) throw new Exception("게임 덤프를 찾을 수 없습니다. RPCS3 게임 목록에 Dragon's Crown을 추가하세요.");
            if (!File.Exists(_p.ProfilePath(profile))) throw new Exception("프로필이 없습니다: " + profile);
            if (needNetplay && !_p.RpcnConfigured()) throw new Exception("RPCN 계정이 설정되지 않았습니다. RPCS3 → RPCN → Create Account / Log In 후 다시 시도하세요.");
            if (needNetplay && _p.CheatsOrPatchesEnabled()) throw new Exception("치트/패치 항목이 있습니다. NETPLAY 전에 모두 비활성화하세요.");
            if (needCheat && _p.RpcnConfigured() && false) { /* informational only */ }
        }

        private void Launch(string profile, bool knownGood, bool borderless, string label)
        {
            try
            {
                if (_game != null && !_game.HasExited) { Append("이미 게임이 실행 중입니다."); return; }
                _p.SaveSettings();
                Append($"{label}: 실행 준비...");
                var proc = _p.Launch(profile, knownGood, borderless);
                _game = proc;
                Append($"{label}: RPCS3 PID {proc.Id} · profile {profile} · {_p.DisplayMode}");
                if (borderless && _p.DisplayMode == "Borderless4K")
                    Append("게임 창이 뜨면 자동으로 테두리 없이 모니터 전체로 전환됩니다 (종료 시 원복).");
                proc.EnableRaisingEvents = true;
                proc.Exited += (s, e) => Dispatcher.Invoke(() =>
                {
                    Append($"{label}: 게임 종료 (exit code {proc.ExitCode})");
                    _p.Log($"game exit code={proc.ExitCode}");
                    RefreshStatus();
                });
                RefreshStatus();
            }
            catch (Exception ex)
            {
                Append("오류: " + ex.Message);
                MessageBox.Show(ex.Message, "실행할 수 없습니다", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ------------------------------------------------------------- main buttons
        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            try { Preflight("Dragons_Crown/DC_PRO_4K", false, false); Launch("Dragons_Crown/DC_PRO_4K", false, true, "PLAY 4K PRO"); }
            catch (Exception ex) { Append("PLAY: " + ex.Message); MessageBox.Show(ex.Message, "PLAY", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void BtnProMax_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Preflight("Dragons_Crown/DC_PRO_MAX_5K", false, false);
                var r = MessageBox.Show("PRO MAX는 400% (5120x2880 → 4K) 입니다.\n60fps/frame pacing이 불안정하면 PLAY(300%)를 사용하세요.\n\n계속할까요?",
                    "PRO MAX", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (r != MessageBoxResult.OK) return;
                Launch("Dragons_Crown/DC_PRO_MAX_5K", false, true, "PRO MAX 5K");
            }
            catch (Exception ex) { Append("PRO MAX: " + ex.Message); MessageBox.Show(ex.Message, "PRO MAX", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void BtnNetplay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Preflight("Dragons_Crown/DC_NETPLAY", false, true);
                var ready = new List<string>
                {
                    "RPCN configured        : " + (_p.RpcnConfigured() ? "✓" : "✗"),
                    "Network Status         : Connected (프로필)",
                    "PSN Status             : RPCN (프로필)",
                    $"Dragon's Crown version : {_p.GameVersion()}",
                    "Cheats                 : " + (_p.CheatsOrPatchesEnabled() ? "✗ 항목 있음" : "✓ OFF"),
                    "Experimental patches   : " + (_p.CheatsOrPatchesEnabled() ? "✗ 확인 필요" : "✓ OFF"),
                    "",
                    "두 PC 모두 같은 TITLE_ID(BCAS20298)와 APP_VER(01.09)여야 합니다.",
                    "첫 연결 테스트에서는 ReShade를 끄는 것을 권장합니다."
                };
                MessageBox.Show(string.Join(Environment.NewLine, ready), "NETPLAY READY", MessageBoxButton.OK, MessageBoxImage.Information);
                Launch("Dragons_Crown/DC_NETPLAY", false, true, "NETPLAY");
            }
            catch (Exception ex) { Append("NETPLAY: " + ex.Message); MessageBox.Show(ex.Message, "NETPLAY", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void BtnCheat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Preflight("Dragons_Crown/DC_CHEAT_OFFLINE", true, false);
                if (_p.RpcnConfigured())
                {
                    var r = MessageBox.Show("RPCN 계정이 설정되어 있습니다. 치트 세션은 네트워크가 차단된 CHEAT OFFLINE 프로필로 실행됩니다.\n" +
                        "치트 사용 중에는 온라인 플레이를 하지 마세요.\n\n계속할까요?", "CHEAT OFFLINE", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    if (r != MessageBoxResult.OK) return;
                }
                string backup = _p.BackupSaves("cheat_offline");
                Append("Save 자동 백업 완료: " + Path.GetFileName(backup));
                Launch("Dragons_Crown/DC_CHEAT_OFFLINE", false, true, "CHEAT OFFLINE");
                Append("치트는 RPCS3 게임 목록 → 게임 우클릭 → Cheats 에서 사용합니다.");
            }
            catch (Exception ex) { Append("CHEAT: " + ex.Message); MessageBox.Show(ex.Message, "CHEAT OFFLINE", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        // ------------------------------------------------------------- panels
        private Window MakePanel(string title, int height = 420)
        {
            return new Window
            {
                Title = title,
                Width = 560,
                Height = height,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x21, 0x26)),
                Foreground = Brushes.Gainsboro,
            };
        }

        private static Button PanelButton(string text, Action onClick)
        {
            var b = new Button { Content = text, Margin = new Thickness(4), Padding = new Thickness(10, 8, 10, 8), FontSize = 13, Cursor = System.Windows.Input.Cursors.Hand };
            b.Click += (s, e) => { try { onClick(); } catch (Exception ex) { MessageBox.Show(ex.Message); } };
            return b;
        }

        private void BtnSaves_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("SAVES — Dragon's Crown");
            var stack = new StackPanel { Margin = new Thickness(14) };
            stack.Children.Add(new TextBlock
            {
                Text = "세이브 폴더: " + Path.Combine(_p.SavesDir, "AutoBackup"),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 0, 4, 10), Foreground = Brushes.Silver
            });
            stack.Children.Add(PanelButton("SAVE BACKUP (지금 백업)", () =>
            {
                string b = _p.BackupSaves("manual");
                MessageBox.Show("백업 완료:\n" + b, "SAVE BACKUP", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshStatus();
            }));
            stack.Children.Add(PanelButton("RESTORE SAVE (백업 목록에서 복원)", () => RestoreDialog()));
            stack.Children.Add(PanelButton("OPEN SAVE FOLDER", () => OpenPath(_p.SavesDir)));
            stack.Children.Add(PanelButton("OPEN RPCS3 SAVEDATA FOLDER", () => OpenPath(Path.Combine(_p.Rpcs3Dir, "dev_hdd0", "home", "00000001", "savedata"))));
            w.Content = stack;
            w.ShowDialog();
        }

        private void RestoreDialog()
        {
            string dir = Path.Combine(_p.SavesDir, "AutoBackup");
            if (!Directory.Exists(dir)) { MessageBox.Show("백업 폴더가 없습니다."); return; }
            var backups = Directory.GetDirectories(dir).OrderByDescending(d => d).ToList();
            if (backups.Count == 0) { MessageBox.Show("백업이 없습니다."); return; }

            var w = MakePanel("RESTORE SAVE", 460);
            var list = new ListBox { Margin = new Thickness(8), Height = 250 };
            foreach (var b in backups) list.Items.Add(Path.GetFileName(b));
            list.SelectedIndex = 0;
            var panel = new StackPanel { Margin = new Thickness(10) };
            panel.Children.Add(new TextBlock { Text = "복원할 백업을 선택하세요. 복원 전에 현재 세이브를 자동 백업합니다.", Margin = new Thickness(4, 0, 4, 8), TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(list);
            panel.Children.Add(PanelButton("RESTORE", () =>
            {
                if (list.SelectedIndex < 0) return;
                string chosen = backups[list.SelectedIndex];
                string safety = _p.BackupSaves("before_restore");
                string savedata = Path.Combine(_p.Rpcs3Dir, "dev_hdd0", "home", "00000001", "savedata");
                foreach (var srcDir in Directory.GetDirectories(chosen))
                {
                    string name = Path.GetFileName(srcDir);
                    string target = Path.Combine(savedata, name);
                    if (Directory.Exists(target))
                    {
                        string quarantine = target + "_replaced_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        Directory.Move(target, quarantine);
                    }
                    CopyTree(srcDir, target);
                }
                _p.Log("restore: " + Path.GetFileName(chosen) + " (safety=" + Path.GetFileName(safety) + ")");
                MessageBox.Show("복원 완료.\n현재 세이브는 다음 위치에 안전 백업되었습니다:\n" + safety, "RESTORE", MessageBoxButton.OK, MessageBoxImage.Information);
            }));
            w.Content = panel;
            w.ShowDialog();
        }

        private static void CopyTree(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src)) File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(src)) CopyTree(d, Path.Combine(dst, Path.GetFileName(d)));
        }

        private void BtnGraphics_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("GRAPHICS — display / graphics", 520);
            var panel = new StackPanel { Margin = new Thickness(14) };

            panel.Children.Add(new TextBlock { Text = "Display Mode", FontWeight = FontWeights.Bold, Margin = new Thickness(4, 6, 4, 2) });
            var modeBox = new ComboBox { Margin = new Thickness(4), Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
            foreach (var m in new[] { "Borderless4K", "Fullscreen", "Windowed" }) modeBox.Items.Add(m);
            modeBox.SelectedItem = _p.DisplayMode;
            panel.Children.Add(modeBox);

            panel.Children.Add(new TextBlock { Text = "Resolution (PLAY 프로필)", FontWeight = FontWeights.Bold, Margin = new Thickness(4, 10, 4, 2) });
            var resBox = new ComboBox { Margin = new Thickness(4), Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
            resBox.Items.Add("4K 300% (DC_PRO_4K)");
            resBox.Items.Add("5K 400% (DC_PRO_MAX_5K)");
            resBox.SelectedIndex = 0;
            panel.Children.Add(resBox);

            var cbVsync = new CheckBox { Content = "VSync ON (Full) — 권장", IsChecked = _p.VSync, Margin = new Thickness(4, 10, 4, 2) };
            var cbRe = new CheckBox { Content = "ReShade 사용 (Deband + 약한 CAS)", IsChecked = _p.ReShadeEnabled, Margin = new Thickness(4, 4, 4, 2) };
            var cbSilent = new CheckBox { Content = "ReShade Silent (메뉴/OSD 숨김, Home으로 호출)", IsChecked = _p.ReShadeSilent, Margin = new Thickness(4, 4, 4, 2) };
            panel.Children.Add(cbVsync);
            panel.Children.Add(cbRe);
            panel.Children.Add(cbSilent);

            panel.Children.Add(new TextBlock
            {
                Text = "VBlank 60 · Frame Skip OFF · Frame limit Auto 는 게임 속도/로직 보호를 위해 고정입니다.",
                TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Silver, Margin = new Thickness(4, 10, 4, 2)
            });

            panel.Children.Add(PanelButton("APPLY", () =>
            {
                _p.DisplayMode = modeBox.SelectedItem.ToString();
                _p.VSync = cbVsync.IsChecked == true;
                _p.ReShadeEnabled = cbRe.IsChecked == true;
                _p.ReShadeSilent = cbSilent.IsChecked == true;
                _p.SaveSettings();
                _p.ApplyReShadeSilent(_p.ReShadeSilent);
                Append($"GRAPHICS: mode={_p.DisplayMode} vsync={_p.VSync} reshade={_p.ReShadeEnabled} silent={_p.ReShadeSilent}");
                RefreshStatus();
                w.Close();
            }));
            w.Content = panel;
            w.ShowDialog();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("SETTINGS — 정보 / RPCN / 컨트롤러", 560);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(new TextBlock
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Text = string.Join(Environment.NewLine, new[]
                {
                    "ROOT          : " + _p.Root,
                    "RPCS3 Current : " + _p.Rpcs3Dir,
                    "RPCS3 KnownGood: " + Path.Combine(_p.Rpcs3Dir, "KnownGood") + " (" + (File.Exists(_p.KnownGoodExe) ? "있음" : "없음") + ")",
                    "RPCS3 build   : " + _p.Rpcs3Build(),
                    "GAME          : " + (_p.GameDir ?? "(미탐지)"),
                    "TITLE ID      : " + _p.TitleId,
                    "VERSION       : " + _p.GameVersion(),
                    "PPU HASH      : " + _p.PpuHash(),
                    "ReShade       : " + _p.ReShadeVersion() + "  (silent=" + _p.ReShadeSilent + ")",
                    "RPCN account  : " + (_p.RpcnConfigured() ? "설정됨 (RPCS3가 관리)" : "미설정"),
                    "Display mode  : " + _p.DisplayMode,
                })
            });
            panel.Children.Add(PanelButton("RPCS3 GUI 열기 (RPCN 계정/설정)", () =>
            {
                Process.Start(new ProcessStartInfo(_p.Rpcs3Exe) { WorkingDirectory = _p.Rpcs3Dir, UseShellExecute = true });
                Append("RPCS3 GUI 실행 — RPCN 메뉴에서 계정 생성/로그인, 컨트롤러 설정을 진행하세요.");
            }));
            panel.Children.Add(PanelButton("컨트롤러 설정 (RPCS3 Pad Settings)", () =>
            {
                Process.Start(new ProcessStartInfo(_p.Rpcs3Exe) { WorkingDirectory = _p.Rpcs3Dir, UseShellExecute = true });
                Append("RPCS3 GUI → 게임패드 아이콘에서 컨트롤러를 설정하세요.");
            }));
            panel.Children.Add(PanelButton("ROOT 경로 다시 지정 (rpcs3.exe 선택)", () =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "RPCS3 설치 폴더의 rpcs3.exe를 선택하세요",
                    Filter = "rpcs3.exe|rpcs3.exe|모든 파일 (*.*)|*.*",
                };
                if (dlg.ShowDialog() == true)
                {
                    string rpcs3 = Path.GetDirectoryName(dlg.FileName);          // <ROOT>\RPCS3
                    string root = Directory.GetParent(rpcs3)?.FullName;          // <ROOT>
                    if (root == null || !Directory.Exists(Path.Combine(root, "Profiles")))
                    {
                        MessageBox.Show("선택한 위치에서 ROOT 구조(RPCS3/Profiles)를 찾지 못했습니다.");
                        return;
                    }
                    File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "root.txt"), root);
                    MessageBox.Show("다음 실행부터 적용됩니다: " + root);
                }
            }));
            w.Content = panel;
            w.ShowDialog();
        }

        private void BtnMaintenance_Click(object sender, RoutedEventArgs e)
        {
            var w = MakePanel("MAINTENANCE", 480);
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(new TextBlock
            {
                Text = "KnownGood RPCS3 / 창 복원 / 로그 / 백업 도구",
                Foreground = Brushes.Silver, Margin = new Thickness(4, 0, 4, 8)
            });
            panel.Children.Add(PanelButton("KNOWN GOOD RPCS3 실행 (fallback)", () =>
            {
                if (!File.Exists(_p.KnownGoodExe)) { MessageBox.Show("KnownGood 스냅샷이 없습니다."); return; }
                var r = MessageBox.Show("검증 완료된 KnownGood RPCS3로 실행합니다.\n(세이브는 KnownGood의 dev_hdd0를 사용합니다)\n\n계속할까요?",
                    "KNOWN GOOD", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (r != MessageBoxResult.OK) return;
                Launch("Dragons_Crown/DC_PRO_4K", true, true, "KNOWN GOOD");
            }));
            panel.Children.Add(PanelButton("세이브 동기화 (Current → KnownGood)", () =>
            {
                string src = Path.Combine(_p.Rpcs3Dir, "dev_hdd0", "home", "00000001");
                string dst = Path.Combine(_p.Rpcs3Dir, "KnownGood", "dev_hdd0", "home", "00000001");
                if (!Directory.Exists(dst)) { MessageBox.Show("KnownGood dev_hdd0가 없습니다."); return; }
                CopyTree(Path.Combine(src, "savedata"), Path.Combine(dst, "savedata"));
                CopyTree(Path.Combine(src, "trophy"), Path.Combine(dst, "trophy"));
                MessageBox.Show("세이브/트로피를 KnownGood로 동기화했습니다.");
            }));
            panel.Children.Add(PanelButton("창 스타일 복원 (Borderless 해제)", () =>
            {
                var engine = new BorderlessEngine(_p, 0);
                MessageBox.Show("게임 창이 실행 중이면 3초 내 원래 창 스타일로 복원됩니다.\n게임을 종료하면 자동 복원됩니다.");
                engine.Start();
            }));
            panel.Children.Add(PanelButton("LOGS 폴더 열기", () => OpenPath(_p.LogDir)));
            panel.Children.Add(PanelButton("프로필 / 런타임 설정 폴더 열기", () => OpenPath(_p.ProfilesDir)));
            panel.Children.Add(PanelButton("BACKUPS 폴더 열기", () => OpenPath(Path.Combine(_p.Root, "Backups"))));
            w.Content = panel;
            w.ShowDialog();
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
