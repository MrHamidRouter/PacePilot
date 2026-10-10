# PacePilot 2.0 - Manual Windows acceptance checklist

Automated `PacePilot.exe --selftest` verifies all six live layout variants at three window sizes, two languages, FPS preset buttons, preferences page, profiles page, and profile text patcher.

Actual manual test required on Windows:

1. Start `PacePilot.exe` with RTSS absent. Verify the bilingual consent dialog; cancel installation and confirm nothing was downloaded or installed.
2. Approve RTSS installation via winget; validate a UAC prompt, installation and detection; repeat when already installed to confirm no duplicate install.
3. Launch a compatible, non-anti-cheat game and check that live telemetry is read from RTSS (not target FPS), graph draws values and the applied FPS value is verified by RTSS.
4. Switch between all six themes, English/Persian, fullscreen/maximized/minimum-size; confirm no overlapping interactive controls, no visual clipping, and retained settings.
5. Create and remove a game profile; restart the program and check that per-game limits and the selected theme/language persist.
6. Verify global mode warnings and known protected-game blocks. Do not test injection into prohibited online games.
7. Check keyboard hotkeys, tray menu, Windows startup and undo after disable (0 FPS).
8. Inspect RTSS diagnostics when applying FPS returns a mismatch.

IMPORTANT: A passing GUI self-test does **not** verify real game compatibility, FPS smoothness, Reflex, DLSS FG, or any proprietary frame-pacing engine.
