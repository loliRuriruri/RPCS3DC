<#
UI 에셋 생성기 — 소스 PNG(정사각형)에서 icon.ico / hero.png / hero_small.png 를 만든다.
사용: powershell -File make_ui_assets.ps1 -Source <png 경로>
출력: <ROOT>\Assets\*  +  Launcher\src\Assets\*  +  remote-coop\src\Assets\*
#>
param([Parameter(Mandatory=$true)][string]$Source)
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
public static class UiAssets {
    public static void Make(string srcPath, string icoPath, string heroPath, string smallPath) {
        using (var src = new Bitmap(srcPath)) {
            SaveResized(src, 512, heroPath);
            SaveResized(src, 128, smallPath);
            int[] sizes = new int[] { 256, 128, 64, 48, 32, 24, 16 };
            var pngs = new List<byte[]>();
            foreach (int s in sizes) { using (var bmp = Resize(src, s)) using (var ms = new MemoryStream()) { bmp.Save(ms, ImageFormat.Png); pngs.Add(ms.ToArray()); } }
            using (var fs = File.Create(icoPath)) using (var bw = new BinaryWriter(fs)) {
                bw.Write((ushort)0); bw.Write((ushort)1); bw.Write((ushort)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++) {
                    int s = sizes[i];
                    bw.Write((byte)(s >= 256 ? 0 : s)); bw.Write((byte)(s >= 256 ? 0 : s));
                    bw.Write((byte)0); bw.Write((byte)0); bw.Write((ushort)1); bw.Write((ushort)32);
                    bw.Write((uint)pngs[i].Length); bw.Write((uint)offset); offset += pngs[i].Length;
                }
                foreach (var p in pngs) bw.Write(p);
            }
        }
    }
    static Bitmap Resize(Bitmap src, int size) {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp)) {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.DrawImage(src, new Rectangle(0, 0, size, size));
        }
        return bmp;
    }
    static void SaveResized(Bitmap src, int size, string path) { using (var bmp = Resize(src, size)) bmp.Save(path, ImageFormat.Png); }
}
"@ -ReferencedAssemblies System.Drawing
foreach ($d in @("$root\Assets","$root\Launcher\src\Assets","$root\remote-coop\src\Assets")) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
[UiAssets]::Make($Source, "$root\Assets\icon.ico", "$root\Assets\hero.png", "$root\Assets\hero_small.png")
Copy-Item "$root\Assets\icon.ico","$root\Assets\hero.png","$root\Assets\hero_small.png" "$root\Launcher\src\Assets\" -Force
Copy-Item "$root\Assets\icon.ico","$root\Assets\hero.png","$root\Assets\hero_small.png" "$root\remote-coop\src\Assets\" -Force
"UI 에셋 생성 완료: $root\Assets"