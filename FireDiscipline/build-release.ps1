<#
    Fire Discipline - package build for GitHub Release.
    Delegates to the unified release packaging script in /scripts.
#>

[CmdletBinding()]
param(
    [string]$OutputDir
)

$Script = Join-Path $PSScriptRoot '..\scripts\build-release.ps1'
if ($OutputDir) {
    & $Script -ModName 'FireDiscipline' -OutputDir $OutputDir
} else {
    & $Script -ModName 'FireDiscipline'
}
