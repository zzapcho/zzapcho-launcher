Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$script:SdkDir = Join-Path $script:RepoRoot '.dotnet-sdk'
$script:ToolsDir = Join-Path $script:RepoRoot '.tools'
$script:DotnetHome = Join-Path $script:RepoRoot '.dotnet-home'
$script:DotnetExe = Join-Path $script:SdkDir 'dotnet.exe'
$script:Solution = Join-Path $script:RepoRoot 'ZzapchoRaidLauncher.sln'
$script:AppProject = Join-Path $script:RepoRoot 'src\Zzapcho.Launcher.App\Zzapcho.Launcher.App.csproj'
$script:TestProject = Join-Path $script:RepoRoot 'src\Zzapcho.Launcher.Tests\Zzapcho.Launcher.Tests.csproj'
$script:ArtifactsDir = Join-Path $script:RepoRoot 'artifacts'
$script:VelopackVersion = '0.0.1298'
$script:VelopackToolDir = Join-Path $script:ToolsDir 'vpk'

function Initialize-Dotnet {
    New-Item -ItemType Directory -Force -Path $script:SdkDir | Out-Null
    New-Item -ItemType Directory -Force -Path $script:ToolsDir | Out-Null
    New-Item -ItemType Directory -Force -Path (Join-Path $script:DotnetHome '.dotnet\tools') | Out-Null

    if (-not (Test-Path -LiteralPath $script:DotnetExe)) {
        $installScript = Join-Path $script:ToolsDir 'dotnet-install.ps1'
        if (-not (Test-Path -LiteralPath $installScript)) {
            Write-Host 'Downloading .NET install script...'
            Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installScript
        }

        Write-Host 'Installing .NET 8 SDK locally into .dotnet-sdk...'
        & powershell -ExecutionPolicy Bypass -File $installScript -Channel 8.0 -InstallDir $script:SdkDir 2>&1 |
            ForEach-Object { Write-Host $_ }
    }

    $env:DOTNET_ROOT = $script:SdkDir
    $env:DOTNET_CLI_HOME = $script:DotnetHome
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = '1'
    $env:MSBuildEnableWorkloadResolver = 'false'
    $env:MSBUILDDISABLENODEREUSE = '1'
    $env:MSBUILDUSESERVER = '0'

    return $script:DotnetExe
}

function Invoke-Restore {
    param([string] $Dotnet)

    Write-Host 'Restoring projects...'
    & $Dotnet restore $script:Solution /p:RestoreUseSkipNonexistentTargets=false -v:minimal
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet restore failed.'
    }
}

function Invoke-Build {
    param(
        [string] $Dotnet,
        [string] $Configuration = 'Debug'
    )

    Write-Host 'Building solution...'
    & $Dotnet build $script:Solution --no-restore -c $Configuration -m:1 -v:minimal
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet build failed.'
    }
}

function Invoke-Tests {
    param(
        [string] $Dotnet,
        [string] $Configuration = 'Debug'
    )

    Write-Host 'Running tests...'
    & $Dotnet run --project $script:TestProject --no-build -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw 'tests failed.'
    }
}

function Get-LauncherVersion {
    param([string] $Version)

    if (-not [string]::IsNullOrWhiteSpace($Version)) {
        return $Version
    }

    $propsPath = Join-Path $script:RepoRoot 'Directory.Build.props'
    [xml] $props = Get-Content -Encoding UTF8 -LiteralPath $propsPath
    $versionNode = $props.Project.PropertyGroup | Select-Object -First 1 | Select-Object -ExpandProperty Version
    if ([string]::IsNullOrWhiteSpace($versionNode)) {
        throw 'Directory.Build.props does not contain Version.'
    }

    return [string] $versionNode
}

function Reset-DirectoryUnderRepo {
    param([string] $Path)

    $repoRoot = [System.IO.Path]::GetFullPath($script:RepoRoot)
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clear path outside repository: $fullPath"
    }

    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $fullPath | Out-Null
    return $fullPath
}

function Invoke-PublishApp {
    param(
        [string] $Dotnet,
        [string] $Version,
        [string] $Runtime = 'win-x64',
        [string] $Configuration = 'Release'
    )

    $publishDir = Reset-DirectoryUnderRepo -Path (Join-Path $script:ArtifactsDir "publish\$Runtime")
    Write-Host "Restoring publish runtime ($Runtime)..."
    & $Dotnet restore $script:AppProject -r $Runtime -v:minimal 2>&1 |
        ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) {
        throw 'publish runtime restore failed.'
    }

    Write-Host "Publishing self-contained app ($Runtime, $Configuration, $Version)..."
    & $Dotnet publish $script:AppProject `
        --no-restore `
        -c $Configuration `
        -r $Runtime `
        --self-contained true `
        -o $publishDir `
        /p:Version=$Version `
        /p:PublishSingleFile=false `
        /p:PublishTrimmed=false `
        /p:PublishReadyToRun=false 2>&1 |
        ForEach-Object { Write-Host $_ }

    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet publish failed.'
    }

    return $publishDir
}

function Initialize-Velopack {
    param([string] $Dotnet)

    New-Item -ItemType Directory -Force -Path $script:VelopackToolDir | Out-Null
    $vpk = Join-Path $script:VelopackToolDir 'vpk.exe'

    if (Test-Path -LiteralPath $vpk) {
        Write-Host "Using Velopack CLI from $script:VelopackToolDir."
    }
    else {
        Write-Host "Installing Velopack CLI $script:VelopackVersion..."
        & $Dotnet tool install vpk --version $script:VelopackVersion --tool-path $script:VelopackToolDir 2>&1 |
            ForEach-Object { Write-Host $_ }

        if ($LASTEXITCODE -ne 0) {
            throw 'Velopack CLI install failed.'
        }
    }

    return $vpk
}

function Invoke-PackApp {
    param(
        [string] $Vpk,
        [string] $PublishDir,
        [string] $Version,
        [string] $Runtime = 'win-x64',
        [string] $Channel = 'win'
    )

    $releaseDir = Reset-DirectoryUnderRepo -Path (Join-Path $script:ArtifactsDir "releases\$Runtime")
    $releaseNotes = Join-Path $script:RepoRoot 'RELEASE_NOTES.md'

    Write-Host "Packaging Velopack release ($Runtime, $Channel, $Version)..."
    & $Vpk --yes pack `
        --packId 'Zzapcho.RaidLauncher' `
        --packVersion $Version `
        --packDir $PublishDir `
        --mainExe 'Zzapcho.Launcher.exe' `
        --packAuthors 'Zzapcho' `
        --packTitle 'Zzapcho Raid Launcher' `
        --runtime $Runtime `
        --channel $Channel `
        --outputDir $releaseDir `
        --releaseNotes $releaseNotes 2>&1 |
        ForEach-Object { Write-Host $_ }

    if ($LASTEXITCODE -ne 0) {
        throw 'Velopack packaging failed.'
    }

    return $releaseDir
}
