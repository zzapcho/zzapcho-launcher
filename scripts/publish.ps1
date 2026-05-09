param(
    [string] $Version = '',
    [string] $Runtime = 'win-x64',
    [string] $Configuration = 'Release',
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

Write-Host ''
Write-Host "Publish complete: $publishDir"
