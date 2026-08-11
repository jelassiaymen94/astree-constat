$ErrorActionPreference = "Stop"
$Frontend = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Frontend

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    throw "Node.js est introuvable. Installez Node.js 20 ou une version supérieure."
}

if (-not (Test-Path "node_modules")) {
    Write-Host "Installation des dépendances frontend..." -ForegroundColor Cyan
    npm install
    if ($LASTEXITCODE -ne 0) { throw "Échec de npm install." }
}

Write-Host "ASTREE Claims AI : http://localhost:5173" -ForegroundColor Green
Write-Host "Vérifiez que l'API .NET est disponible sur http://localhost:5294" -ForegroundColor Yellow
npm run dev
