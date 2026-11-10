[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PluginPath,
    [Parameter(Mandatory = $true)][string]$GameDirectory
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$manifest = Join-Path $repository 'External/GraveyardKeeperData/manifest.json'
$expectedDataVersion = (Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json).data_version
if ($expectedDataVersion -le 0) { throw 'The shared-data manifest must declare a positive data version.' }
$stage = Join-Path $repository ('out/verify-single-dll-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
$exe = Join-Path $stage 'Smoke.exe'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo "/out:$exe" (Join-Path $PSScriptRoot 'SingleDllSmoke.cs')
if ($LASTEXITCODE -ne 0) { throw 'Smoke-test compilation failed.' }
$isolated = Join-Path $stage 'GraveyardKeepelago.dll'
Copy-Item -LiteralPath (Resolve-Path -LiteralPath $PluginPath).Path -Destination $isolated
& $exe $isolated (Resolve-Path -LiteralPath $GameDirectory).Path $expectedDataVersion
if ($LASTEXITCODE -ne 0) { throw 'Standalone DLL verification failed.' }
