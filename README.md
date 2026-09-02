# Al Ikhsan Media (Drone Version)

Local-first Windows receiver that accepts direct RTMP from DJI Fly and exposes the
same media through loopback RTSP for vMix. The v1.0 path is remux-only: application
code does not capture a screen, decode/re-encode frames, or add overlays.

## Download

Build Windows yang sudah diterbitkan tersedia di halaman
[GitHub Releases](https://github.com/dewasatria11/live-drone-apps/releases/latest).
Selama gate Phase 0–6 belum lengkap, gunakan hanya release bertanda
Alpha/Beta/RC untuk pengembangan dan validasi. Jangan menganggap prerelease siap
untuk acara produksi.

Setelah mengunduh, cocokkan installer dengan `SHA256SUMS.txt`. Build yang belum
ditandatangani dapat memunculkan peringatan Windows SmartScreen.

## Phase 0–1 architecture

The .NET 8 WPF composition root depends on platform-neutral Core contracts,
MediaMTX Infrastructure, and the isolated Setup Portal assembly. MediaMTX v1.20.1
is a pinned, checksum-verified owned child process. See
`docs/ARCHITECTURE.md`, `docs/IMPLEMENTATION_PLAN.md`, and `docs/DECISIONS.md`.

## Build and test

macOS development validates only portable boundaries and the Darwin test artifact;
it does not retarget the WPF application:

```bash
dotnet restore AlIkhsanMedia.Drone.Mac.slnf
dotnet build AlIkhsanMedia.Drone.Mac.slnf --no-restore
dotnet test AlIkhsanMedia.Drone.Mac.slnf --no-build --filter 'Category!=Windows'
```

The authoritative Windows build is:

```powershell
./eng/fetch-mediamtx.ps1 -RuntimeIdentifier win-x64
./eng/verify-vendor.ps1 -RuntimeIdentifier win-x64
dotnet restore AlIkhsanMedia.Drone.sln
dotnet build AlIkhsanMedia.Drone.sln --no-restore
dotnet test AlIkhsanMedia.Drone.sln --no-build
```

`.github/workflows/ci.yml` harus lulus sebelum perilaku Windows dinyatakan
tervalidasi; release dan packaging berada terpisah di
`.github/workflows/release.yml`.

## Version dan release

- CI berjalan pada push `main` dan pull request tanpa membuat GitHub Release.
- Release hanya dipicu oleh tag `v*.*.*` atau dispatch manual dengan versi valid.
- Selama development gunakan `v0.1.0-alpha.1`, `v0.2.0-beta.1`, atau
  `v1.0.0-rc.1`; semuanya otomatis ditandai pre-release.
- Release stabil ditolak selama checklist Phase 0–6 atau hardware validation
  masih belum lengkap.

Integration tests require pinned FFmpeg/FFprobe test tooling. FFmpeg is not a
production or installer dependency.

## Privacy and cost

The Local Edition has no login, activation, Device ID, trial, subscription,
feature lock, advertising, telemetry, paid API, or mandatory cloud. Runtime media
stays local unless vMix itself sends its program output elsewhere.
