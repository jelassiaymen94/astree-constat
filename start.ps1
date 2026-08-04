$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Root

function Import-DotEnv([string]$Path) {
    if (-not (Test-Path $Path)) {
        throw "Fichier .env introuvable : $Path"
    }

    foreach ($line in Get-Content $Path) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith("#")) { continue }

        $separator = $trimmed.IndexOf("=")
        if ($separator -lt 1) { continue }

        $name = $trimmed.Substring(0, $separator).Trim()
        $value = $trimmed.Substring($separator + 1).Trim()
        if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        [Environment]::SetEnvironmentVariable($name, $value, "Process")
    }
}

function Test-ProcessFromPidFile([string]$PidFile) {
    if (-not (Test-Path $PidFile)) { return $false }
    $savedPid = Get-Content $PidFile -ErrorAction SilentlyContinue
    if (-not $savedPid) { return $false }
    return $null -ne (Get-Process -Id $savedPid -ErrorAction SilentlyContinue)
}

function Start-ServiceWindow([string]$Name, [string]$Command, [string]$PidFile) {
    if (Test-ProcessFromPidFile $PidFile) {
        Write-Host "$Name est déjà démarré." -ForegroundColor Yellow
        return
    }

    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($Command))
    $process = Start-Process powershell.exe -ArgumentList "-NoExit", "-EncodedCommand", $encoded -PassThru
    Set-Content -Path $PidFile -Value $process.Id
    Write-Host "$Name démarré (PID $($process.Id))." -ForegroundColor Green
}

function Wait-Endpoint([string]$Url, [int]$Seconds = 90) {
    $deadline = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-RestMethod -Uri $Url -TimeoutSec 3
            if ($response.connected -eq $true) { return $true }
        } catch {
            Start-Sleep -Seconds 2
            continue
        }
        Start-Sleep -Seconds 2
    }
    return $false
}

Import-DotEnv (Join-Path $Root ".env")

$required = @("SQLSERVER_SA_PASSWORD", "ConnectionStrings__DefaultConnection", "AiService__BaseUrl", "LLM_PROVIDER")
if ($env:LLM_PROVIDER -eq "groq") { $required += "GROQ_API_KEY" }
foreach ($name in $required) {
    $value = [Environment]::GetEnvironmentVariable($name, "Process")
    if ([string]::IsNullOrWhiteSpace($value)) { throw "Variable obligatoire absente du .env : $name" }
}

$python = Join-Path $Root "ai-service\.venv\Scripts\python.exe"
if (-not (Test-Path $python)) {
    Write-Host "Création de l'environnement Python..." -ForegroundColor Cyan
    python -m venv (Join-Path $Root "ai-service\.venv")
}

# Un environnement virtuel existant peut être incomplet ou obsolète. Vérifier les
# dépendances essentielles avant chaque lancement et ne réinstaller qu'en cas de besoin.
& $python -c "import fastapi, uvicorn, pydantic_settings, groq" 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Installation ou mise à jour des dépendances Python..." -ForegroundColor Cyan
    & $python -m pip install -r (Join-Path $Root "ai-service\requirements.txt")
    if ($LASTEXITCODE -ne 0) { throw "Échec de l'installation des dépendances Python." }
}

$runDirectory = Join-Path $Root ".run"
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null

Write-Host "Démarrage de SQL Server..." -ForegroundColor Cyan
docker compose up -d sqlserver

$fastApiCommand = "Set-Location '$Root'; & '$python' -m uvicorn app.main:app --app-dir ai-service --port 8000"
$dotnetProject = Join-Path $Root "backend\AstreeClaims.Api\AstreeClaims.Api.csproj"
$dotnetCommand = "Set-Location '$Root'; dotnet run --project '$dotnetProject' --launch-profile http"

Start-ServiceWindow "FastAPI" $fastApiCommand (Join-Path $runDirectory "fastapi.pid")
Start-ServiceWindow "API .NET" $dotnetCommand (Join-Path $runDirectory "dotnet.pid")

Write-Host "Vérification des services..." -ForegroundColor Cyan
$databaseReady = Wait-Endpoint "http://localhost:5294/api/health/database"
$aiReady = Wait-Endpoint "http://localhost:5294/api/health/ai"

if ($databaseReady -and $aiReady) {
    Write-Host "Projet prêt : SQL Server, FastAPI et API .NET sont connectés." -ForegroundColor Green
    Start-Process "http://localhost:5294/swagger"
} else {
    Write-Host "Un service n'est pas encore prêt. Consulte les deux fenêtres de logs." -ForegroundColor Yellow
    Write-Host "Swagger : http://localhost:5294/swagger"
}
