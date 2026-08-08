<#
    Fire Discipline - build and deploy to the RimWorld Mods folder.

    Usage:
        .\deploy.ps1              build, then deploy
        .\deploy.ps1 -NoBuild     deploy whatever is already built
        .\deploy.ps1 -WhatIf      show what would happen, change nothing

    Deploys About/, 1.6/Assemblies/, 1.6/Defs/ and LoadFolders.xml if present.
    Source/ and build intermediates are never copied.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$ModsPath = 'D:\SteamLibrary\steamapps\common\RimWorld\Mods',
    [string]$ModName  = 'FireDiscipline',
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'

$SourceRoot  = $PSScriptRoot
$ProjectPath = Join-Path $SourceRoot 'Source\FireDiscipline\FireDiscipline.csproj'
$TargetRoot  = Join-Path $ModsPath $ModName

Write-Host "Fire Discipline deploy" -ForegroundColor Cyan
Write-Host "  from: $SourceRoot"
Write-Host "  to:   $TargetRoot"
Write-Host ""

# ---------------------------------------------------------------- build
if (-not $NoBuild) {
    if (-not (Test-Path $ProjectPath)) {
        throw "Project not found: $ProjectPath"
    }

    Write-Host "Building..." -ForegroundColor Yellow
    & dotnet build $ProjectPath -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE - nothing was deployed."
    }
    Write-Host "Build OK." -ForegroundColor Green
    Write-Host ""
}

# ---------------------------------------------------------------- verify source
$Assembly = Join-Path $SourceRoot '1.6\Assemblies\FireDiscipline.dll'
if (-not (Test-Path $Assembly)) {
    throw "Assembly not found: $Assembly. Run without -NoBuild."
}

$builtAt = (Get-Item $Assembly).LastWriteTime
Write-Host ("Assembly built at {0:yyyy-MM-dd HH:mm:ss}" -f $builtAt)

if (-not (Test-Path $ModsPath)) {
    throw "Mods folder not found: $ModsPath"
}

# ---------------------------------------------------------------- remove old
# Guarded delete: only ever removes a folder that sits directly under the Mods
# path AND looks like this mod. Prevents a bad -ModName or -ModsPath from
# recursively deleting something unrelated.
if (Test-Path $TargetRoot) {
    $marker = Join-Path $TargetRoot 'About\About.xml'
    $parent = Split-Path $TargetRoot -Parent

    if ($parent -ne (Resolve-Path $ModsPath).Path) {
        throw "Refusing to delete '$TargetRoot': it is not directly inside '$ModsPath'."
    }
    if (-not (Test-Path $marker)) {
        throw "Refusing to delete '$TargetRoot': no About\About.xml found, this does not look like a mod folder."
    }

    if ($PSCmdlet.ShouldProcess($TargetRoot, 'Remove existing install')) {
        Remove-Item -LiteralPath $TargetRoot -Recurse -Force
        Write-Host "Removed old install." -ForegroundColor Yellow
    }
}

# ---------------------------------------------------------------- copy new
$items = @(
    @{ Path = 'About';           Required = $true  },
    @{ Path = '1.6\Assemblies';  Required = $true  },
    @{ Path = '1.6\Defs';        Required = $true  },
    @{ Path = 'LoadFolders.xml'; Required = $false },
    @{ Path = 'README.md';       Required = $false }
)

if ($PSCmdlet.ShouldProcess($TargetRoot, 'Copy new build')) {
    New-Item -ItemType Directory -Path $TargetRoot -Force | Out-Null

    foreach ($item in $items) {
        $src = Join-Path $SourceRoot $item.Path

        if (-not (Test-Path $src)) {
            if ($item.Required) { throw "Missing required item: $src" }
            continue
        }

        $dst = Join-Path $TargetRoot $item.Path
        $dstParent = Split-Path $dst -Parent
        if (-not (Test-Path $dstParent)) {
            New-Item -ItemType Directory -Path $dstParent -Force | Out-Null
        }

        Copy-Item -LiteralPath $src -Destination $dst -Recurse -Force
        Write-Host "  copied $($item.Path)"
    }

    # The pdb is only useful for local debugging and bloats the published mod.
    $pdb = Join-Path $TargetRoot '1.6\Assemblies\FireDiscipline.pdb'
    if (Test-Path $pdb) { Remove-Item -LiteralPath $pdb -Force }

    Write-Host ""
    Write-Host "Deployed to $TargetRoot" -ForegroundColor Green
    Write-Host "RimWorld loads assemblies at startup - restart the game for this to take effect." -ForegroundColor Cyan
}
