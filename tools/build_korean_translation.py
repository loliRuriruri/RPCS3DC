"""Build a partial Korean translation (rpcs3_ko.qm) for RPCS3 0.0.42-20053.

Only strings that actually exist inside rpcs3.exe are included, so nothing is
invented. Qt's QTranslator falls back to the empty context, therefore all
messages are stored in one context-less block and match regardless of the
original tr() context.
"""
import os
import re
import subprocess
import sys

EXE = r"E:\PS3\RPCS3\rpcs3.exe"
TS = r"E:\PS3\Mods_Patches\RPCS3_Translation\rpcs3_ko.ts"
QM_OUT = r"E:\PS3\Mods_Patches\RPCS3_Translation\rpcs3_ko.qm"
QM_DEST = r"E:\PS3\RPCS3\qt6\translations\rpcs3_ko.qm"
LRELEASE = os.path.join(os.path.dirname(sys.executable), "Scripts", "pyside6-lrelease.exe")

# ---------------------------------------------------------------- translations
KO = {
    # ---- main menu / toolbar ------------------------------------------------
    "File": "파일",
    "Emulation": "에뮬레이션",
    "View": "보기",
    "Configuration": "설정",
    "Help": "도움말",
    "Manage": "관리",
    "Devices": "장치",
    "Recording": "녹화",
    "Game Collections": "게임 모음",
    "Tools": "도구",
    "Show Tool Bar": "툴바 표시",
    "Show Game List": "게임 목록 표시",
    "Show Debugger": "디버거 표시",
    "Show Log/TTY": "로그/TTY 표시",
    "Show Title Bars": "제목 표시줄 표시",
    "Show Hidden Entries": "숨긴 항목 표시",
    "Show Completed Entries": "완료 항목 표시",
    "Show Broken Entries": "손상 항목 표시",
    "Support Us": "후원하기",
    "About Qt": "Qt 정보",
    "About": "정보",
    "Exit RPCS3": "RPCS3 종료",
    "Exit the application": "응용 프로그램 종료",
    "Boot a game": "게임 부팅",
    "Add Games": "게임 추가",
    "Add ISO Games": "ISO 게임 추가",
    "Refresh Game List": "게임 목록 새로 고침",
    "Refresh gamelist": "게임 목록 새로 고침",
    "Switch to grid mode": "그리드 모드로 전환",
    "Switch to list mode": "목록 모드로 전환",
    "Grid": "그리드",
    "List": "목록",
    "Full Screen": "전체 화면",
    "Toggle fullscreen": "전체 화면 전환",
    "Save Data": "세이브 데이터",
    "Manage save data": "세이브 데이터 관리",
    "Manage trophies": "트로피 관리",
    "User Accounts": "사용자 계정",
    "Manage user accounts": "사용자 계정 관리",
    "Memory Viewer": "메모리 뷰어",
    "RSX Debugger": "RSX 디버거",
    "Kernel Explorer": "커널 탐색기",
    "Decrypt PS3 Binaries": "PS3 바이너리 복호화",
    "Extract PUP": "PUP 추출",
    "Extract MSELF": "MSELF 추출",
    "Extract Encrypted TAR": "암호화 TAR 추출",
    "Download Compatibility Database": "호환성 데이터베이스 다운로드",
    "Download Config Database": "설정 데이터베이스 다운로드",
    "Download Integrity Database": "무결성 데이터베이스 다운로드",
    "Check Integrity": "무결성 검사",
    "Check for Updates": "업데이트 확인",
    "Show Game Compatibility": "게임 호환성 보기",
    "Insert Disc": "디스크 삽입",
    "Eject Disc": "디스크 꺼내기",
    "Log Viewer": "로그 뷰어",
    "Game Patches": "게임 패치",
    "Cheats": "치트",
    "Game Info": "게임 정보",
    "Disk Usage": "디스크 사용량",
    "Clean up Game List": "게임 목록 정리",
    "System Commands": "시스템 명령",
    "Emulated Devices": "에뮬레이트 장치",
    "Cameras": "카메라",
    "Mice": "마우스",
    "Configuration: %0": "설정: %0",
    # ---- game list context menu --------------------------------------------
    "Boot": "부팅",
    "Reboot": "재부팅",
    "Boot with Custom Configuration": "커스텀 설정으로 부팅",
    "Boot with Global Configuration": "전역 설정으로 부팅",
    "Boot with Default Configuration": "기본 설정으로 부팅",
    "Boot with Manually Selected Configuration": "직접 선택한 설정으로 부팅",
    "Boot with last SaveState": "마지막 세이브스테이트로 부팅",
    "Change Custom Configuration": "커스텀 설정 변경",
    "Create Custom Configuration From Global Settings": "전역 설정에서 커스텀 설정 만들기",
    "Create Custom Configuration From Default Settings": "기본 설정에서 커스텀 설정 만들기",
    "Create Custom Configuration From Database Settings": "DB 설정에서 커스텀 설정 만들기",
    "Create Custom Gamepad Configuration": "커스텀 게임패드 설정 만들기",
    "Change Custom Gamepad Configuration": "커스텀 게임패드 설정 변경",
    "Compare Configurations": "설정 비교",
    "Manage Game Patches": "게임 패치 관리",
    "Create LLVM Cache": "LLVM 캐시 생성",
    "Remove": "제거",
    "Remove Custom Configuration": "커스텀 설정 제거",
    "Remove Custom Gamepad Configuration": "커스텀 게임패드 설정 제거",
    "Remove Shader Cache": "셰이더 캐시 제거",
    "Remove PPU Cache": "PPU 캐시 제거",
    "Remove SPU Cache": "SPU 캐시 제거",
    "Remove HDD1 Cache": "HDD1 캐시 제거",
    "Remove All Caches": "모든 캐시 제거",
    "Remove Savestates": "세이브스테이트 제거",
    "Remove Patch": "패치 제거",
    "Remove Patch?": "패치를 제거할까요?",
    "Show Patch File": "패치 파일 보기",
    "Manage Game": "게임 관리",
    "Create Desktop Shortcut": "바탕 화면 바로가기 만들기",
    "Create Start Menu Shortcut": "시작 메뉴 바로가기 만들기",
    "Hide Game In Game List": "게임 목록에서 숨기기",
    "Rename In Game List": "게임 목록에서 이름 변경",
    "Edit Tooltip Notes": "툴팁 메모 편집",
    "Reset Time Played": "플레이 시간 초기화",
    "Custom Images": "사용자 이미지",
    "Import Custom Icon": "사용자 아이콘 가져오기",
    "Replace Custom Icon": "사용자 아이콘 교체",
    "Remove Custom Icon": "사용자 아이콘 제거",
    "Open Folder": "폴더 열기",
    "Open Disc Game Folder": "디스크 게임 폴더 열기",
    "Open Custom Config Folder": "커스텀 설정 폴더 열기",
    "Open Cache Folder": "캐시 폴더 열기",
    "Copy Info": "정보 복사",
    "Copy Name + Serial": "이름 + 시리얼 복사",
    "Copy Name": "이름 복사",
    "Copy Serial": "시리얼 복사",
    "Check ISO Integrity": "ISO 무결성 검사",
    "Check Game Compatibility": "게임 호환성 확인",
    # ---- settings dialog ---------------------------------------------------
    "Settings": "설정",
    "Settings: [%0] %1": "설정: [%0] %1",
    "Save custom configuration": "커스텀 설정 저장",
    "CPU": "CPU",
    "GPU": "GPU",
    "Audio": "오디오",
    "I/O": "입출력",
    "System": "시스템",
    "Network": "네트워크",
    "Advanced": "고급",
    "Debug": "디버그",
    "Emulator": "에뮬레이터",
    "Graphics": "그래픽",
    "Input/Output": "입출력",
    "Apply": "적용",
    "Restore Defaults": "기본값 복원",
    "Close": "닫기",
    "Save": "저장",
    "Renderer": "렌더러",
    "Graphics Device": "그래픽 장치",
    "Default Resolution": "기본 해상도",
    "Aspect Ratio": "화면 비율",
    "Frame Limit": "프레임 제한",
    "Anti-Aliasing": "안티앨리어싱",
    "Anisotropic Filter": "비등방성 필터",
    "Shader Precision": "셰이더 정밀도",
    "Shader Mode": "셰이더 모드",
    "Shader Compiler Threads": "셰이더 컴파일 스레드",
    "Write Color Buffers": "컬러 버퍼 쓰기",
    "Write Depth Buffer": "깊이 버퍼 쓰기",
    "Read Color Buffers": "컬러 버퍼 읽기",
    "Read Depth Buffer": "깊이 버퍼 읽기",
    "Strict Rendering Mode": "엄격 렌더링 모드",
    "Resolution Scale": "해상도 배율",
    "Minimum Scalable Dimension": "최소 확장 크기",
    "Output Scaling Mode": "출력 스케일링 모드",
    "Texture LOD Bias": "텍스처 LOD 바이어스",
    "VSync": "수직 동기화",
    "Multithreaded RSX": "멀티스레드 RSX",
    "Asynchronous Texture Streaming": "비동기 텍스처 스트리밍",
    "Resolution": "해상도",
    "720p (Recommended)": "720p (권장)",
    "PS3 native": "PS3 네이티브",
    "FidelityFX Super Resolution": "FidelityFX Super Resolution",
    "Bilinear": "양선형",
    "Nearest": "최근접",
    "Precise (Slowest)": "정확 (가장 느림)",
    "Approximate (Fast)": "근사 (빠름)",
    "Relaxed (Fastest)": "완화 (가장 빠름)",
    "Language": "언어",
    "Console Language": "콘솔 언어",
    "License Area": "라이선스 지역",
    "Keyboard Type": "키보드 종류",
    "Date Format": "날짜 형식",
    "Time Format": "시간 형식",
    "Enter button assignment": "확인 버튼 할당",
    "Performance Overlay": "성능 오버레이",
    "Log Level": "로그 수준",
    "Enabled": "사용",
    "Disabled": "사용 안 함",
    "Legacy Recompiler": "레거시 리컴파일러",
    "Async Recompiler": "비동기 리컴파일러",
    "Async Recompiler with Shader Interpreter": "셰이더 인터프리터 포함 비동기 리컴파일러",
    "Interpreter Only": "인터프리터 전용",
    "SPU Decoder": "SPU 디코더",
    "PPU Decoder": "PPU 디코더",
    "Preferred SPU Threads": "선호 SPU 스레드",
    "SPU Block Size": "SPU 블록 크기",
    "Thread Scheduler": "스레드 스케줄러",
    "Accurate SPU Reservations": "정확한 SPU 예약",
    "Disable SPU GETLLAR Spin Optimization": "SPU GETLLAR 스핀 최적화 비활성화",
    "SPU loop detection": "SPU 루프 감지",
    "Enable SPU loop detection": "SPU 루프 감지 사용",
    "Start games in fullscreen mode": "게임을 전체 화면으로 시작",
    "Exit RPCS3 when process finishes": "프로세스 종료 시 RPCS3 종료",
    "Show trophy popups": "트로피 팝업 표시",
    "Prevent display sleep while running games": "게임 실행 중 화면 절전 방지",
    "Use native user interface": "네이티브 사용자 인터페이스 사용",
    "Use recursive scan": "하위 폴더 검색 사용",
    "Play music during boot sequence": "부팅 시퀀스에서 음악 재생",
    "Pause emulation on RPCS3 focus loss": "RPCS3 포커스 손실 시 에뮬레이션 일시정지",
    "Show shader compilation hint": "셰이더 컴파일 안내 표시",
    "Show PPU compilation hint": "PPU 컴파일 안내 표시",
    "Master Volume": "마스터 볼륨",
    "Audio Device": "오디오 장치",
    "Audio Buffer Duration": "오디오 버퍼 지속 시간",
    "Enable Buffering": "버퍼링 사용",
    "Enable Time Stretching": "시간 늘리기 사용",
    "Convert to 16 bit": "16비트로 변환",
    "Dump to file": "파일로 덤프",
    "Keyboard Handler": "키보드 핸들러",
    "Mouse Handler": "마우스 핸들러",
    "Camera": "카메라",
    "Pad handler mode": "패드 핸들러 모드",
    "Background input enabled": "백그라운드 입력 사용",
    "Show move cursor": "Move 커서 표시",
    "Internet enabled": "인터넷 사용",
    "PSN status": "PSN 상태",
    "DNS address": "DNS 주소",
    "Enable UPNP": "UPNP 사용",
    "Bind address": "바인드 주소",
    "Log": "로그",
    "TTY": "TTY",
    "Clear": "지우기",
    "Copy": "복사",
    "Search": "검색",
    # ---- messages / misc ---------------------------------------------------
    "Warning": "경고",
    "Error": "오류",
    "Success": "성공",
    "Failure": "실패",
    "Information": "정보",
    "Unknown error": "알 수 없는 오류",
    "Boot Failed": "부팅 실패",
    "Boot failed": "부팅 실패",
    "Booting failed: %1 %2": "부팅 실패: %1 %2",
    "Confirmation": "확인",
    "Confirm Removal": "제거 확인",
    "Confirm Reset": "초기화 확인",
    "Confirm Creation": "생성 확인",
    "Confirm Hiding": "숨기기 확인",
    "Are you sure?": "계속할까요?",
    "Compiling PPU modules": "PPU 모듈 컴파일 중",
    "Compiling shaders": "셰이더 컴파일 중",
    "Emulation is running": "에뮬레이션이 실행 중입니다",
    "No patches found for the specified version": "지정한 버전에 대한 패치가 없습니다",
    "Patch downloader": "패치 다운로더",
    "Download successful": "다운로드 성공",
    "Validation failed": "검증 실패",
    "Import successful": "가져오기 성공",
    "Import failed": "가져오기 실패",
    "Nothing to import": "가져올 항목 없음",
    "Your patch file is already up to date.": "패치 파일이 이미 최신입니다.",
    "Your patch file is now up to date": "패치 파일이 최신으로 갱신되었습니다",
    "Update patches?": "패치를 업데이트할까요?",
    "New patches are available.": "새 패치가 있습니다.",
    "Screenshots": "스크린샷",
    "Recordings": "녹화",
    "Captures": "캡처",
    "Trophy": "트로피",
    "Move": "이동",
    "Playtime": "플레이 시간",
    "Last Play": "마지막 플레이",
    "Compatibility": "호환성",
    "Category": "카테고리",
    "Version": "버전",
    "Firmware": "펌웨어",
    "Name": "이름",
    "Serial": "시리얼",
    "Icon": "아이콘",
    "Path": "경로",
    "Sound": "사운드",
    "Parental": "연령",
    "Dir Size": "폴더 크기",
    "Playable": "플레이 가능",
    "Ingame": "인게임",
    "Intro": "인트로",
    "Loadable": "로드 가능",
    "Nothing": "실행 불가",
    "Unknown": "알 수 없음",
}

# ------------------------------------------------------- build .ts + compile
data = open(EXE, "rb").read()
runs = {m.group(0).decode("ascii") for m in re.finditer(rb"[\x20-\x7E]{3,120}", data)}

used, skipped = {}, []
for src, dst in KO.items():
    if src in runs:
        used[src] = dst
    elif ("&" + src) in runs:
        # RPCS3 stores menu entries with a mnemonic ampersand (e.g. "&Boot")
        used["&" + src] = dst
    else:
        skipped.append(src)

os.makedirs(os.path.dirname(TS), exist_ok=True)
with open(TS, "w", encoding="utf-8", newline="\n") as f:
    f.write('<?xml version="1.0" encoding="utf-8"?>\n<!DOCTYPE TS>\n')
    f.write('<TS version="2.1" language="ko_KR" sourcelanguage="en_US">\n')
    f.write('    <context>\n        <name></name>\n')
    for src, dst in used.items():
        esc = lambda s: s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
        f.write("        <message>\n")
        f.write(f"            <source>{esc(src)}</source>\n")
        f.write(f"            <translation>{esc(dst)}</translation>\n")
        f.write("        </message>\n")
    f.write("    </context>\n</TS>\n")

print(f"strings in exe   : {len(runs)}")
print(f"translated (used): {len(used)}")
print(f"skipped (not in exe): {len(skipped)}")
if skipped:
    print("  e.g.:", skipped[:12])
print("ts ->", TS)

if os.path.exists(LRELEASE):
    r = subprocess.run([LRELEASE, TS, "-qm", QM_OUT], capture_output=True, text=True)
    print("lrelease rc:", r.returncode)
    print((r.stdout or "").strip()[-500:])
    print((r.stderr or "").strip()[-500:])
    if os.path.exists(QM_OUT):
        os.makedirs(os.path.dirname(QM_DEST), exist_ok=True)
        import shutil
        shutil.copyfile(QM_OUT, QM_DEST)
        print("installed ->", QM_DEST, os.path.getsize(QM_DEST), "bytes")
else:
    print("lrelease not found:", LRELEASE)
