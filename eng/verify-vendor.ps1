param([ValidateSet('win-x64', 'osx-arm64')][string]$RuntimeIdentifier = 'win-x64')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$manifest = Get-Content (Join-Path $PSScriptRoot 'versions.json') -Raw | ConvertFrom-Json
$artifact = ($manifest.dependencies | Where-Object name -eq 'MediaMTX').artifacts | Where-Object rid -eq $RuntimeIdentifier
$executable = Join-Path $root "vendor/mediamtx/$RuntimeIdentifier/$($artifact.executable)"
if (-not (Test-Path $executable)) { throw 'Binary vendor belum tersedia.' }
$actual = (Get-FileHash $executable -Algorithm SHA256).Hash.ToLowerInvariant()
if (-not $artifact.executableSha256 -or $actual -ne $artifact.executableSha256) { throw 'Checksum executable MediaMTX tidak cocok.' }
Write-Output "MediaMTX $RuntimeIdentifier terverifikasi: $actual"
