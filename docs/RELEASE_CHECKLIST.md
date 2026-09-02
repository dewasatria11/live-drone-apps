# Release Checklist

Checklist ini bersifat machine-enforced oleh `eng/assert-release-version.ps1`.
Release stabil `vX.Y.Z` ditolak bila satu saja kotak belum selesai. Prerelease
alpha/beta/RC tetap harus melewati Windows build dan test gate.

## Phase 0 — Repository baseline

- [x] Solution boundaries, central package management, analyzers, dan docs tersedia.
- [x] Core dependency direction diuji.

## Phase 1 — Media vertical slice

- [x] MediaMTX dipin dan checksum Windows/Darwin tercatat.
- [x] RTMP → RTSP, reconnect, recovery, dan cleanup lulus pada macOS test host.
- [ ] MediaMTX Windows x64 dan orphan cleanup lulus pada Windows CI.

## Phase 2 — Domain dan Windows boundaries

- [x] Six-slot model, key generation, settings, URL, scoring, dan state machine diuji.
- [ ] WPF, DPAPI, Job Object, Windows network, port, dan firewall test lulus di CI.
- [ ] Phase 2 diverifikasi pada laptop Windows 10/11 asli.

## Phase 3 — WPF operator UI

- [ ] Dashboard, setup wizard shell, settings, tray, dan safe-close selesai.
- [ ] UI memakai service nyata tanpa production mock.
- [ ] Visual QA 100%, 125%, dan 150% selesai.

## Phase 4 — Portal, firewall, dan diagnostics

- [ ] Portal/QR/token lifecycle dan security headers selesai.
- [ ] Firewall repair/UAC/public-profile recovery diuji.
- [ ] Diagnostics dan redacted support bundle selesai.

## Phase 5 — Preview dan vMix

- [ ] Real WebRTC preview dan failure independence selesai.
- [ ] RTSP probe dan panduan vMix selesai.
- [ ] vMix aktual membaca output pada laptop operator.

## Phase 6 — Hardening dan release

- [ ] Installer fresh install, upgrade, dan uninstall diuji pada Windows 10/11.
- [ ] Soak test satu stream minimal dua jam lulus.
- [ ] Enam synthetic publisher lulus.
- [ ] Threat model, test plan, troubleshooting, dan operator guide final selesai.
- [ ] Hardware DJI mempunyai `docs/HARDWARE_VALIDATION.md` dengan `Status: Clean`.
- [ ] Tidak ada defect critical/high, secret, credential, atau orphan process.
- [ ] Release artifact, SBOM/dependency list, notices, dan checksum lengkap.

## Signing

- [x] Release notes selalu memperingatkan bahwa build belum ditandatangani sampai
      certificate resmi milik pemilik tersedia.
