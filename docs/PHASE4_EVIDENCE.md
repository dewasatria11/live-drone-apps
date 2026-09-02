# Phase 4 Evidence

## Implemented

- Local Kestrel setup portal binds to the configured address and port.
- Setup tokens are random, slot-scoped, limited to ten minutes, and revocable.
- Portal responses use `no-store`, `nosniff`, `no-referrer`, strict CSP nonce, neutral 404 responses, and HTML escaping.
- Mobile setup page provides selectable RTMP text and Clipboard API fallback.
- Kestrel logging providers are disabled so token-bearing request paths are not emitted by the portal.

## Test evidence

- macOS Core/portal tests: 20 passed.
- macOS RTMP → RTSP integration regression: 2 passed.
- Windows CI run 33634046821: success (WPF build, Windows production adapters, MediaMTX and integration suite).

## Open validation

- Manual access from a physical phone on a Windows laptop/network is not available on the macOS development host.
- QR image rendering, support-bundle export, and diagnostics aggregation remain Phase 4 work items before the phase can be declared complete.
