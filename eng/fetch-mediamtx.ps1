param(
    [ValidateSet('win-x64', 'osx-arm64')]
    [string]$RuntimeIdentifier = 'win-x64'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$manifest = Get-Content (Join-Path $PSScriptRoot 'versions.json') -Raw | ConvertFrom-Json
$dependency = $manifest.dependencies | Where-Object name -eq 'MediaMTX'
$artifact = $dependency.artifacts | Where-Object rid -eq $RuntimeIdentifier
if (-not $artifact) { throw "Artifact MediaMTX tidak ditemukan untuk $RuntimeIdentifier." }
$destination = Join-Path $root "vendor/mediamtx/$RuntimeIdentifier"
New-Item -ItemType Directory -Force $destination | Out-Null
$archive = Join-Path $destination $artifact.fileName
if (-not (Test-Path $archive) -or (Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $artifact.sha256) {
    Invoke-WebRequest -UseBasicParsing -Uri $artifact.url -OutFile "$archive.partial"
    if ((Get-FileHash "$archive.partial" -Algorithm SHA256).Hash.ToLowerInvariant() -ne $artifact.sha256) {
        Remove-Item "$archive.partial" -Force
        throw 'Checksum archive MediaMTX tidak cocok; file tidak dieksekusi.'
    }
    Move-Item "$archive.partial" $archive -Force
}
if ($archive.EndsWith('.zip')) { Expand-Archive $archive $destination -Force }
else { & tar -xzf $archive -C $destination; if ($LASTEXITCODE -ne 0) { throw 'Ekstraksi MediaMTX gagal.' } }
$executable = Join-Path $destination $artifact.executable
if (-not (Test-Path $executable)) { throw 'Executable MediaMTX tidak ditemukan setelah ekstraksi.' }
if ($artifact.executableSha256 -and (Get-FileHash $executable -Algorithm SHA256).Hash.ToLowerInvariant() -ne $artifact.executableSha256) {
    throw 'Checksum executable MediaMTX tidak cocok; binary tidak dijalankan.'
}
Write-Output $executable
