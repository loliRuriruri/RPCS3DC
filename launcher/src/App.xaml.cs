using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace DragonCrownProEnhanced
{
    public partial class App : Application
    {
        public static bool CliMode { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // make sure the borderless window style is restored even if this app closes early
            Exit += (s, args) =>
            {
                try
                {
                    // minimal discovery only: never re-run game discovery on exit
                    // (it must not touch game.txt / settings)
                    var p = Project.Discover(discoverGame: false);
                    string state = Path.Combine(p.LogDir, "borderless_state.json");
                    if (File.Exists(state))
                    {
                        var engine = new BorderlessEngine(p, 0);
                        engine.RestoreNow();
                        p.Log("launcher closing: borderless restored");
                    }
                }
                catch { }
            };
            // CLI mode (also used for automated verification):
            //   DragonCrownProEnhanced.exe --launch STANDARD|PROENHANCED|PROMAX|NETPLAY|RPCNSAFE|LOCAL|REMOTE|CHEAT|KNOWNGOOD [--no-wait]
            //   DragonCrownProEnhanced.exe --preset Standard|ProEnhanced
            //   DragonCrownProEnhanced.exe --resolution 4K|5K
            //   DragonCrownProEnhanced.exe --backup [label] / --backup-settings [label]
            //   DragonCrownProEnhanced.exe --restore-window     (immediate restore, no watcher)
            //   DragonCrownProEnhanced.exe --diag [outfile] / --status <outfile>
            //   DragonCrownProEnhanced.exe --reset-graphics / --reset-multiplayer
            var args = e.Args ?? Array.Empty<string>();
            if (args.Length > 0)
            {
                CliMode = true;
                RunCli(args);
                Shutdown();
                return;
            }
            base.OnStartup(e);
            new MainWindow().Show();
        }

        private static void RunCli(string[] args)
        {
            var p = Project.Discover();
            string a0 = args[0].ToLowerInvariant();
            try
            {
                // keep ReShade paths valid even when the project folder moved (portable installs)
                try { p.EnsureReShadePaths(); } catch { }
                switch (a0)
                {
                    case "--status":
                    {
                        string outFile = args.Length > 1 ? args[1] : Path.Combine(p.LogDir, "status.txt");
                        Directory.CreateDirectory(p.LogDir);
                        var cp = p.CheatPatchStatus();
                        var lines = new[]
                        {
                            "ROOT=" + p.Root,
                            "RPCS3=" + p.Rpcs3Exe + " exists=" + File.Exists(p.Rpcs3Exe),
                            "KNOWNGOOD=" + p.KnownGoodExe + " exists=" + File.Exists(p.KnownGoodExe),
                            "GAME=" + (p.GameExe ?? "(none)") + " exists=" + (p.GameExe != null && File.Exists(p.GameExe)),
                            "TITLE_ID=" + p.TitleId,
                            "VERSION=" + p.GameVersion(),
                            "PPU_HASH=" + p.PpuHash(),
                            "RPCS3_BUILD=" + p.Rpcs3Build(),
                            "GRAPHICS_PRESET=" + p.GraphicsPreset,
                            "RESOLUTION_PROFILE=" + p.ResolutionProfile,
                            "ACTIVE_PROFILE=" + p.ActiveProfileName,
                            "RESHADE=" + p.ReShadeVersion() + " enabled=" + p.ReShadeEnabled + " silent=" + p.ReShadeSilent,
                            "RPCN_CONFIGURED=" + p.RpcnConfigured(),
                            "RPCN_LOGIN=" + p.RpcnLoginStatus(),
                            "CHEAT_ENTRIES=" + cp.CheatEntries + " CHEAT_ENABLED=" + cp.CheatEnabled,
                            "PATCH_ENTRIES=" + cp.PatchEntries + " PATCH_ENABLED=" + cp.PatchEnabled,
                            "DISPLAY_MODE=" + p.DisplayMode + " vsync=" + p.VSync,
                            "PROFILES=" + string.Join(",", new[] { "DC_PRO_4K", "DC_PRO_MAX_5K", "DC_NETPLAY", "NETPLAY_SAFE", "DC_CHEAT_OFFLINE" }
                                        .Select(n => n + "=" + File.Exists(p.ProfilePath("Dragons_Crown/" + n)))),
                        };
                        File.WriteAllLines(outFile, lines);
                        p.Log("cli --status -> " + outFile);
                        break;
                    }
                    case "--backup":
                    {
                        string label = args.Length > 1 ? args[1] : "cli";
                        string dir = p.BackupSaves(label);
                        Console.WriteLine(dir);
                        p.Log("cli --backup -> " + dir);
                        break;
                    }
                    case "--restore-window":
                    {
                        // immediate restore from borderless_state.json - never starts a watcher,
                        // and asks any live watcher to yield so the restore sticks
                        new BorderlessEngine(p, 0).RequestStopAndRestore();
                        p.Log("cli --restore-window (immediate restore + stop flag, no watcher)");
                        break;
                    }
                    case "--preset":
                    {
                        string which = args.Length > 1 ? args[1] : "Standard";
                        p.ApplyGraphicsPreset(which.Equals("ProEnhanced", StringComparison.OrdinalIgnoreCase) ? Project.PresetProEnhanced : Project.PresetStandard);
                        p.Log("cli --preset " + which);
                        break;
                    }
                    case "--resolution":
                    {
                        string which = args.Length > 1 ? args[1] : "4K";
                        p.SetResolution(which.Equals("5K", StringComparison.OrdinalIgnoreCase) ? Project.Res5K : Project.Res4K);
                        p.Log("cli --resolution " + which + " -> " + p.ActiveProfileName);
                        break;
                    }
                    case "--diag":
                    {
                        string outFile = args.Length > 1 ? args[1] : Path.Combine(p.LogDir, "diagnostics.txt");
                        Directory.CreateDirectory(p.LogDir);
                        var items = p.RunDiagnostics();
                        File.WriteAllLines(outFile, items.Select(i => (i.Ok ? "[OK]  " : "[NG]  ") + i.Name.PadRight(38) + i.Detail + (i.Hint.Length > 0 ? "  -> " + i.Hint : "")));
                        p.Log("cli --diag -> " + outFile);
                        break;
                    }
                    case "--backup-settings":
                    {
                        string label = args.Length > 1 ? args[1] : "cli";
                        string dir = p.BackupSettings(label);
                        Console.WriteLine(dir);
                        p.Log("cli --backup-settings -> " + dir);
                        break;
                    }
                    case "--reset-graphics":
                        p.BackupSettings("before_reset_graphics");
                        p.ResetGraphics();
                        p.Log("cli --reset-graphics");
                        break;
                    case "--reset-multiplayer":
                        p.BackupSettings("before_reset_multiplayer");
                        p.ResetMultiplayer();
                        p.Log("cli --reset-multiplayer");
                        break;
                    case "--launch":
                    {
                        string what = args.Length > 1 ? args[1].ToUpperInvariant() : "PLAY";
                        string profile;
                        bool knownGood = false, borderless = true, forceReShadeOff = false;
                        bool skipPreflight = args.Any(x => x.Equals("--skip-preflight", StringComparison.OrdinalIgnoreCase));
                        // RPCN / Netplay Safe preflight (same rules as the GUI)
                        if (!skipPreflight && (what == "RPCNSAFE" || what == "NETPLAY"))
                        {
                            if (p.GameVersion() != Project.NetplayRequiredVersion)
                                throw new Exception("RPCN Online requires Dragon's Crown BCAS20298 v" + Project.NetplayRequiredVersion +
                                                    ". Detected: v" + p.GameVersion() + ". Apply the official update before continuing.");
                            if (!p.RpcnConfigured())
                                throw new Exception("RPCN account is not configured (RPCS3 → RPCN → Create Account).");
                            bool? enabled = p.EnabledCheatsOrPatches();
                            if (enabled == true) throw new Exception("Enabled cheat/patch detected. Disable all cheats/patches before netplay.");
                            if (enabled == null) p.Log("preflight: cheat/patch enabled state UNKNOWN - verify in RPCS3");
                        }
                        else if (skipPreflight && (what == "RPCNSAFE" || what == "NETPLAY"))
                        {
                            p.Log("preflight skipped by --skip-preflight (automation only)");
                        }
                        switch (what)
                        {
                            case "PROENHANCED":
                                p.ApplyGraphicsPreset(Project.PresetProEnhanced);
                                profile = p.ActiveProfile;
                                break;
                            case "STANDARD":
                                p.ApplyGraphicsPreset(Project.PresetStandard);
                                profile = p.ActiveProfile;
                                break;
                            case "PROMAX":
                                // legacy CLI behaviour: always the 400% profile
                                profile = Project.Profile5K;
                                break;
                            case "LOCAL":
                            case "REMOTE":
                                profile = p.ActiveProfile;
                                break;
                            case "RPCNSAFE":
                                profile = Project.ProfileNetplaySafe;
                                forceReShadeOff = true;
                                break;
                            case "NETPLAY":
                                profile = Project.ProfileNetplay;
                                break;
                            case "CHEAT":
                                profile = Project.ProfileCheat;
                                p.BackupSaves("cheat_offline");
                                break;
                            case "KNOWNGOOD":
                                profile = p.ActiveProfile;
                                knownGood = true;
                                break;
                            default:
                                profile = p.ActiveProfile;
                                break;
                        }
                        p.ApplyReShadeSilent(p.ReShadeSilent);
                        var proc = p.Launch(profile, knownGood, borderless, forceReShadeOff);
                        Console.WriteLine("PID=" + proc.Id);
                        p.Log("cli --launch " + what + " -> PID " + proc.Id + " (profile=" + profile + ", forcedReShadeOff=" + forceReShadeOff + ")");
                        // keep this process alive so the borderless watcher thread survives until the game ends
                        if (!args.Any(x => x.Equals("--no-wait", StringComparison.OrdinalIgnoreCase)))
                        {
                            proc.WaitForExit();
                            p.Log("cli --launch " + what + " -> game exited (code " + proc.ExitCode + ")");
                        }
                        break;
                    }
                    case "--set-game":
                    {
                        string path = args.Length > 1 ? args[1] : "";
                        bool ok = p.SetGameRoot(path, out string err);
                        Console.WriteLine(ok ? "OK  " + p.GameDir : "FAIL  " + err);
                        p.Log($"cli --set-game [{path}] -> {(ok ? "OK " + p.GameDir : "FAIL " + err)}");
                        break;
                    }
                    case "--game":
                    {
                        string outFile = args.Length > 1 ? args[1] : Path.Combine(p.LogDir, "game_path.txt");
                        string gameTxt = Path.Combine(AppContext.BaseDirectory, "game.txt");
                        var lines = new[]
                        {
                            "ROOT=" + p.Root,
                            "GAMES_YML=" + Path.Combine(p.Rpcs3Dir, "config", "games.yml"),
                            "GAME_ROOT=" + (p.GameDir ?? "(not found)"),
                            "GAME_EXE=" + (p.GameExe ?? "(none)"),
                            "TITLE_ID=" + (p.GameTitleId ?? "(unknown)"),
                            "APP_VER=" + p.GameVersion(),
                            "GAME_TXT=" + (File.Exists(gameTxt) ? File.ReadAllText(gameTxt).Trim() : "(none)"),
                        };
                        Directory.CreateDirectory(p.LogDir);
                        File.WriteAllLines(outFile, lines);
                        foreach (var l in lines) Console.WriteLine(l);
                        p.Log("cli --game -> " + (p.GameDir ?? "(not found)"));
                        break;
                    }
                    case "--probe":
                    {
                        string raw = args.Length > 1 ? args[1] : "";
                        string norm = Project.NormalizePath(raw);
                        bool ok = p.TryResolveGamePath(raw, out string root, out string detail);
                        Console.WriteLine("raw        = [" + raw + "]");
                        Console.WriteLine("normalized = [" + norm + "]");
                        Console.WriteLine("result     = " + (ok ? "OK  " + root : "FAIL"));
                        Console.WriteLine("detail     = " + detail);
                        p.Log($"cli --probe raw=[{raw}] norm=[{norm}] -> {(ok ? "OK " + root : "FAIL")} ({detail})");
                        break;
                    }
                    case "--rpcs3":
                    {
                        string what = args.Length > 1 ? args[1].ToLowerInvariant() : "status";
                        if (what == "status")
                        {
                            string line = Rpcs3UiBridge.StatusLine(p);
                            Console.WriteLine("RPCS3: " + line);
                            p.Log("cli --rpcs3 status -> " + line);
                            break;
                        }
                        if (what == "burst")
                        {
                            var tasks = Enumerable.Range(0, 10)
                                .Select(_ => System.Threading.Tasks.Task.Run(() => Rpcs3UiBridge.EnsureMainWindow(p)))
                                .ToArray();
                            System.Threading.Tasks.Task.WaitAll(tasks);
                            int count = Rpcs3UiBridge.ProjectProcesses(p).Count;
                            Console.WriteLine("BURST_DONE processes=" + count);
                            p.Log("cli --rpcs3 burst -> processes=" + count);
                            break;
                        }
                        var kind = what == "controller" ? Rpcs3SettingsKind.Controller
                                 : what == "rpcn" ? Rpcs3SettingsKind.Rpcn
                                 : what == "general" ? Rpcs3SettingsKind.General
                                 : Rpcs3SettingsKind.MainWindow;
                        var (ok, msg) = Rpcs3UiBridge.OpenSettings(p, kind);
                        string flat = msg.Replace("\r\n", " | ").Replace("\n", " | ");
                        Console.WriteLine((ok ? "OK: " : "FAIL: ") + flat);
                        p.Log("cli --rpcs3 " + what + " -> " + (ok ? "ok" : "fail") + " | " + flat);
                        break;
                    }
                    default:
                        p.Log("cli: unknown argument " + a0);
                        break;
                }
            }
            catch (Exception ex)
            {
                p.Log("cli error: " + ex.Message);
                try { Console.Error.WriteLine(ex.Message); } catch { }
            }
        }
    }
}
