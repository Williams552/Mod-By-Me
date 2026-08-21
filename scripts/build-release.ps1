<#
    .SYNOPSIS
        Packages RimWorld mods in this repository into release-ready zip archives.

    .DESCRIPTION
        Builds the specified mod (or all mods) in Release configuration and packages
        only the runtime assets (About, versioned Assemblies, Defs, Patches, Languages,
        Textures, LoadFolders.xml, README.md) into a standalone <ModName>-Release.zip.

    .PARAMETER ModName
        The name of the mod to package: 'FireDiscipline', 'EchoResonance', 'LoneSurvivor',
        'MatrilinealGene', 'RimwardExiles', or 'All'.

    .PARAMETER All
        Switch to build and package all available mods in the repository.

    .PARAMETER OutputDir
        Optional directory to save generated zip files. Defaults to each mod's root folder.

    .EXAMPLE
        .\scripts\build-release.ps1 -ModName FireDiscipline
        .\scripts\build-release.ps1 -All
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('FireDiscipline', 'EchoResonance', 'LoneSurvivor', 'MatrilinealGene', 'RimwardExiles', 'All')]
    [string]$ModName = 'All',

    [switch]$All,

    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path $PSScriptRoot -Parent

$KnownMods = @('FireDiscipline', 'EchoResonance', 'LoneSurvivor', 'MatrilinealGene', 'RimwardExiles')

function Build-ModRelease {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TargetMod
    )

    $ModRoot = Join-Path $RepoRoot $TargetMod
    if (-not (Test-Path $ModRoot)) {
        Write-Warning "Mod folder not found: $ModRoot. Skipping."
        return
    }

    Write-Host "====================================================" -ForegroundColor Cyan
    Write-Host "  Packaging Release: $TargetMod" -ForegroundColor Cyan
    Write-Host "====================================================" -ForegroundColor Cyan

    # 1. Locate C# Project if present
    $csprojFiles = Get-ChildItem -Path (Join-Path $ModRoot 'Source') -Filter '*.csproj' -Recurse -ErrorAction SilentlyContinue
    if ($csprojFiles -and $csprojFiles.Count -gt 0) {
        $csprojPath = $csprojFiles[0].FullName
        Write-Host "Building project: $($csprojFiles[0].Name)..." -ForegroundColor Yellow
        & dotnet build $csprojPath -c Release -v minimal /p:SkipDeploy=true
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for $TargetMod with exit code $LASTEXITCODE."
        }
        Write-Host "Build OK." -ForegroundColor Green
    } else {
        Write-Host "No .csproj found for $TargetMod (XML/Content mod or WIP). Skipping dotnet build." -ForegroundColor Gray
    }

    # 2. Setup Staging Directory
    $ReleaseDir = Join-Path $ModRoot 'ReleaseBuild'
    if (Test-Path $ReleaseDir) {
        Remove-Item -Path $ReleaseDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null

    $StagingModDir = Join-Path $ReleaseDir $TargetMod
    New-Item -ItemType Directory -Path $StagingModDir -Force | Out-Null

    # 3. Copy Mod Distribution Files
    Write-Host "Collecting distribution files..." -ForegroundColor Yellow

    # Copy About folder (Mandatory)
    $aboutSrc = Join-Path $ModRoot 'About'
    if (-not (Test-Path $aboutSrc)) {
        Write-Warning "No About/ folder found in $TargetMod. Skipping package."
        Remove-Item -Path $ReleaseDir -Recurse -Force
        return
    }
    Copy-Item -Path $aboutSrc -Destination (Join-Path $StagingModDir 'About') -Recurse -Force

    # Copy Root files
    foreach ($rootFile in @('LoadFolders.xml', 'README.md', 'LICENSE')) {
        $fSrc = Join-Path $ModRoot $rootFile
        if (Test-Path $fSrc) {
            Copy-Item -LiteralPath $fSrc -Destination (Join-Path $StagingModDir $rootFile) -Force
        }
    }

    # Copy Versioned Folders (e.g., 1.5, 1.6, Common)
    $versionDirs = Get-ChildItem -Path $ModRoot -Directory | Where-Object { $_.Name -match '^1\.\d+$|^Common$' }
    foreach ($vDir in $versionDirs) {
        $destVersionDir = Join-Path $StagingModDir $vDir.Name
        New-Item -ItemType Directory -Path $destVersionDir -Force | Out-Null

        $subFolders = @('Assemblies', 'Defs', 'Patches', 'Languages', 'Textures', 'Sounds')
        foreach ($sub in $subFolders) {
            $subSrc = Join-Path $vDir.FullName $sub
            if (Test-Path $subSrc) {
                Copy-Item -Path $subSrc -Destination (Join-Path $destVersionDir $sub) -Recurse -Force
            }
        }
    }

    # Remove debugging pdb files from staging assemblies
    Get-ChildItem -Path $StagingModDir -Filter '*.pdb' -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force

    # 4. Zip the Mod Folder
    $ZipDestDir = if ($OutputDir) { $OutputDir } else { $ModRoot }
    if (-not (Test-Path $ZipDestDir)) {
        New-Item -ItemType Directory -Path $ZipDestDir -Force | Out-Null
    }

    $ZipPath = Join-Path $ZipDestDir "$TargetMod-Release.zip"
    if (Test-Path $ZipPath) {
        Remove-Item -LiteralPath $ZipPath -Force
    }

    Write-Host "Compressing to $ZipPath..." -ForegroundColor Yellow
    Compress-Archive -Path $StagingModDir -DestinationPath $ZipPath -Force

    # Cleanup staging directory
    Remove-Item -Path $ReleaseDir -Recurse -Force

    Write-Host "Success! Created: $ZipPath" -ForegroundColor Green
    Write-Host ""
}

# Determine targets
$Targets = @()
if ($All -or $ModName -eq 'All') {
    $Targets = $KnownMods
} else {
    $Targets = @($ModName)
}

foreach ($target in $Targets) {
    Build-ModRelease -TargetMod $target
}

Write-Host "All requested release packages completed successfully." -ForegroundColor Green
