. "$PSScriptRoot\common.ps1"

$dotnet = Initialize-Dotnet
Invoke-Restore -Dotnet $dotnet
Invoke-Build -Dotnet $dotnet

Write-Host ''
Write-Host 'Launching app...'
& $dotnet run --project (Join-Path $script:RepoRoot 'src\Zzapcho.Launcher.App') --no-build
