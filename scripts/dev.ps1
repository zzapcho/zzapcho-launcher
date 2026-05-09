param(
    [switch] $SkipTests
)

. "$PSScriptRoot\common.ps1"

$dotnet = Initialize-Dotnet
Invoke-Restore -Dotnet $dotnet
Invoke-Build -Dotnet $dotnet

if (-not $SkipTests) {
    Invoke-Tests -Dotnet $dotnet
}

Write-Host ''
Write-Host 'Launching app...'
& $dotnet run --project (Join-Path $script:RepoRoot 'src\Zzapcho.Launcher.App') --no-build
