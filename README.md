# PacePilot 1.3 — Windows FPS control

**Developer: Mr.HamidRouter**

PacePilot is an independent Windows GUI for the installed RTSS framerate limiter. RTSS is not bundled. This is not a FramePacer clone or replacement for RTSS.

## Simple Windows setup

1. Install and run RivaTuner Statistics Server (RTSS).
2. Download and extract this project.
3. Double-click **Start-PacePilot.cmd**. It checks prerequisites, compiles `Bin/PacePilot.exe` with the Windows .NET Framework 4.x compiler, executes the built-in GUI self-test and launches the program.
4. Next time, run **Bin/PacePilot.exe** directly.

No extra build tools or Visual Studio are required for the local build; Windows .NET Framework 4.x is required. Use Windows 10/11 x64.

## Ready-made EXE via GitHub Actions

The `windows-build.yml` workflow compiles `PacePilot.exe` on a Windows runner, executes self-test, and provides the executable under the workflow's **Artifacts** section. It does not require the end user to run scripts or compile source.

**Testing limitation:** Windows execution and actual RTSS frame limiting must be validated on Windows. The build workflow tests the GUI only, not in-game limiting.

## Security and support

No RTSS binaries or bypass mechanisms are bundled. Avoid online anti-cheat-protected games. Changes to RTSS profiles can require elevated permissions and are backed up by the program.

## License

MIT — see LICENSE.txt.
