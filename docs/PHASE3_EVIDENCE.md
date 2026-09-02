# Phase 3 Evidence

## Automated evidence

- macOS portable build: sukses, 0 warning/0 error.
- macOS portable tests: 29 lulus (19 Core/ViewModel, 9 Infrastructure, 1 real-media integration).
- Windows CI: sukses pada run [33598390014](https://github.com/dewasatria11/live-drone-apps/actions/runs/33598390014), termasuk WPF build dan RTMP→RTSP integration.
- Runtime boundary: `RuntimeSession` memuat settings DPAPI, memilih adapter Windows, memverifikasi hash MediaMTX, menjalankan service nyata, dan memetakan path snapshots ke ViewModel.

## UI behavior implemented

- enam slot dari ViewModel, bukan data status runtime hardcoded;
- status text Waiting/Connecting/Live/Stale/Error dari domain state;
- URL RTMP DJI Fly dan RTSP loopback vMix dibangun dari key tervalidasi;
- copy menggunakan clipboard service dan feedback visual;
- engine startup/error message Bahasa Indonesia;
- tray menu dan safe-close confirmation saat slot Live;
- minimum window 1100×700, keyboard-focusable WPF controls, dark emerald design tokens.

## Open gate

Visual QA pada Windows 10/11 dengan scaling 100%, 125%, dan 150% belum dapat dijalankan
di host macOS dan belum boleh ditandai lulus. Screenshot harus diambil pada laptop Windows
target sebelum Phase 3 dinyatakan complete. Preview WebView2/QR tetap scope Phase 5/4.
