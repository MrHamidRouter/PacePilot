# Security Policy

## Supported Releases

The latest [GitHub Release](https://github.com/MrHamidRouter/PacePilot/releases/latest) is the supported distribution. Older releases may contain bugs and are retained for transparency, not recommended for new installations.

## Reporting Vulnerabilities

Please use GitHub's **Private vulnerability reporting** option in the repository Security tab, if enabled. Do not publicly share proof-of-concept exploits, personal game-account data, authentication tokens, or identifiable hardware logs. If private reporting is unavailable, open an issue containing only a non-sensitive high-level summary and request a secure reporting channel.

## Official Distribution & Signatures

Download PacePilot **only from GitHub Releases**. The official ZIP has a separately published SHA-256 checksum and a GitHub build-provenance attestation; see [release verification](../Docs/SECURITY_AND_RELEASE.md).

**Unsigned builds are not guaranteed to pass Microsoft SmartScreen**; never disable system security to install PacePilot. GitHub provenance and checksums do not certify absence of malware.

## Project Boundaries

PacePilot is an independent WinForms frontend to separately installed RTSS. It does not circumvent anti-cheat, modify game integrity checks, install unsigned kernel drivers or suppress Windows UAC/SmartScreen.
