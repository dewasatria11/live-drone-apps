param([Parameter(Mandatory = $true)][string]$Version, [Parameter(Mandatory = $true)][string]$Destination)
$ErrorActionPreference = 'Stop'
$release = & (Join-Path $PSScriptRoot 'assert-release-version.ps1') -Version $Version
$root = Split-Path -Parent $PSScriptRoot
$signature = 'Build ini belum ditandatangani secara digital. Windows dapat menampilkan peringatan SmartScreen; verifikasi SHA-256 sebelum menjalankan installer.'
$hardwarePath = Join-Path $root 'docs/HARDWARE_VALIDATION.md'
$hardware = if ((Test-Path $hardwarePath) -and (Get-Content $hardwarePath -Raw) -match '(?im)^Status:\s*Clean\s*$') { 'Clean, sesuai evidence pada docs/HARDWARE_VALIDATION.md.' } else { 'Belum diverifikasi pada drone, remote, HP Android, DJI Fly, dan vMix milik operator.' }
@"
# Al Ikhsan Media (Drone Version) $Version

## Ringkasan perubahan

Build pengembangan Windows untuk validasi jalur media lokal dan fondasi aplikasi.
Rincian perubahan per versi tersedia di `CHANGELOG.md`.

## Fitur yang sudah berfungsi

- MediaMTX v1.20.1 yang dipin dan diverifikasi checksum.
- Jalur RTMP langsung ke RTSP secara remux-only tanpa overlay aplikasi.
- Lifecycle child process, health check, reconnect, dan bounded crash recovery.
- Six-slot domain model, secure stream key, settings migration, dan adapter Windows.

## Instalasi singkat

1. Unduh `$($release.Tag)` installer dan `SHA256SUMS.txt` dari release ini.
2. Verifikasi checksum SHA-256.
3. Jalankan installer pada Windows 10 22H2 atau Windows 11 x64.

## Penggunaan singkat

Jalankan aplikasi, kirim URL RTMP dari DJI Fly, lalu gunakan URL RTSP loopback pada vMix. Build awal ini belum menyediakan seluruh UI operator Phase 3–6.

## Requirement Windows

- Windows 10 22H2 x64 atau Windows 11 x64.
- DJI Fly yang mendukung Custom RTMP.
- Laptop dan HP pada jaringan lokal yang sama.
- vMix untuk konsumsi RTSP.

## Known limitations

- UI penuh, setup portal, preview, diagnostics, installer validation pada laptop target, dan seluruh hardening Phase 3–6 belum selesai.
- Tanpa transcoding; codec sumber harus didukung vMix.
- Wi-Fi venue dapat menerapkan client isolation.

## Verifikasi checksum

Jalankan `Get-FileHash .\AlIkhsanMedia-DroneVersion-Setup-$Version.exe -Algorithm SHA256` dan cocokkan dengan `SHA256SUMS.txt`.

## Status pengujian

- Windows CI pada release ini wajib lulus sebelum asset diterbitkan.
- Hardware DJI: $hardware

## Tanda tangan digital

$signature
"@ | Set-Content $Destination -Encoding utf8
