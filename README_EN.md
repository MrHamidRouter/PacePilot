# PacePilot v1.2 — Windows x64

Build with `Run-PacePilot.cmd` (native .NET Framework 4.x Windows C# compiler required). Each build produces a **uniquely named executable** in `Bin` to avoid CS0016 when an earlier version is running. To launch a newer version, fully exit earlier PacePilot via its notification-area menu (the X button may minimize to tray).

RTSS is separately required. This version checks FPS settings in native memory, after profile save, and on disk. If the API reports an unpersisted value, it may atomically update the `[Framerate]` keys of an existing RTSS profile after creating a backup under `%APPDATA%/PacePilot/RTSS-Profile-Backups`. It reports success only when RTSS's native API reads back the requested FPS.

`Test-UI.cmd`: UI and profile-patching regression tests, no RTSS changes. `Test-RTSS.cmd`: read-only RTSS diagnostics. `Copy RTSS diagnostics` in app: copies details to clipboard. Detailed action traces are saved at `%APPDATA%/PacePilot/Logs/RTSS-Actions.log`.

**Not compiled or tested against RTSS on a Windows machine here**. Verify FPS behavior in a non-protected offline game, not just in the UI. Does not provide FramePacer's proprietary engine, Reflex, DLSS FG, or anti-cheat bypass.
