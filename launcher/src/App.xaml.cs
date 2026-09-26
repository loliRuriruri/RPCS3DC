using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace DragonCrownProEnhanced
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // make sure the borderless window style is restored even if this app closes early
            Exit += (s, args) =>
            {
                try
                {
                    var p = Project.Discover();
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
            //   DragonCrownProEnhanced.exe --launch PLAY|PROMAX|NETPLAY|CHEAT|KNOWNGOOD
            //   DragonCrownProEnhanced.exe --backup [label]
            //   DragonCrownProEnhanced.exe --restore-window
            //   DragonCrownProEnhanced.exe --status <outfile>
            var args = e.Args ?? Array.Empty<string>();
            if (args.Length > 0)
            {
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
                switch (a0)
                {
                    case "--status":
                    {
                        string outFile = args.Length > 1 ? args[1] : Path.Combine(p.LogDir, "status.txt");
                        Directory.CreateDirectory(p.LogDir);
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
                            "RESHADE=" + p.ReShadeVersion() + " enabled=" + p.ReShadeEnabled + " silent=" + p.ReShadeSilent,
                            "RPCN_CONFIGURED=" + p.RpcnConfigured(),
                            "CHEATS_OR_PATCHES=" + p.CheatsOrPatchesEnabled(),
                            "DISPLAY_MODE=" + p.DisplayMode + " vsync=" + p.VSync,
                            "PROFILES=" + string.Join(",", new[] { "DC_PRO_4K", "DC_PRO_MAX_5K", "DC_NETPLAY", "DC_CHEAT_OFFLINE" }
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
                        var engine = new BorderlessEngine(p, 0);
                        engine.Start();
                        p.Log("cli --restore-window");
                        break;
                    }
                    case "--preset":
                    {
                        string which = args.Length > 1 ? args[1] : "Standard";
                        p.ApplyGraphicsPreset(which.Equals("ProEnhanced", StringComparison.OrdinalIgnoreCase) ? Project.PresetProEnhanced : Project.PresetStandard);
                        p.Log("cli --preset " + which);
                        break;
                    }
                    case "--diag":
                    {
                        string outFile = args.Length > 1 ? args[1] : Path.Combine(p.LogDir, "diagnostics.txt");
                        Directory.CreateDirectory(p.LogDir);
                        var items = p.RunDiagnostics();
                        File.WriteAllLines(outFile, items.Select(i => (i.Ok ? "[OK]  " : "[NG]  ") + i.Name.PadRight(34) + i.Detail + (i.Hint.Length > 0 ? "  -> " + i.Hint : "")));
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
                        bool knownGood = false, borderless = true;
                        switch (what)
                        {
                            case "PROENHANCED":
                                p.ApplyGraphicsPreset(Project.PresetProEnhanced);
                                profile = "Dragons_Crown/DC_PRO_4K";
                                break;
                            case "STANDARD":
                                p.ApplyGraphicsPreset(Project.PresetStandard);
                                profile = "Dragons_Crown/DC_PRO_4K";
                                break;
                            case "PROMAX": profile = "Dragons_Crown/DC_PRO_MAX_5K"; break;
                            case "LOCAL":
                            case "REMOTE": profile = "Dragons_Crown/DC_PRO_4K"; break;
                            case "RPCNSAFE": profile = "Dragons_Crown/NETPLAY_SAFE"; break;
                            case "NETPLAY": profile = "Dragons_Crown/DC_NETPLAY"; break;
                            case "CHEAT":
                                profile = "Dragons_Crown/DC_CHEAT_OFFLINE";
                                p.BackupSaves("cheat_offline");
                                break;
                            case "KNOWNGOOD":
                                profile = "Dragons_Crown/DC_PRO_4K";
                                knownGood = true;
                                break;
                            default: profile = "Dragons_Crown/DC_PRO_4K"; break;
                        }
                        p.ApplyReShadeSilent(p.ReShadeSilent);
                        var proc = p.Launch(profile, knownGood, borderless);
                        Console.WriteLine("PID=" + proc.Id);
                        p.Log("cli --launch " + what + " -> PID " + proc.Id);
                        // keep this process alive so the borderless watcher thread survives until the game ends
                        if (!args.Any(x => x.Equals("--no-wait", StringComparison.OrdinalIgnoreCase)))
                        {
                            proc.WaitForExit();
                            p.Log("cli --launch " + what + " -> game exited (code " + proc.ExitCode + ")");
                        }
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
