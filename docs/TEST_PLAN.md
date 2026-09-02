# Test Plan

Automated: Core unit tests, settings/migration tests, MediaMTX config/API contract tests,
real RTMP→RTSP integration/reconnect/crash/cleanup tests, serta Windows-only DPAPI, Job Object,
network, port, dan firewall tests pada GitHub Actions `windows-latest`.

Manual Windows: Windows 10 22H2 dan Windows 11 x64, Private/Public profile, Ethernet/Wi‑Fi,
VPN/Hyper-V/WSL/Docker noise, port conflict, sleep/resume, IP change, dan installer lifecycle.

Hardware gate: drone, remote, HP/DJI Fly, router, laptop operator, dan vMix aktual harus dicatat
di `HARDWARE_VALIDATION.md`; belum boleh diberi status Clean tanpa bukti rekaman/screenshot.
