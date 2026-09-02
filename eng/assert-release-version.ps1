param([Parameter(Mandatory = $true)][string]$Version)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if ($Version -notmatch '^v(?<semver>\d+\.\d+\.\d+)(?<pre>-(alpha|beta|rc)\.\d+)?$') {
    throw "Versi '$Version' tidak valid. Gunakan vX.Y.Z atau prerelease alpha/beta/rc bernomor."
}
$isPrerelease = -not [string]::IsNullOrEmpty($Matches.pre)
$semVer = $Matches.semver + $Matches.pre
if (-not $isPrerelease) {
    $checklistPath = Join-Path $root 'docs/RELEASE_CHECKLIST.md'
    if (-not (Test-Path $checklistPath)) { throw 'Release stabil ditolak: RELEASE_CHECKLIST.md tidak ditemukan.' }
    $checklist = Get-Content $checklistPath -Raw
    if ($checklist -match '(?m)^- \[ \]') { throw 'Release stabil ditolak: masih ada gate Phase 0–6 yang belum lulus.' }
    $hardwarePath = Join-Path $root 'docs/HARDWARE_VALIDATION.md'
    if (-not (Test-Path $hardwarePath) -or (Get-Content $hardwarePath -Raw) -notmatch '(?im)^Status:\s*Clean\s*$') {
        throw 'Release stabil ditolak: hardware DJI belum mempunyai status Clean.'
    }
}
[pscustomobject]@{ Tag = $Version; SemVer = $semVer; IsPrerelease = $isPrerelease }
