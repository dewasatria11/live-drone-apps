param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Configuration = 'Release',
    [string]$OutputDirectory = 'artifacts/release'
)
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'Packaging installer hanya boleh dijalankan pada Windows x64.' }
$root = Split-Path -Parent $PSScriptRoot
$release = & (Join-Path $PSScriptRoot 'assert-release-version.ps1') -Version $Version
$output = [IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $root 'artifacts')) + [IO.Path]::DirectorySeparatorChar
if (-not $output.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Output packaging harus berada di bawah folder artifacts repository.' }
$publish = Join-Path $output 'publish'
$staging = Join-Path $output 'installer-source'
if (Test-Path $output) { Remove-Item $output -Recurse -Force }
New-Item -ItemType Directory -Force $output, $publish, $staging | Out-Null
& (Join-Path $PSScriptRoot 'verify-vendor.ps1') -RuntimeIdentifier win-x64 | Out-Host
$project = Join-Path $root 'src/AlIkhsanMedia.Drone.App/AlIkhsanMedia.Drone.App.csproj'
dotnet restore $project --runtime win-x64
if ($LASTEXITCODE -ne 0) { throw 'Restore runtime Windows x64 gagal.' }
dotnet publish $project --configuration $Configuration --runtime win-x64 --self-contained true --no-restore --output $publish -p:Version=$($release.SemVer) -p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) { throw 'Publish self-contained Windows x64 gagal.' }
Copy-Item (Join-Path $publish '*') $staging -Recurse -Force
New-Item -ItemType Directory -Force (Join-Path $staging 'media') | Out-Null
Copy-Item (Join-Path $root 'vendor/mediamtx/win-x64/mediamtx.exe') (Join-Path $staging 'media/mediamtx.exe') -Force
Copy-Item (Join-Path $root 'eng/versions.json') (Join-Path $staging 'media/versions.json') -Force
Copy-Item (Join-Path $root 'THIRD_PARTY_NOTICES.md') $staging -Force
$isccCandidates = @(
    (Get-Command ISCC.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) }
$iscc = $isccCandidates | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 tidak ditemukan pada runner Windows.' }
$versionOutput = & $iscc '/?' 2>&1 | Select-Object -First 1
if ($versionOutput -notmatch '6\.7\.1') { throw "Versi Inno Setup tidak sesuai pin 6.7.1: $versionOutput" }
$installerName = "AlIkhsanMedia-DroneVersion-Setup-$Version"
& $iscc "/DAppVersion=$($release.SemVer)" "/DSourceDir=$staging" "/DOutputDir=$output" "/DOutputBaseFilename=$installerName" (Join-Path $root 'installer/AlIkhsanMediaDrone.iss')
if ($LASTEXITCODE -ne 0) { throw 'Kompilasi installer Inno Setup gagal.' }
$installer = Join-Path $output "$installerName.exe"
if (-not (Test-Path $installer)) { throw 'Installer yang diharapkan tidak ditemukan.' }
$hash = (Get-FileHash $installer -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $installerName.exe" | Set-Content (Join-Path $output 'SHA256SUMS.txt') -Encoding ascii
Copy-Item (Join-Path $root 'THIRD_PARTY_NOTICES.md') $output -Force
[pscustomobject]@{ Installer = $installer; Checksum = $hash; OutputDirectory = $output }
