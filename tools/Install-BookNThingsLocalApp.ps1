param(
    [string]$Url = "http://localhost:5038",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\BookNThings.Web\BookNThings.Web.csproj"
$publishDir = Join-Path $repoRoot ".artifacts\BookNThingsApp"
$launcherPath = Join-Path $PSScriptRoot "Start-BookNThings.ps1"
$shortcutIconPath = Join-Path $repoRoot "src\BookNThings.Web\wwwroot\icons\bnt-icon.ico"

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

if (-not $SkipPublish) {
    dotnet publish $projectPath --configuration Release --output $publishDir
}

$powerShell = Get-Command "pwsh.exe" -ErrorAction SilentlyContinue
if (-not $powerShell) {
    $powerShell = Get-Command "powershell.exe" -ErrorAction Stop
}

$desktop = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktop "BookNThings.lnk"

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $powerShell.Source
$shortcut.Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$launcherPath`" -Url `"$Url`""
$shortcut.WorkingDirectory = $repoRoot
$shortcut.Description = "Open the local BookNThings app"
if (Test-Path $shortcutIconPath) {
    $shortcut.IconLocation = "$shortcutIconPath,0"
}
$shortcut.Save()

Write-Host "Created Desktop shortcut: $shortcutPath"
Write-Host "The shortcut starts BookNThings locally and opens it in an Edge app window."
