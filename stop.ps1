$ErrorActionPreference = "SilentlyContinue"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Root

$runDirectory = Join-Path $Root ".run"
foreach ($name in @("dotnet", "fastapi")) {
    $pidFile = Join-Path $runDirectory "$name.pid"
    if (Test-Path $pidFile) {
        $savedPid = Get-Content $pidFile
        if ($savedPid) {
            taskkill.exe /PID $savedPid /T /F | Out-Null
            Write-Host "$name arrêté." -ForegroundColor Green
        }
        Remove-Item $pidFile -Force
    }
}

docker compose stop sqlserver
Write-Host "Projet arrêté sans supprimer les données SQL." -ForegroundColor Green
