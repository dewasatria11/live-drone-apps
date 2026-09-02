# Implementation Plan

Dokumen ini melacak implementasi Phase 0 sampai Phase 2 terhadap
`PRD_AL_IKHSAN_MEDIA_DRONE_VERSION.md`. Kotak hanya dicentang setelah ada bukti
build atau test nyata.

## Baseline inspeksi

- Repository awal hanya berisi PRD; folder bukan Git worktree, sehingga tidak ada
  perubahan Git pengguna yang dapat dibandingkan atau ditimpa.
- Host pengembangan: macOS Darwin arm64. Target produk tetap Windows 10/11 x64.
- .NET SDK belum tersedia pada inspeksi awal dan harus dipasang untuk build.
- FFmpeg/FFprobe 8.0.1 tersedia sebagai alat test-only, bukan dependency produk.

## Dependency graph

```text
AlIkhsanMedia.Drone.App (net8.0-windows, WPF)
  -> AlIkhsanMedia.Drone.Core
  -> AlIkhsanMedia.Drone.Infrastructure
  -> AlIkhsanMedia.Drone.SetupPortal

AlIkhsanMedia.Drone.Infrastructure -> AlIkhsanMedia.Drone.Core
AlIkhsanMedia.Drone.SetupPortal    -> AlIkhsanMedia.Drone.Core
AlIkhsanMedia.Drone.Core           -> no project reference
```

Test projects may reference the production assemblies they verify. Integration
tests invoke a checksum-verified MediaMTX binary and test-only FFmpeg processes.

## Phase 0 — Repository and decision baseline

- [x] Read the complete 1,935-line PRD.
- [x] Inspect repository, Git state, SDK, FFmpeg, and host architecture.
- [x] Create implementation plan, architecture, and decision log.
- [x] Create solution and all PRD project skeletons.
- [x] Enable nullable references, analyzers, deterministic builds, warnings as errors.
- [x] Add central package management with exact versions.
- [x] Add dependency-direction enforcement test.
- [x] Restore, build, and run Phase 0 tests cleanly.

Gate: clean restore/build/test and enforced dependency direction.

## Phase 1 — Media bridge vertical slice

- [x] Pin MediaMTX v1.20.1 official artifacts, licenses, URLs, and SHA-256.
- [x] Implement idempotent vendor restore with checksum verification before extract/use.
- [x] Verify the extracted executable checksum at runtime before process start.
- [x] Generate atomic, version-correct config with RTMP test binding and loopback-only
      RTSP/API/metrics/WebRTC bindings.
- [x] Implement serialized start/stop lifecycle and bounded stdout/stderr capture.
- [x] Implement real API health/path adapter with tolerant JSON contract tests.
- [x] Implement bounded crash restart policy (1s, 3s, 10s; maximum 3).
- [x] Ensure owned child process is terminated on disposal/test shutdown.
- [x] Publish synthetic H.264/AAC RTMP with FFmpeg and observe a real MediaMTX path.
- [x] Read RTSP over TCP with FFprobe/FFmpeg and assert decoded packet/frame evidence.
- [x] Stop publisher, republish same URL, and assert media flows again.
- [x] Kill MediaMTX and prove automatic recovery and subsequent media flow.
- [x] Stop service and prove child PID exits and all allocated ports can be rebound.
- [x] Run restore/build/all tests again from clean generated state.

Gate: real RTMP -> MediaMTX -> RTSP flow without WPF UI; reconnect, crash recovery,
and orphan/port cleanup tests all pass.

## Evidence log

Final clean run on 2026-09-02 (macOS arm64, .NET SDK 8.0.130):

- Restore: 8/8 projects restored from an empty NuGet cache request.
- Build: succeeded, 0 warnings, 0 errors.
- Tests: 9 passed, 0 failed (1 Core, 6 Infrastructure unit/contract, 1 boundary,
  1 full integration).
- Real media evidence: FFprobe counted 116 RTSP packets on initial publish, 116
  after publisher disconnect/reconnect, and 116 after forced engine crash/restart.
- Crash evidence: restart count 1; owned PID changed from 7654 to 7661.
- Cleanup evidence: PID 7661 no longer existed and all four dynamically allocated
  RTMP/RTSP/API/metrics ports rebound successfully after shutdown.

Phase 0 and Phase 1 gates are satisfied on the available host. The Windows x64
artifact is pinned and verified, and Windows Job Object code is present, but a
Windows 10/11 execution of that platform-specific branch remains part of the later
Windows validation matrix; it was not fabricated on this macOS host.

## Phase 2 — Domain, network, ports, settings

Strategi build: solution utama mempertahankan WPF `net8.0-windows`; macOS memakai
solution filter yang tidak mengubah target produk dan hanya membangun Core,
Infrastructure non-UI, SetupPortal, serta test portable. Workflow `windows-latest`
menjalankan solution penuh dan test berkategori Windows.

- [x] Six-slot model dengan UUID immutable dan default satu slot aktif.
- [x] Secure key URL-safe dengan entropy minimum 128-bit dan redaction.
- [x] `ISecretProtector` serta implementasi Windows DPAPI CurrentUser.
- [x] Settings schema, validation, v1 migration, dan atomic persistence.
- [x] RTMP/RTSP URL builder dengan IPv4 dan custom port.
- [x] Network discovery abstraction dan deterministic candidate scoring.
- [x] Klasifikasi physical/VPN/virtual/APIPA/default gateway/profile.
- [x] Port preflight yang tidak menghentikan process lain.
- [x] Stream state machine dan bitrate calculation dengan clock abstraction.
- [x] Stable diagnostic taxonomy.
- [x] Windows-only production adapters untuk DPAPI, Job Object, Firewall, network,
      dan port owner inspection.
- [x] Unit/integration tests portable lulus di macOS.
- [x] GitHub Actions Windows membangun WPF dan mewajibkan DPAPI, Job Object,
      Windows MediaMTX, media integration, serta orphan cleanup tests.
- [ ] Windows CI benar-benar dijalankan dan lulus.

Gate: semua logic portable dan generated URL lulus automated tests. Status Phase 2
tetap **Belum tervalidasi di Windows** sampai workflow berjalan pada repository
GitHub atau laptop Windows asli.

### Evidence Phase 2 — macOS arm64

Clean run 2026-09-02 dengan .NET SDK 8.0.130:

- workflow YAML dan `eng/versions.json` berhasil diparse;
- restore 6 project pada macOS solution filter;
- build sukses, 0 warning, 0 error;
- 25 test portable lulus, 0 gagal, 0 skip: 15 Core, 9 Infrastructure,
  dan 1 real-media integration;
- generated secure key dan URL builder dipakai langsung oleh integration test;
- RTSP packet: initial 116, publisher reconnect 116, engine recovery 116;
- restart PID 8532 -> 8545; setelah shutdown tidak ada process MediaMTX dan
  seluruh empat port dinamis dapat di-bind ulang;
- lima test berkategori `Windows` tersedia tetapi sengaja tidak dijalankan di
  macOS: DPAPI CurrentUser, Job Object, port owner, adapter/profile enumeration,
  dan Windows Firewall inspection;
- repository belum memiliki `.git`/remote, sehingga Windows workflow belum dapat
  dijalankan atau diperiksa. Windows status: **Belum tervalidasi di Windows**.

## CI, packaging, dan release automation

- [x] `ci.yml` terpisah untuk push `main` dan pull request, tanpa release mutation.
- [x] `release.yml` hanya untuk tag SemVer atau manual dispatch.
- [x] Full Windows build/test/checksum gate mendahului packaging dan publication.
- [x] Self-contained `win-x64` publish dan Inno Setup installer definition.
- [x] Installer checksum, third-party notice, dan release notes Bahasa Indonesia.
- [x] Alpha/beta/RC otomatis menjadi GitHub Pre-release.
- [x] Stable release ditolak sampai checklist Phase 0–6 dan DJI hardware Clean.
- [x] Hanya publication job memiliki `contents: write`; built-in `GITHUB_TOKEN` digunakan.
- [x] Workflow lolos YAML parser dan actionlint; seluruh script PowerShell lolos parser.
- [ ] Windows CI dan Inno Setup packaging benar-benar dijalankan pada GitHub runner.
- [ ] GitHub Release benar-benar diterbitkan atas instruksi eksplisit pemilik.

Status: automation sudah diimplementasikan dan tervalidasi statis dari macOS.
Execution Windows/Release belum diklaim karena workspace bukan Git repository dan
tidak ada push, tag, atau publication yang diizinkan.

## Phase 3–6 execution audit (2026-09-02)

- [x] WPF shell now opens with explicit six-slot dashboard surface and no fabricated
      stream URLs/status; full service orchestration remains gated on subsequent work.
- [x] Setup portal token store and embedded Kestrel endpoints have been added with
      no-store/CSP/nosniff headers and HTML-escaped output.
- [x] Quick start, troubleshooting, threat model, test plan, and hardware validation
      evidence template added.
- [ ] Real WPF startup orchestration, portal wiring, QR, diagnostics/support bundle,
      WebView2 preview, vMix manual validation, installer lifecycle, and DJI hardware
      validation remain incomplete and must not be marked as production-ready.
