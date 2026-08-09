<#
    Fire Discipline - package build for GitHub Release.

    Usage:
        .\build-release.ps1

    This script builds the project and packages only the necessary mod files
    into a .zip file (FireDiscipline-Release.zip) that can be uploaded to GitHub Releases.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$SourceRoot  = $PSScriptRoot
$ProjectPath = Join-Path $SourceRoot 'Source\FireDiscipline\FireDiscipline.csproj'
$ReleaseDir  = Join-Path $SourceRoot 'ReleaseBuild'
$ZipPath     = Join-Path $SourceRoot 'FireDiscipline-Release.zip'

Write-Host "Fire Discipline - Building Release Zip" -ForegroundColor Cyan

# 1. Build Project
Write-Host "Building project..." -ForegroundColor Yellow
& dotnet build $ProjectPath -v minimal
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}
Write-Host "Build OK." -ForegroundColor Green

# 2. Prepare Release Directory
if (Test-Path $ReleaseDir) {
    Remove-Item -Path $ReleaseDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null

$ModDir = Join-Path $ReleaseDir 'FireDiscipline'
New-Item -ItemType Directory -Path $ModDir -Force | Out-Null

# 3. Copy necessary files (same as deploy.ps1)
$items = @(
    @{ Path = 'About';           Required = $true  },
    @{ Path = '1.6\Assemblies';  Required = $true  },
    @{ Path = '1.6\Defs';        Required = $true  },
    @{ Path = '1.6\Textures';    Required = $false },
    @{ Path = 'LoadFolders.xml'; Required = $false },
    @{ Path = 'README.md';       Required = $false }
)

Write-Host "Copying files..." -ForegroundColor Yellow
foreach ($item in $items) {
    $src = Join-Path $SourceRoot $item.Path
    $dst = Join-Path $ModDir $item.Path

    if (-not (Test-Path $src)) {
        if ($item.Required) { throw "Missing required item: $src" }
        continue
    }

    $dstParent = Split-Path $dst -Parent
    if (-not (Test-Path $dstParent)) {
        New-Item -ItemType Directory -Path $dstParent -Force | Out-Null
    }

    Copy-Item -Path $src -Destination $dst -Recurse -Force
}

# 4. Zip the folder
if (Test-Path $ZipPath) {
    Remove-Item -Path $ZipPath -Force
}

Write-Host "Zipping to $ZipPath..." -ForegroundColor Yellow
Compress-Archive -Path $ModDir -DestinationPath $ZipPath -Force

# Cleanup
Remove-Item -Path $ReleaseDir -Recurse -Force

Write-Host "Done! Release package created at: $ZipPath" -ForegroundColor Green
Write-Host "Upload this zip file to your GitHub Releases page." -ForegroundColor Green
