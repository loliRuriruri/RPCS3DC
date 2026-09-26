using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DragonCrownRemoteCoop
{
    public sealed class DiagItem
    {
        public string Name;
        public bool Ok;
        public string Detail;
        public string Hint = "";
        public string Code = "";
    }

    /// <summary>
    /// HOST / GUEST diagnostics with failure classification (never a single "Remote Co-op Failed").
    /// Item count is runtime-based; no hardcoded totals.
    /// </summary>
    public static class Diagnostics
    {
        private static DiagItem Item(string name, bool ok, string detail, string hint = "", string code = "")
            => new DiagItem { Name = name, Ok = ok, Detail = detail, Hint = ok ? "" : hint, Code = code };

        public static List<DiagItem> RunHost()
        {
            var list = new List<DiagItem>();
            var s = HostSetup.GetStatus();

            list.Add(Item("Sunshine installed", s.SunshineInstalled,
                s.SunshineInstalled ? (s.SunshineVersion.Length > 0 ? s.SunshineVersion : "detected") : "not installed",
                "HOST SETUP → INSTALL/REPAIR HOST (official MSI)", "SUNSHINE_NOT_INSTALLED"));
            list.Add(Item("Sunshine service", s.ServiceRunning,
                s.ServiceExists ? s.ServiceState : "not found",
                "Sunshine 를 한 번 실행하면 서비스가 등록됩니다. 또는 sc start " + SunshineManager.ServiceName,
                "SUNSHINE_SERVICE_STOPPED"));
            list.Add(Item("Sunshine Web UI", s.WebUiReachable,
                s.WebUiReachable ? "https://localhost:47990" : "unreachable",
                "Sunshine 실행 후 Web UI(47990) 접속 확인", "SUNSHINE_WEBUI_UNAVAILABLE"));

            var b = s.Backend;
            list.Add(Item("Gamepad backend (Dragon's Crown)", s.BackendReadyForDc,
                b.FreeReady
                    ? $"FREE READY: {b.FreeLabel} · gamepad={(s.Policy.Gamepad.Length > 0 ? s.Policy.Gamepad : "auto")}"
                    : b.Label,
                "FREE 경로 권장: ViGEmBus(무료) + gamepad=x360. Virtual HID(유료)는 선택이며 없어도 진행 가능.",
                "GAMEPAD_BACKEND_MISSING"));
            list.Add(Item("ViGEmBus (FREE — recommended)", b.ViGEmBusInstalled,
                b.FreeLabel,
                "HOST SETUP → [INSTALL FREE GAMEPAD DRIVER (ViGEmBus)] — 무료 legacy/EOL 드라이버(Xbox 360/XInput).",
                "VIGEMBUS_MISSING"));
            list.Add(Item("Virtual HID Driver (PREMIUM — optional)", true,
                b.VirtualHidInstalled ? b.PremiumLabel : "Not installed (optional)",
                "유료 라이선스가 필요한 선택 기능입니다. Dragon's Crown Remote 2P 에는 필요하지 않습니다.",
                "VIRTUAL_HID_UNAVAILABLE"));

            list.Add(Item("Controller input", s.Policy.ControllerEnabled,
                s.Policy.ControllerEnabled ? "enabled" : "disabled",
                "sunshine.conf: controller = enabled", "CONTROLLER_INPUT_DISABLED"));
            list.Add(Item("Keyboard / Mouse policy", s.Policy.ControllerOnly,
                s.Policy.Summary,
                "Controller-only Mode 를 켜면 친구가 HOST 키보드/마우스를 조작할 수 없습니다."));
            list.Add(Item("Virtual pad", s.VirtualPadReady || !s.GuestConnected,
                s.VirtualPadState + " (xinput=" + s.XInputCount + ")",
                "Guest 연결 후 [TEST REMOTE PAD] 로 입력을 확인하세요.", "VIRTUAL_PAD_NOT_CREATED"));
            list.Add(Item("Guest connection", s.GuestConnected,
                s.GuestConnected ? "connected" : "waiting",
                "Guest PC 에서 Moonlight 로 접속하면 CONNECTED 로 바뀝니다.", "GUEST_NOT_CONNECTED"));

            list.Add(Item("RPCS3", s.Rpcs3Found, s.Rpcs3Found ? Rpcs3Integration.Rpcs3Exe : "not found",
                "<ROOT>\\RPCS3\\rpcs3.exe 필요"));
            list.Add(Item("DragonCrown launcher", Rpcs3Integration.LauncherFound,
                Rpcs3Integration.LauncherFound ? Rpcs3Integration.LauncherExe : "not found",
                "<ROOT>\\Launcher\\DragonCrownProEnhanced.exe 필요"));
            list.Add(Item("RPCS3 Player 1 (must stay unchanged)", !string.IsNullOrEmpty(s.P1) && s.P1 != "없음",
                s.P1, "Player 1 은 이 도구가 절대 변경하지 않습니다."));
            list.Add(Item("RPCS3 Player 2", Rpcs3Integration.Player2IsXInput, s.P2,
                "HOST SETUP → [SET P2 TO XINPUT] (백업 후 적용)", "RPCS3_P2_NOT_CONFIGURED"));
            list.Add(Item("Player 2 input", Rpcs3Integration.Player2IsXInput && (s.VirtualPadReady || !s.GuestConnected),
                Rpcs3Integration.Player2IsXInput ? "XInput" : "not XInput",
                "Guest 연결 + TEST REMOTE PAD 로 실제 입력을 확인하세요.", "RPCS3_P2_NO_INPUT"));

            AppEnv.Log($"diagnostics host: {list.Count(i => i.Ok)}/{list.Count} ok");
            return list;
        }

        public static List<DiagItem> RunGuest()
        {
            var list = new List<DiagItem>();
            var s = GuestSetup.GetStatus();

            list.Add(Item("Moonlight installed", s.MoonlightInstalled,
                s.MoonlightInstalled ? (s.MoonlightVersion.Length > 0 ? s.MoonlightVersion : "detected") : "not installed",
                "GUEST SETUP → INSTALL MOONLIGHT (official installer)", "MOONLIGHT_NOT_INSTALLED"));
            list.Add(Item("Controller detected", s.ControllerDetected,
                s.ControllerState + (s.ControllerNames.Length > 0 ? " : " + string.Join(" | ", s.ControllerNames.Take(3)) : ""),
                "USB/블루투스 패드 연결 후 [TEST CONTROLLER] (joy.cpl 확인 가능)", "GUEST_CONTROLLER_MISSING"));
            list.Add(Item("Host address", s.HostConfigured,
                s.HostConfigured ? s.HostAddress : "not set",
                "HOST PC 의 LAN IP 또는 hostname 입력 (자동 탐색 실패 시)", "HOST_NOT_PAIRED"));
            list.Add(Item("Pairing status", true, s.PairingStatus,
                "Moonlight GUI 에서 PIN 확인 → HOST Sunshine Web UI 에서 승인 (PIN 은 저장하지 않습니다)"));
            list.Add(Item("Streaming capability", s.StreamingCapable,
                s.StreamingCapable ? "Moonlight + controller ready" : "Moonlight and/or controller missing",
                "Moonlight 설치 + 패드 연결이 필요합니다."));

            AppEnv.Log($"diagnostics guest: {list.Count(i => i.Ok)}/{list.Count} ok");
            return list;
        }
    }
}
