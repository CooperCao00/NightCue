param(
    [string]$OutputPath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts\packages')
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$version = (Get-Content -LiteralPath (Join-Path $repo 'VERSION') -Raw).Trim()
$stage = Join-Path $repo ('artifacts\package-work\' + [Guid]::NewGuid().ToString('N'))
$buildOutput = Join-Path $stage 'build'
& (Join-Path $repo 'build.ps1') -OutputPath $buildOutput

# Explicit allowlists prevent local settings, logs, credentials and build output
# from accidentally entering a source archive. Dotfiles are included explicitly.
$sourceRoot = Join-Path $stage 'source\NightCue'
$portableRoot = Join-Path $stage 'portable\NightCue'
New-Item -ItemType Directory -Path $sourceRoot, $portableRoot, $OutputPath -Force | Out-Null
$sourceFiles = @(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'source-files.txt') |
    Where-Object { $_ -and -not $_.StartsWith('#') })
foreach ($name in $sourceFiles) {
    $target = Join-Path $sourceRoot $name
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $target
}
Copy-Item -LiteralPath (Join-Path $buildOutput 'app\NightCue.exe') -Destination $portableRoot
foreach ($name in @('LICENSE', 'CHANGELOG.md', 'docs/QUICKSTART.en.txt', 'docs/使用说明.txt')) {
    Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $portableRoot
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
function New-ReleaseZip([string]$root, [string]$destination) {
    # Create a new file beside the destination; no recursive cleanup is needed.
    $temporaryZip = $destination + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
    [IO.Compression.ZipFile]::CreateFromDirectory($root, $temporaryZip, [IO.Compression.CompressionLevel]::Optimal, $true)
    $archive = [IO.Compression.ZipFile]::OpenRead($temporaryZip)
    try {
        foreach ($entry in $archive.Entries) {
            if ($entry.FullName -match '(^|/)(settings\.xml|state\.xml|error\.log)$') {
                throw "Runtime data found in archive: $($entry.FullName)"
            }
        }
        if (-not ($archive.Entries.FullName -contains 'NightCue/LICENSE')) { throw 'License missing from archive.' }
    }
    finally { $archive.Dispose() }
    Move-Item -LiteralPath $temporaryZip -Destination $destination -Force
}

$sourceZip = Join-Path $OutputPath "NightCue-source-v$version.zip"
$portableZip = Join-Path $OutputPath "NightCue-v$version-windows-portable.zip"
New-ReleaseZip $sourceRoot $sourceZip
New-ReleaseZip $portableRoot $portableZip
$hashes = foreach ($file in @($sourceZip, $portableZip)) {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant(), [IO.Path]::GetFileName($file)
}
[IO.File]::WriteAllLines((Join-Path $OutputPath 'SHA256SUMS.txt'), [string[]]$hashes, [Text.Encoding]::ASCII)
Write-Output "Release packages ready: $OutputPath"
Write-Output 'Nothing has been uploaded or published.'
