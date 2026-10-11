# PacePilot Studio

**Precision frame-rate control and live hardware telemetry for Windows 10/11 x64**

[![Windows build](https://github.com/MrHamidRouter/PacePilot/actions/workflows/windows-build.yml/badge.svg)](https://github.com/MrHamidRouter/PacePilot/actions/workflows/windows-build.yml)
[![Latest release](https://img.shields.io/github/v/release/MrHamidRouter/PacePilot?display_name=tag)](https://github.com/MrHamidRouter/PacePilot/releases/latest)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](LICENSE.txt)

**Developer:** Mr.HamidRouter  
**Current version:** 2.6.1  
**Language:** C# · **Desktop UI:** native WinForms + GDI+  
**Required external gaming component:** RivaTuner Statistics Server (RTSS) for FPS limiting and the in-game overlay.

[**Download for Windows (.zip)**](https://github.com/MrHamidRouter/PacePilot/releases/latest/download/PacePilot-Windows-x64.zip) ·
[**Published SHA-256 checksum**](https://github.com/MrHamidRouter/PacePilot/releases/latest/download/PacePilot-Windows-x64.zip.sha256) ·
[**Release history**](https://github.com/MrHamidRouter/PacePilot/releases) ·
[**Build & security transparency**](Docs/SECURITY_AND_RELEASE.md)

---

## What PacePilot does / امکانات برنامه

| Feature | Description |
| --- | --- |
| Frame limiter | RTSS-backed global and per-game FPS caps with verification |
| Frame-time & OSD | Real RTSS frame samples, frame-time trend and Alt+Shift+O overlay toggle; shows N/A without a game hook |
| Games & profiles | Scan supported installed games, assign and save profile limits |
| Hardware telemetry | CPU, GPU, RAM, VRAM, available temperatures and graphics power via bundled LibreHardwareMonitorLib and Windows counters |
| Manual VRM mapping | Select actual published GPU/board sensor IDs; missing channels are never fabricated |
| Hardware identity | CPU/GPU/motherboard/RAM names appear under dashboard cards |
| Visual themes | Six native custom-drawn color systems; theme artwork concepts are **design references**, not screenshots |
| Pre-game RAM check | Inspect RAM usage and optionally trim only PacePilot's own memory |
| Software updates | Compare against official GitHub Releases |

**فارسی:** PacePilot برای محدودسازی فریم بازی با موتور RTSS، نمایش نمودار واقعی Frame Time، پایش دما و مصرف CPU/GPU/RAM/VRAM، اسکن بازی‌ها، انتخاب دستی سنسور VRM و شش تم گرافیکی ساخته شده است. دمای VRM فقط وقتی نمایش داده می‌شود که سنسور واقعی آن وجود داشته و قابل خواندن باشد.

## Safe installation / نصب و اجرای مطمئن

1. Get the official **ZIP** and **.sha256** file from the same [GitHub Release](https://github.com/MrHamidRouter/PacePilot/releases/latest).
2. **Verify the ZIP checksum** using the PowerShell command below *before* extracting or running anything.
3. Extract the **entire ZIP** to a normal folder; keep DLLs next to `PacePilot.exe`.
4. If RTSS is installed, run `PacePilot.exe`. Otherwise run `Setup-PacePilot.bat` and explicitly approve the RTSS package installation. A UAC prompt is expected for the current elevated EXE.

```powershell
$actual = (Get-FileHash ".\PacePilot-Windows-x64.zip" -Algorithm SHA256).Hash.ToLowerInvariant()
$expected = ((Get-Content ".\PacePilot-Windows-x64.zip.sha256" -Raw).Trim() -split '\s+')[0].ToLowerInvariant()
if ($actual -ne $expected) { throw "Checksum mismatch: do NOT execute this file." }
"Integrity verified: $actual"
```

The ZIP contains `INTEGRITY_MANIFEST.json` with SHA-256 hashes of the EXE and DLLs, source commit ID and signing status. The GitHub CI build also creates verifiable **build provenance attestations**. See the [detailed security guide](Docs/SECURITY_AND_RELEASE.md).

### Publisher trust: an important limitation

**The portable executable is unsigned unless the publisher configures a publicly trusted Authenticode certificate in the release pipeline.** It may consequently trigger **Unknown Publisher** or Microsoft Defender SmartScreen. **The size of a compressed WinForms ZIP (even under 1 MB) does not establish whether it is malicious or safe.** Checksums and public source increase auditability but do not prove that a program is malware-free. Never disable Windows security protections just to execute it.

**فارسی:** حجم کم فایل ZIP طبیعی است و به‌تنهایی نشانه ویروس یا سالم‌بودن نیست. برای بررسی اصالت، فایل `.sha256` رسمی را با هش فایل ZIP مقایسه کنید. نسخه فاقد امضای معتبر ممکن است هشدار SmartScreen بدهد؛ غیرفعال‌کردن آنتی‌ویروس یا SmartScreen توصیه نمی‌شود.

## Runtime design

```text
PacePilot Studio (C# WinForms / six native themes)
  ├── Game discovery, per-game profiles and RAM readiness
  ├── RTSS: FPS limiter, on-screen overlay and real frame timing
  ├── Built-in LibreHardwareMonitorLib + Windows GPU/WMI telemetry
  ├── Hardware inventory and optional explicit VRM sensor mapping
  └── GitHub Releases updater + local integrity information
```

The application is **not affiliated** with RTSS, Guru3D, MSI Afterburner, FramePacer or graphics manufacturers. Protected games can restrict external overlays and monitoring hooks. It does **not** bypass anti-cheat or implement NVIDIA Reflex, DLSS Frame Generation, proprietary injection engines or guaranteed reduction in latency.

## Development & tests

The Windows Actions job compiles the real application, runs the bilingual UI and logic self-test, creates a distributable ZIP, records SHA-256 digests and publishes a source-linked artifact attestation. These automated checks **do not establish** hardware sensor compatibility, per-game RTSS hooking or security scan results on every machine.

**Licensing:** MIT for original PacePilot source. Bundled hardware sensor dependencies retain their own licenses; see `Docs/THIRD_PARTY_NOTICES.md` and the package license files.

**Further reading:** [English / فارسی release guidance](Docs/SECURITY_AND_RELEASE.md) · [Development archive](Docs/RELEASE_HISTORY.md) · [PacePilot website](https://mrhamidrouter.github.io/PacePilot/)
