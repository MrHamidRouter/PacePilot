# PacePilot Studio v2.0

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
