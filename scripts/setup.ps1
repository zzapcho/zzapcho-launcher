. "$PSScriptRoot\common.ps1"

$dotnet = Initialize-Dotnet
& $dotnet --info
Invoke-Restore -Dotnet $dotnet

Write-Host ''
Write-Host 'Setup complete.'
