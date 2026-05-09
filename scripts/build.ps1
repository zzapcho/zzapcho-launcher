. "$PSScriptRoot\common.ps1"

$dotnet = Initialize-Dotnet
Invoke-Restore -Dotnet $dotnet
Invoke-Build -Dotnet $dotnet

Write-Host ''
Write-Host 'Build complete.'
