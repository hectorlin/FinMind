$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$webDir = Join-Path $scriptDir "FinMind.Web"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet is not installed or not in PATH."
}

if (-not (Test-Path $webDir -PathType Container)) {
    Write-Error "FinMind.Web directory not found at: $webDir"
}

Set-Location $webDir
dotnet run
