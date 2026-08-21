<#
    .SYNOPSIS
        Deploys RimWorld mods from this repository directly to the local game Mods folder.

    .DESCRIPTION
        Builds the project (unless -NoBuild is specified) and copies only necessary runtime
        mod assets into the RimWorld Mods folder. Protects against accidental deletion with
        directory boundary and marker checks.

    .PARAMETER ModName
        The mod to deploy: 'FireDiscipline', 'EchoResonance', 'LoneSurvivor', 'MatrilinealGene', 'RimwardExiles', or 'All'.
        Defaults to 'FireDiscipline'.

    .PARAMETER ModsPath
        The destination RimWorld Mods directory.
        Defaults to 'D:\SteamLibrary\steamapps\common\RimWorld\Mods'.

    .PARAMETER NoBuild
        Skips invoking dotnet build before deployment.

    .PARAMETER All
        Deploys all known mods in the repository.

    .EXAMPLE
        .\scripts\deploy.ps1 -ModName FireDiscipline
        .\scripts\deploy.ps1 -ModName LoneSurvivor -WhatIf
        .\scripts\deploy.ps1 -All
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Position = 0)]
    [ValidateSet('FireDiscipline', 'EchoResonance', 'LoneSurvivor', 'MatrilinealGene', 'RimwardExiles', 'All')]
    [string]$ModName = 'FireDiscipline',

    [string]$ModsPath = 'D:\SteamLibrary\steamapps\common\RimWorld\Mods',

    [switch]$NoBuild,

    [switch]$All
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path $PSScriptRoot -Parent

$KnownMods = @('FireDiscipline', 'EchoResonance', 'LoneSurvivor', 'MatrilinealGene', 'RimwardExiles')

function Deploy-Mod {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TargetMod
    )

    $ModRoot    = Join-Path $RepoRoot $TargetMod
    $TargetRoot = Join-Path $ModsPath $TargetMod

    if (-not (Test-Path $ModRoot)) {
        Write-Warning "Mod folder not found: $ModRoot. Skipping."
        return
    }

    Write-Host "====================================================" -ForegroundColor Cyan
    Write-Host "  Deploying: $TargetMod" -ForegroundColor Cyan
    Write-Host "    from: $ModRoot"
    Write-Host "    to:   $TargetRoot"
    Write-Host "====================================================" -ForegroundColor Cyan

    # 1. Build C# Project (if not skipped)
    if (-not $NoBuild) {
        $csprojFiles = Get-ChildItem -Path (Join-Path $ModRoot 'Source') -Filter '*.csproj' -Recurse -ErrorAction SilentlyContinue
        if ($csprojFiles -and $csprojFiles.Count -gt 0) {
            $csprojPath = $csprojFiles[0].FullName
            Write-Host "Building project: $($csprojFiles[0].Name)..." -ForegroundColor Yellow
            & dotnet build $csprojPath -v minimal /p:SkipDeploy=true
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed for $TargetMod with exit code $LASTEXITCODE - deployment aborted."
            }
            Write-Host "Build OK." -ForegroundColor Green
        } else {
            Write-Host "No .csproj found for $TargetMod. Skipping build step." -ForegroundColor Gray
        }
    }

    # 2. Check Mods Directory Existence
    if (-not (Test-Path $ModsPath)) {
        throw "RimWorld Mods destination folder not found: $ModsPath"
    }

    # 3. Guarded Delete of Existing Deployment
    if (Test-Path $TargetRoot) {
        $marker = Join-Path $TargetRoot 'About\About.xml'
        $parent = Split-Path $TargetRoot -Parent

        $resolvedModsPath = (Resolve-Path $ModsPath).Path
        $resolvedParent   = (Resolve-Path $parent).Path

        if ($resolvedParent -ne $resolvedModsPath) {
            throw "Refusing to delete '$TargetRoot': it is not directly inside '$ModsPath'."
        }
        if (-not (Test-Path $marker)) {
            throw "Refusing to delete '$TargetRoot': no About\About.xml found, this does not appear to be a mod folder."
        }

        if ($PSCmdlet.ShouldProcess($TargetRoot, "Remove existing install for $TargetMod")) {
            Remove-Item -LiteralPath $TargetRoot -Recurse -Force
            Write-Host "Removed previous install." -ForegroundColor Yellow
        }
    }

    # 4. Copy New Build
    if ($PSCmdlet.ShouldProcess($TargetRoot, "Deploy mod files for $TargetMod")) {
        New-Item -ItemType Directory -Path $TargetRoot -Force | Out-Null

        # Copy About folder
        $aboutSrc = Join-Path $ModRoot 'About'
        if (Test-Path $aboutSrc) {
            Copy-Item -LiteralPath $aboutSrc -Destination (Join-Path $TargetRoot 'About') -Recurse -Force
            Write-Host "  [+] Copied About/"
        }

        # Copy Root configuration & docs
        foreach ($rootFile in @('LoadFolders.xml', 'README.md')) {
            $fSrc = Join-Path $ModRoot $rootFile
            if (Test-Path $fSrc) {
                Copy-Item -LiteralPath $fSrc -Destination (Join-Path $TargetRoot $rootFile) -Force
                Write-Host "  [+] Copied $rootFile"
            }
        }

        # Copy Versioned Folders (e.g., 1.5, 1.6, Common)
        $versionDirs = Get-ChildItem -Path $ModRoot -Directory | Where-Object { $_.Name -match '^1\.\d+$|^Common$' }
        foreach ($vDir in $versionDirs) {
            $destVersionDir = Join-Path $TargetRoot $vDir.Name
            New-Item -ItemType Directory -Path $destVersionDir -Force | Out-Null

            $subFolders = @('Assemblies', 'Defs', 'Patches', 'Languages', 'Textures', 'Sounds')
            foreach ($sub in $subFolders) {
                $subSrc = Join-Path $vDir.FullName $sub
                if (Test-Path $subSrc) {
                    Copy-Item -LiteralPath $subSrc -Destination (Join-Path $destVersionDir $sub) -Recurse -Force
                    Write-Host "  [+] Copied $($vDir.Name)/$sub"
                }
            }
        }

        # Strip PDB files from destination assemblies
        Get-ChildItem -Path $TargetRoot -Filter '*.pdb' -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force

        Write-Host "Deployed successfully to: $TargetRoot" -ForegroundColor Green
        Write-Host ""
    }
}

# Determine targets
$Targets = @()
if ($All -or $ModName -eq 'All') {
    $Targets = $KnownMods
} else {
    $Targets = @($ModName)
}

foreach ($target in $Targets) {
    Deploy-Mod -TargetMod $target
}

Write-Host "Deployment completed. Restart RimWorld for assembly changes to take effect." -ForegroundColor Cyan
