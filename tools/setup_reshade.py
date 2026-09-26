"""Set up ReShade post-processing for the E:\\PS3 RPCS3 environment (reversible)."""
import os
import shutil

RS = r"E:\PS3\Mods_Patches\ReShade"
SLIM = os.path.join(RS, "reshade-shaders", "Shaders")
SWEET = os.path.join(RS, "reshade-shaders-sweetfx")
RPCS3 = r"E:\PS3\RPCS3"

shaders = os.path.join(RS, "Shaders")
textures = os.path.join(RS, "Textures")
presets = os.path.join(RS, "Presets")
for d in (shaders, textures, presets):
    os.makedirs(d, exist_ok=True)

# 1) flat shader folder: only the effects we actually allow + their includes
for f in ("Deband.fx", "ReShade.fxh", "ReShadeUI.fxh", "Macros.fxh", "Blending.fxh", "DrawText.fxh"):
    src = os.path.join(SLIM, f)
    if os.path.exists(src):
        shutil.copyfile(src, os.path.join(shaders, f))
for f in ("CAS.fx", "LumaSharpen.fx", "SMAA.fx", "SMAA.fxh"):
    src = os.path.join(SWEET, "Shaders", "SweetFX", f)
    if os.path.exists(src):
        shutil.copyfile(src, os.path.join(shaders, f))

# 2) textures
for f in ("FontAtlas.png", "lut.png"):
    src = os.path.join(RS, "reshade-shaders", "Textures", f)
    if os.path.exists(src):
        shutil.copyfile(src, os.path.join(textures, f))
for f in ("AreaTex.png", "SearchTex.png"):
    src = os.path.join(SWEET, "Textures", "SweetFX", f)
    if os.path.exists(src):
        shutil.copyfile(src, os.path.join(textures, f))

# 3) ReShade.ini for rpcs3.exe
reshade_ini = f"""[GENERAL]
EffectSearchPaths={shaders}
TextureSearchPaths={textures}
PresetPath={presets}\\DC_POSTFX_MINIMAL.ini
PerformanceMode=1
PreprocessorDefinitions=RESHADE_DEPTH_LINEARIZATION_FAR_PLANE=1000.0,RESHADE_DEPTH_INPUT_IS_UPSIDE_DOWN=0,RESHADE_DEPTH_INPUT_IS_REVERSED=0,RESHADE_DEPTH_INPUT_IS_LOGARITHMIC=0
NoDebugInfo=0
NoEffectCache=0

[INPUT]
KeyOverlay=36,0,0,0
KeyEffects=0,0,0,0
GamepadNavigation=0

[STYLE]
Colour=200,200,200,255

[PROXY]
EnableProxyLibrary=0
ProxyLibrary=
"""
with open(os.path.join(RPCS3, "ReShade.ini"), "w", encoding="utf-8", newline="\r\n") as f:
    f.write(reshade_ini)

# 4) presets
minimal = """PreprocessorDefinitions=RESHADE_DEPTH_LINEARIZATION_FAR_PLANE=1000.0
Techniques=Deband@Deband.fx,ContrastAdaptiveSharpen@CAS.fx
TechniqueSorting=Deband@Deband.fx,ContrastAdaptiveSharpen@CAS.fx

[Deband.fx]
Deband_Radius=16.0
Deband_Threshold=0.006
Deband_Range=16.0

[CAS.fx]
Contrast=0.0
Sharpening=0.30
"""
with_smaa = """PreprocessorDefinitions=RESHADE_DEPTH_LINEARIZATION_FAR_PLANE=1000.0
Techniques=Deband@Deband.fx,ContrastAdaptiveSharpen@CAS.fx,SMAA@SMAA.fx
TechniqueSorting=Deband@Deband.fx,ContrastAdaptiveSharpen@CAS.fx,SMAA@SMAA.fx

[Deband.fx]
Deband_Radius=16.0
Deband_Threshold=0.006
Deband_Range=16.0

[CAS.fx]
Contrast=0.0
Sharpening=0.25

[SMAA.fx]
EdgeDetectionType=1
EdgeDetectionThreshold=0.05
MaxSearchSteps=32
MaxSearchStepsDiagonal=16
CornerRounding=25
PredicationEnabled=0
DebugOutput=0
"""
open(os.path.join(presets, "DC_POSTFX_MINIMAL.ini"), "w", encoding="utf-8", newline="\r\n").write(minimal)
open(os.path.join(presets, "DC_POSTFX_MINIMAL_SMAA.ini"), "w", encoding="utf-8", newline="\r\n").write(with_smaa)

# 5) enable / disable / uninstall scripts
enable = r"""@echo off
rem Enable the ReShade Vulkan layer for RPCS3 (value 1 = enabled).
net session >nul 2>&1 || (echo [ERROR] Administrator rights required. & pause & exit /b 1)
reg add "HKLM\SOFTWARE\Khronos\Vulkan\ImplicitLayers" /v "C:\ProgramData\ReShade\ReShade64.json" /t REG_DWORD /d 1 /f
echo ReShade layer ENABLED (POSTFX profiles will load it).
pause
"""
disable = r"""@echo off
rem Disable the ReShade Vulkan layer without uninstalling anything (value 0 = disabled).
net session >nul 2>&1 || (echo [ERROR] Administrator rights required. & pause & exit /b 1)
reg add "HKLM\SOFTWARE\Khronos\Vulkan\ImplicitLayers" /v "C:\ProgramData\ReShade\ReShade64.json" /t REG_DWORD /d 0 /f
echo ReShade layer DISABLED (CLEAN behaviour restored).
pause
"""
uninstall = r"""@echo off
rem Official ReShade uninstall for RPCS3 (removes layer + module, keeps shader/preset files).
start "" "C:\Users\<user>\Downloads\ReShade_Setup_6.8.0_Addon.exe" --headless --api vulkan --state uninstall "E:\PS3\RPCS3\rpcs3.exe"
echo ReShade uninstall requested. Verify afterwards with: reg query "HKLM\SOFTWARE\Khronos\Vulkan\ImplicitLayers"
pause
"""
open(os.path.join(RS, "ReShade_Enable.cmd"), "w", encoding="utf-8", newline="\r\n").write(enable)
open(os.path.join(RS, "ReShade_Disable.cmd"), "w", encoding="utf-8", newline="\r\n").write(disable)
open(os.path.join(RS, "ReShade_Uninstall.cmd"), "w", encoding="utf-8", newline="\r\n").write(uninstall)

print("shaders:", sorted(os.listdir(shaders)))
print("textures:", sorted(os.listdir(textures)))
print("presets:", sorted(os.listdir(presets)))
print("ReShade.ini ->", os.path.join(RPCS3, "ReShade.ini"))
