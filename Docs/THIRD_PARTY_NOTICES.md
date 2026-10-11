# Third-party components and licenses

PacePilot is developed by **Mr.HamidRouter**. The application bundles hardware-monitoring libraries to operate **without installing or running any hardware monitoring utility**.

## LibreHardwareMonitorLib — v0.9.4
- Upstream: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor
- Package: https://www.nuget.org/packages/LibreHardwareMonitorLib/0.9.4
- License: Mozilla Public License 2.0 (**MPL-2.0**)
- License text: https://www.mozilla.org/en-US/MPL/2.0/
- Source code: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor
- The library and transitive dependencies are shipped as unmodified assemblies in the portable ZIP; the application reads sensors but does not modify fan curves or voltages.

## HidSharp and System.Management dependencies
Runtime dependencies for the bundled sensor package are restored by NuGet and distributed with PacePilot. Their corresponding licenses are available in their NuGet packages and should be respected for any redistribution. The CI build checks for their presence.

## RTSS
RivaTuner Statistics Server (RTSS) is **not bundled** and remains an external dependency for FPS limiting and on-screen display. The project neither owns nor changes RTSS licensing.

## Security and hardware coverage
Hardware telemetry is best-effort and read-only. Some motherboard/VRM sensors are not exposed to applications. On Windows, sensor drivers may be restricted by security settings. Do not disable Windows security or anti-cheat protection for sensor access.
