$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Root

Write-Host "Validation S4 — ASTREE Claims AI" -ForegroundColor Cyan

$schema = Get-Content (Join-Path $Root "database\schema.sql") -Raw
$dto = Get-Content (Join-Path $Root "backend\AstreeClaims.Api\DTOs\Generation\GenerationDtos.cs") -Raw
$models = Get-Content (Join-Path $Root "ai-service\app\models.py") -Raw

if ($schema -notmatch "'summary', 'letter', 'response'") { throw "Types SQL incohérents." }
if ($dto -notmatch "summary\|letter\|response") { throw "Types .NET incohérents." }
if ($models -notmatch 'Literal\["summary", "letter", "response"\]') { throw "Types FastAPI incohérents." }
Write-Host "[OK] Types cohérents : summary, letter, response" -ForegroundColor Green

$python = Join-Path $Root "ai-service\.venv\Scripts\python.exe"
if (-not (Test-Path $python)) {
    python -m venv (Join-Path $Root "ai-service\.venv")
}

& $python -c "import fastapi, uvicorn, pydantic_settings, groq, pytest" 2>$null
if ($LASTEXITCODE -ne 0) {
    & $python -m pip install -r (Join-Path $Root "ai-service\requirements.txt")
    if ($LASTEXITCODE -ne 0) { throw "Installation Python impossible." }
}

Write-Host "Tests .NET..." -ForegroundColor Cyan
dotnet test (Join-Path $Root "AstreeClaims.sln") --nologo
if ($LASTEXITCODE -ne 0) { throw "Échec des tests .NET." }

Write-Host "Tests Python..." -ForegroundColor Cyan
$previousProvider = $env:LLM_PROVIDER
$env:LLM_PROVIDER = "deterministic"
& $python -m pytest (Join-Path $Root "ai-service\tests") -q
$pythonExit = $LASTEXITCODE
$env:LLM_PROVIDER = $previousProvider
if ($pythonExit -ne 0) { throw "Échec des tests Python." }

Write-Host "Validation S4 réussie : cohérence statique, tests .NET et tests Python." -ForegroundColor Green
Write-Host "Validation manuelle recommandée : générer summary, letter et response depuis Swagger." -ForegroundColor Yellow
