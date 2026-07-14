param(
    [string]$Url = "http://localhost:5038",
    [int]$StartupTimeoutSeconds = 45
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot ".artifacts\BookNThingsApp"
$projectPath = Join-Path $repoRoot "src\BookNThings.Web\BookNThings.Web.csproj"
$appDll = Join-Path $publishDir "BookNThings.Web.dll"

function Test-AppIsRunning {
    param([string]$TargetUrl)

    try {
        $response = Invoke-WebRequest -Uri $TargetUrl -UseBasicParsing -TimeoutSec 2
        return $response.StatusCode -ge 200 -and $response.StatusCode -lt 500
    }
    catch {
        return $false
    }
}

function Find-Edge {
    $edgeCommand = Get-Command "msedge.exe" -ErrorAction SilentlyContinue
    if ($edgeCommand) {
        return $edgeCommand.Source
    }

    $candidates = @()
    if (${env:ProgramFiles(x86)}) {
        $candidates += Join-Path ${env:ProgramFiles(x86)} "Microsoft\Edge\Application\msedge.exe"
    }

    if ($env:ProgramFiles) {
        $candidates += Join-Path $env:ProgramFiles "Microsoft\Edge\Application\msedge.exe"
    }

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path $candidate)) {
            return $candidate
        }
    }

    return $null
}

if (-not (Test-Path $appDll)) {
    dotnet publish $projectPath --configuration Release --output $publishDir
}

if (-not (Test-AppIsRunning -TargetUrl $Url)) {
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = $Url

    Start-Process `
        -FilePath "dotnet" `
        -ArgumentList "`"$appDll`"" `
        -WorkingDirectory $publishDir `
        -WindowStyle Hidden

    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-AppIsRunning -TargetUrl $Url) {
            break
        }

        Start-Sleep -Milliseconds 500
    }
}

$edgePath = Find-Edge
if ($edgePath) {
    Start-Process -FilePath $edgePath -ArgumentList "--app=$Url"
}
else {
    Start-Process $Url
}
