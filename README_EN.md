# PacePilot Studio v2.1

### Administrator launch and FPS verification (v2.1.1)

`PacePilot.exe` has a standard Windows **requireAdministrator** manifest. Windows UAC consent is expected on launch. This build no longer aborts on an early RTSS native-memory mismatch: it proceeds through save and verified profile repair, then checks the profile on disk and via RTSS before claiming success. The target game must support RTSS interception.

**Startup tradeoff:** A requireAdministrator executable cannot guarantee silent elevated sign-in startup; UAC consent may be needed. This application does not bypass UAC or SmartScreen.


**Developer: Mr.HamidRouter**

An original, open-source Windows frontend for RTSS FPS limiting with **six distinct, functional themes** and English/Persian localization.

### Download

Get the `PacePilot-Windows-x64.zip` file from a successful [GitHub Actions Windows build](https://github.com/MrHamidRouter/PacePilot/actions/workflows/windows-build.yml). Extract the ZIP. If RTSS is not installed, run **`Setup-PacePilot.bat`** once: it detects existing RTSS, installs it with `winget` if needed, then starts `PacePilot.exe`. Otherwise run the EXE directly. Administrator confirmation may appear.

```bat
winget install --id Guru3D.RTSS --exact --source winget --silent --accept-package-agreements --accept-source-agreements --disable-interactivity
```

### Features

- Six themes: Neon Teal, Windows Blue, Cyber Purple, Racing Red, Esports Lime and Graphite Gold.
- English and Persian interface; remembered selection.
- Global and per-game FPS caps, 0–1000 FPS; automatic saved-game selection.
- Actual RTSS FPS/frametime samples and a live graph; no synthetic telemetry.
- Hotkeys, system tray, Windows startup, always-on-top preference.
- Consent-driven RTSS installation using `winget` and official download fallback.
- Real configuration diagnostics, safety warnings for protected games and automated UI tests.

### Install RTSS

RTSS is not bundled. If absent, PacePilot asks permission before using `winget` to install `Guru3D.RTSS`. If that fails, it offers Guru3D's official download page. The package installer may require Windows administrator approval.

### Known limits

PacePilot is a standalone RTSS frontend, **not a reproduction of FramePacer's proprietary pacing engine**. NVIDIA Reflex, DLSS Frame Generation/Dynamic MFG, proprietary injection and dedicated controller support are **not implemented**. RTSS-compatible games are necessary for FPS caps and monitoring. Aggressive anti-cheat solutions may prohibit external tools. Windows CI is not a substitute for real hardware/gameplay testing.

**Windows 10/11 x64 · .NET Framework 4.x · MIT · Developer Mr.HamidRouter**

### What's new in 2.1

- Six embedded artwork themes with functional FPS controls and a graphical theme gallery.
- Local Steam, Epic Games and GOG game-library discovery (manual EXE selection remains available).
- Check official GitHub Releases for newer versions at most once per day or on demand. Download is user-controlled.
- RTSS permission fix: if profile storage is protected, a narrowly scoped writer can request Windows UAC approval. A successful Windows CI run does not replace on-device game testing.
- Extract the ZIP and use Setup-PacePilot.bat if RTSS is not installed.

**Developer: Mr.HamidRouter**

## PacePilot 2.2 – in-game RTSS OSD and sensors

- One PacePilot Windows executable includes the existing FPS controls, game discovery, six themed layouts, and a Performance HUD.
- Press **Alt + Shift + O** to toggle the in-game RTSS overlay. It owns an RTSSSharedMemoryV2 OSD slot; it does **not** inject into or evade protections of any game.
- FPS, frametime, and the 20-segment frametime bar are based on actual RTSS shared-memory samples; unavailable readings remain blank.
- CPU and RAM come from native Windows counters. For GPU usage, GPU/CPU temperature, GPU clock, power, VRAM and VRM, run **HWiNFO64 Sensors** with **Shared Memory Support** enabled. Missing hardware readings show **—**.
- Background anti-cheat *services* no longer block unrelated compatible games. **Actual protected titles** (e.g. PUBG/BattlEye, Valorant, Fortnite) remain excluded from PacePilot's external limits and hook-dependent OSD. Use each game's own FPS limiter.
- A green GitHub Windows build and self-test do not establish real-game compatibility; verify FPS application and OSD visibility on the target system.


## PacePilot 2.3 — Premium themes, live frame activity, and RAM readiness

The **existing RTSS limiter, OSD hotkey, game library, saved profiles, and update checks remain in one executable.** The main dashboard now follows the same consistent layout in all **six embedded-artwork themes**; cards, borders and banner accents change to match each theme. These are native Windows controls built from the concepts, **not rasterized screenshots or a promise of pixel-perfect mockup reproduction**.

### Real frametime feed
The animated **Frame Activity** strip and **Frame-Time History** graph are driven by the same actual RTSS per-game frame samples. **FPS limit applied** and **RTSS process running** do not establish that a game-render hook is active. When no valid target-game samples arrive, the graph stays empty and an explicit **Awaiting RTSS game hook** message is shown instead of fabricated motion. Avoid selecting helper executables such as `UnrealCEFSubProcess.exe`; select the real game process. Protected games may not support injected RTSS OSD.

### Safe pre-game memory check
Select **Prepare / Inspect** in the **Pre-game RAM readiness** panel to view physical RAM and optionally trim **PacePilot's own** working set. **Heavy apps** lists visible user applications using significant memory, without force-closing anything. Launch games using your normal Steam/Epic shortcut after the check. PacePilot **does not** flush Windows standby memory, change memory quotas, kill background services, or promise FPS improvements. This button is optional and does not run at Windows startup.

GPU / VRM telemetry still depends on available HWiNFO64 shared-memory sensors. Missing measurements show **—**.


## Version 2.4 — Premium native UI, no mockup wallpaper

Six selectable skins now use native scalable graphics rather than low-resolution concept screenshot crops: gradient sidebar, custom vector hero, rounded metrics, custom buttons and live theme previews. The real-only frametime chart explains when RTSS has no game rendering samples. Existing RTSS limiting, OSD hotkey, game library, CPU/GPU monitoring, safe RAM readiness and GitHub update checks remain intact.

The interface is a native implementation inspired by the approved designs, not a pixel-perfect bitmap conversion. Real-game UI and RTSS hook compatibility should be validated on Windows.

## v2.4.1 — real hardware sensor fallbacks

The six-theme UI is unchanged. PacePilot reads **HWiNFO64 Shared Memory** first. If not available, it asynchronously checks LibreHardwareMonitor/OpenHardwareMonitor WMI providers and Windows GPU utilization/adapter-memory counters. It never invents unavailable readings. Windows counters typically cannot report GPU temperature, VRM temperature, board power or core clocks; those require supported hardware sensors. Some motherboards have no VRM temperature sensor at all.

Click any empty GPU/VRAM/VRM tile for provider diagnostics and setup steps. For HWiNFO64, open Sensors-only and enable **Shared Memory Support** in *main* settings (it may turn off after 12 hours on the free tier). Alternatively run LibreHardwareMonitor.
