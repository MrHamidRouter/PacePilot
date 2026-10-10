# 🇬🇧 PacePilot v1.4 — English guide

**Developer: Mr.HamidRouter**

## One EXE for Windows

Open the latest successful **Windows EXE build and self-test** on [GitHub Actions](https://github.com/MrHamidRouter/PacePilot/actions/workflows/windows-build.yml), download artifact **PacePilot-Windows-x64-v1.4**, extract it and run **PacePilot.exe**. End users do not need Visual Studio or CMD files. If building locally from source, run `Start-PacePilot.cmd` once; it generates `Bin\PacePilot.exe` and executes the UI test.

## Automatic RTSS download / installation

On launch, PacePilot checks for an existing RivaTuner Statistics Server (RTSS) installation. When missing, it **asks permission** to install package **`Guru3D.RTSS`** via Microsoft's `winget`. Accept Windows administrator approval if prompted. The app rechecks the install and starts RTSS afterward. Existing RTSS installations are reused and not reinstalled.

If `winget` is unavailable, the user declines, or installation/network verification fails, PacePilot offers the [official Guru3D RTSS download](https://www.guru3d.com/download/rtss-rivatuner-statistics-server-download/) and allows manual selection of an installed RTSS folder. No RTSS installers are bundled or redistributed; package hash checks are never disabled. Installation needs connectivity, working winget and possibly administrator approval.

## English and Persian UI

Switch between `EN` and `فا` in the sidebar. The language changes immediately and persists. Buttons, statuses, safety warnings and tray menu are translated. Low-level RTSS diagnostic logs stay in English.

## Features

Real RTSS FPS cap (0 = unlimited), preset FPS buttons, live FPS/frametime display, chart, global/per-game profiles, foreground-game auto-selection, system-wide hotkeys, tray mode, Windows startup and RTSS diagnostic logging with backup for profile fallbacks.

**System:** Windows 10/11 x64 + .NET Framework 4.x. Do not assume third-party limiters are compatible with protected online games.

**Testing:** The GitHub Actions self-test checks bilingual UI, layout, FPS selection and profile-file handling. It does **not** validate an RTSS installation or actual gameplay.

**License:** MIT (PacePilot source). RTSS is third-party software with separate terms.
