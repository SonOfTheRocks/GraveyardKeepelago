[CmdletBinding()]
param(
    [string]$GameDirectory,
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$stage = Join-Path $repository ('out/single-dll-' + [Guid]::NewGuid().ToString('N'))
$build = Join-Path $stage 'build'
$project = Join-Path $repository 'GraveyardKeepelago/GraveyardKeepelago.csproj'
$arguments = @('build', $project, '--configuration', 'Release',
    "-p:BaseOutputPath=$build/", '-p:PackageClient=true', '--verbosity', 'quiet')
if ($GameDirectory) {
    $arguments += "-p:GamePath=$((Resolve-Path -LiteralPath $GameDirectory).Path)"
}
if ($NoRestore) { $arguments += '--no-restore' }

& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Release build failed; no standalone DLL created.' }

$binaryDirectory = Join-Path $build 'Release/netstandard2.0'
$tool = (Get-Content -LiteralPath (Join-Path $binaryDirectory 'ilrepack-path.txt') -Raw).Trim()
$standalone = Join-Path $stage 'GraveyardKeepelago.dll'
$names = @('GraveyardKeepelago.dll', 'Archipelago.MultiClient.Net.dll',
    'KaitoKid.ArchipelagoUtilities.Net.dll', 'KaitoKid.Utilities.Interfaces.dll')
$inputs = @($names | ForEach-Object { Join-Path $binaryDirectory $_ })
$references = @(Get-Content -LiteralPath (Join-Path $binaryDirectory 'packaging-reference-directories.txt') |
    Sort-Object -Unique | ForEach-Object { "/lib:$_" })
& $tool "/out:$standalone" "/lib:$binaryDirectory" /target:library @references @inputs
if ($LASTEXITCODE -ne 0) { throw 'Assembly merge failed; no release created.' }

$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($standalone).FileVersion
$hash = (Get-FileHash -LiteralPath $standalone -Algorithm SHA256).Hash.Substring(0, 12).ToLowerInvariant()
$destination = Join-Path $repository "out/releases/GraveyardKeepelago-$version-$hash"
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item -LiteralPath $standalone -Destination $destination
Write-Output "Single-DLL release: $(Join-Path $destination 'GraveyardKeepelago.dll')"
