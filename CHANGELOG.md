# Changelog

Semua perubahan penting akan dicatat di dokumen ini. Format versi mengikuti SemVer.

## [Unreleased]

### Added

- Windows CI untuk full WPF build, DPAPI, Job Object, MediaMTX Windows, real
  RTMP→RTSP integration, reconnect, crash recovery, dan orphan cleanup.
- Release workflow terpisah untuk tag SemVer dan manual dispatch.
- Self-contained Windows x64 publish dan installer Inno Setup.
- SHA-256 installer, third-party notices, release notes Bahasa Indonesia, dan
  automatic prerelease classification.
- Machine-enforced block terhadap stable release sebelum gate Phase 0–6 dan
  hardware validation lengkap.

### Security

- MediaMTX Windows diverifikasi terhadap pinned archive dan executable checksum
  sebelum test maupun packaging.
- GitHub Release menggunakan scoped `GITHUB_TOKEN`; tidak memerlukan PAT.

### Known limitations

- Windows CI belum dijalankan karena workspace lokal belum merupakan Git worktree.
- Hardware DJI/vMix dan seluruh Phase 3–6 belum tervalidasi.
