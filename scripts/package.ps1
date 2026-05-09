param(
    [string] $Version = '',
    [string] $Runtime = 'win-x64',
    [string] $Configuration = 'Release',
    [string] $Channel = 'win',
    [switch] $SkipTests
)

. "$PSScriptRoot\common.ps1"

$dotnet = Initialize-Dotnet
$resolvedVersion = Get-LauncherVersion -Version $Version

Invoke-Restore -Dotnet $dotnet
Invoke-Build -Dotnet $dotnet -Configuration $Configuration

if (-not $SkipTests) {
    Invoke-Tests -Dotnet $dotnet -Configuration $Configuration
}

$publishDir = Invoke-PublishApp -Dotnet $dotnet -Version $resolvedVersion -Runtime $Runtime -Configuration $Configuration
$vpk = Initialize-Velopack -Dotnet $dotnet
$releaseDir = Invoke-PackApp -Vpk $vpk -PublishDir $publishDir -Version $resolvedVersion -Runtime $Runtime -Channel $Channel

Write-Host ''
Write-Host "Package complete: $releaseDir"
