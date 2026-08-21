<#
    Rimward Exiles - build and deploy to the RimWorld Mods folder.
    Delegates to the unified deployment script in /scripts.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$ModsPath = 'D:\SteamLibrary\steamapps\common\RimWorld\Mods',
    [string]$ModName  = 'RimwardExiles',
    [switch]$NoBuild
)

$Script = Join-Path $PSScriptRoot '..\scripts\deploy.ps1'
& $Script -ModName $ModName -ModsPath $ModsPath -NoBuild:$NoBuild
