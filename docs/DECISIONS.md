# Engineering Decisions

## ADR-001 — Preserve the PRD project boundaries

**Status:** Accepted, 2026-09-02.

Core targets platform-neutral .NET 8 and owns domain records and interfaces.
Infrastructure implements MediaMTX and OS process details. SetupPortal remains a
separate assembly. App is the Windows-only WPF composition root. A test enforces
that Core has no forbidden project references.

## ADR-002 — Pin MediaMTX v1.20.1 and two platform artifacts

**Status:** Accepted, 2026-09-02.

Production packages the official Windows amd64 archive. The official Darwin arm64
archive is also pinned solely to prove the vertical slice on the current developer
host. Both archive hashes come from the signed-tag release's official
`checksums.sha256`; extracted executable hashes are recorded after verified
extraction so runtime can validate exactly what it executes. No `latest` URL or
runtime download is permitted.

## ADR-003 — FFmpeg is test-only

**Status:** Accepted, 2026-09-02.

The locally installed FFmpeg/FFprobe 8.0.1 provides a legal synthetic publisher and
real RTSP reader. It is not referenced by product assemblies or bundled in the
installer. CI documentation must pin/provision its version separately.

## ADR-004 — Cross-platform testable Infrastructure, Windows production ownership

**Status:** Accepted, 2026-09-02.

Media config, integrity, API parsing, restart policy, and lifecycle logic target
`net8.0` so they can be integration-tested on the current macOS host. Windows uses a
Job Object kill-on-close implementation. Non-Windows test hosts use explicit PID
ownership and tree termination. This does not change the Windows-only product target.

## ADR-005 — Minimal WPF skeleton before media gate

**Status:** Accepted, 2026-09-02.

Phase 0 creates a compilable WPF composition root but no dashboard or cosmetic UI.
Phase 1 is proven through services and integration tests first, as required by the
PRD.

## ADR-006 — No application-level media transform

**Status:** Accepted, 2026-09-02.

Production invokes MediaMTX only. Synthetic generation/decode flags exist only in
test process commands. The production route therefore cannot add a logo, cursor,
notification, border, lower-third, watermark, or any other frame overlay and cannot
transcode.

## ADR-007 — Windows solution remains authoritative

**Status:** Accepted, 2026-09-02.

The App project unconditionally targets `net8.0-windows`, enables WPF, and enables
Windows targeting. macOS does not compile an alternate App target. A solution
filter excludes App only for local portable-domain validation. The authoritative
GitHub Actions job uses `windows-latest` and builds the complete solution.

## ADR-008 — Windows capabilities behind Core-owned contracts

**Status:** Accepted, 2026-09-02.

Core owns `ISecretProtector`, `IProcessContainmentService`, `IFirewallService`,
`INetworkDiscoveryService`, and `IPortInspectionService`. Infrastructure contains
only real production Windows adapters. Test doubles exist solely in test projects;
there are no macOS substitutes for DPAPI, Job Objects, or Windows Firewall.

## ADR-009 — Settings schema v2 and immutable slot identity

**Status:** Accepted, 2026-09-02.

Schema v2 stores exactly six slots, immutable UUIDs, encrypted stream-key envelopes,
network selection, validated distinct ports, and application settings. Migration
from the conceptual PRD v1 shape preserves valid IDs and encrypted values, fills
missing slots deterministically with newly generated identities, and never silently
accepts malformed or duplicate data. Infrastructure persists validated JSON using
a same-directory temporary file followed by atomic replacement.

## ADR-010 — Separate CI and release trust boundaries

**Status:** Accepted, 2026-09-02.

`ci.yml` has read-only repository permission and never creates a release.
`release.yml` separates build/test/package from publication: only the final publish
job receives `contents: write`, and it uses the built-in `GITHUB_TOKEN`. Release
assets are transferred through Actions artifacts after every Windows gate succeeds.

## ADR-011 — Stable releases are mechanically blocked

**Status:** Accepted, 2026-09-02.

Tags without an alpha/beta/rc suffix cannot be packaged while any entry in
`docs/RELEASE_CHECKLIST.md` remains unchecked or DJI hardware validation is not
explicitly `Clean`. Prerelease suffixes are automatically published as GitHub
Pre-releases. Builds remain unsigned until an owner-provided signing certificate is
available; release notes must state that fact.
