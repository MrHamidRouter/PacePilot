# PacePilot Studio — Download security & release verification

**Developer:** Mr.HamidRouter  
**Source repository:** https://github.com/MrHamidRouter/PacePilot  
**Release:** https://github.com/MrHamidRouter/PacePilot/releases/latest  
**Operating system:** Windows 10/11 x64  
**Runtime:** Native C# WinForms on the installed Windows .NET Framework; a compact ZIP is normal.  
**Only separately installed gameplay dependency:** RivaTuner Statistics Server (RTSS). The hardware sensor libraries are included in the ZIP.

## Publisher signing and Windows prompts

PacePilot is **unsigned until the developer supplies a real publicly trusted Authenticode code-signing certificate** through GitHub Actions secrets. A build without it **may trigger Microsoft Defender SmartScreen (Unknown Publisher)** and will request an administrator UAC approval due to its RTSS profile / hardware sensor functionality. Neither a large ZIP, source availability, an SHA-256 checksum, GitHub build provenance, nor a self-signed certificate makes an executable malware-free or automatically removes SmartScreen warnings.

For professional Windows distribution, configure a verified publisher certificate (for example, Microsoft's Artifact Signing service) and sign each released EXE. This repository supports optional Authenticode signing via the two protected CI secrets `PACEPILOT_SIGNING_PFX_BASE64` and `PACEPILOT_SIGNING_PASSWORD`. If these are absent, the workflow explicitly reports UNSIGNED. A valid signature still cannot promise immediate SmartScreen reputation.

**Never** disable Microsoft Defender, SmartScreen, Secure Boot, or other Windows security features to install PacePilot.

## Verify the downloaded ZIP before running anything

1. Download both `PacePilot-Windows-x64.zip` and `PacePilot-Windows-x64.zip.sha256` **from the same official GitHub Release**.
2. In PowerShell, in the folder with both files, run:

```powershell
$actual = (Get-FileHash ".\PacePilot-Windows-x64.zip" -Algorithm SHA256).Hash.ToLowerInvariant()
$expected = ((Get-Content ".\PacePilot-Windows-x64.zip.sha256" -Raw).Trim() -split '\s+')[0].ToLowerInvariant()
if ($actual -ne $expected) { throw "PacePilot ZIP checksum mismatch. Do not run the file." }
"Checksum matches the official release: $actual"
```

3. Extract the ZIP. `INTEGRITY_MANIFEST.json` lists **each packaged file's hash and size** plus the source commit and signature status.
4. You can independently inspect the repository source and the Windows build workflow. Confirm the release tag and the commit in the manifest.
5. Optionally verify the cryptographically signed GitHub build attestation (repository and source commit provenance) using the GitHub CLI:

```powershell
gh attestation verify ".\PacePilot-Windows-x64.zip" --repo MrHamidRouter/PacePilot
```

**Important:** Checksums identify unexpected file changes *relative to the trusted GitHub release*. A compromised publisher account can replace both ZIP and checksum. GitHub build provenance documents where an artifact was built; it does **not** provide antivirus certification. Keep your security software enabled and inspect any warning.

## Verify EXE publisher signature (if a signed release becomes available)

```powershell
Get-AuthenticodeSignature ".\PacePilot.exe" |
    Select-Object Status,SignerCertificate,TimeStamperCertificate
```

`NotSigned` means no publisher certificate was included; this is **not** equivalent to `Valid`. Signing must be performed with a genuine verified code-signing identity and independently trusted certificate chain.

## What is inside the ZIP?

- `PacePilot.exe`: WinForms control panel and RTSS integration.
- `LibreHardwareMonitorLib.dll` and required managed dependencies (for internal CPU/GPU/VRM sensor discovery).
- `Setup-PacePilot.bat`: optional setup with explicit user consent before RTSS installation through Windows `winget`.
- `README_FA.md`, `README_EN.md`, third-party notices and license.
- `INTEGRITY_MANIFEST.json` with per-file hashes.

PacePilot does not bypass game anti-cheat mechanisms. RTSS overlays and FPS limits cannot be guaranteed in protected games.

## Build and test scope

The GitHub Actions Windows job compiles the actual C# source, runs the UI/self-test suite, creates the ZIP and checksums, and attaches a signed build-provenance attestation. Those checks establish build consistency only. Hardware sensor accuracy, RTSS game compatibility, security-engine verdicts, and UI appearance require separate real-machine tests.

Report potential vulnerabilities privately to the repository maintainer. Do not post personal or sensitive hardware logs in public issues.
