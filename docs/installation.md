# Installation et démarrage

Environnement de référence : Windows, Docker Desktop, .NET 8, Python et PowerShell.

## Prérequis

```powershell
docker --version
docker compose version
dotnet --version
python --version
```

## Configuration unique

Copier `.env.example` vers `.env`, puis renseigner uniquement les secrets locaux. Le fichier `.env` racine centralise la configuration de Docker, .NET et FastAPI.

Variables minimales :

```dotenv
SQLSERVER_SA_PASSWORD="<MOT_DE_PASSE_SQL_FORT>"
ConnectionStrings__DefaultConnection="Server=127.0.0.1,1433;Database=AstreeClaimsDb;User Id=sa;Password=<MOT_DE_PASSE_SQL_FORT>;TrustServerCertificate=True"
AiService__BaseUrl="http://localhost:8000"
LLM_PROVIDER="groq"
GROQ_API_KEY="<CLE_GROQ>"
```

Ne jamais commiter, afficher ou journaliser `.env`.

## Démarrage recommandé — une commande

Depuis la racine :

```powershell
.\start.cmd
```

Le lanceur :

1. charge `.env` dans le processus ;
2. contrôle les variables obligatoires ;
3. crée l’environnement Python s’il est absent ;
4. vérifie et installe les dépendances Python manquantes ;
5. démarre SQL Server, FastAPI et l’API .NET ;
6. contrôle `/api/health/database` et `/api/health/ai` ;
7. ouvre Swagger.

Swagger : `http://localhost:5294/swagger`.

## Arrêt

```powershell
.\stop.cmd
```

Cette commande arrête les services sans supprimer le volume SQL. Ne jamais utiliser `docker compose down -v` sauf volonté explicite de supprimer les données.

## Démarrage manuel de dépannage

### SQL Server

```powershell
docker compose up -d sqlserver
docker compose logs --tail=100 sqlserver
```

### FastAPI

```powershell
python -m venv .\ai-service\.venv
.\ai-service\.venv\Scripts\python.exe -m pip install -r .\ai-service\requirements.txt
.\ai-service\.venv\Scripts\python.exe -m uvicorn app.main:app --app-dir ai-service --port 8000
```

FastAPI charge automatiquement le `.env` situé à la racine du projet.

### API .NET

```powershell
dotnet run --project .\backend\AstreeClaims.Api --launch-profile http
```

## Tests automatisés

```powershell
dotnet restore .\AstreeClaims.sln
dotnet test .\AstreeClaims.sln
.\ai-service\.venv\Scripts\python.exe -m pytest .\ai-service\tests -q
```

Les tests Python simulent Groq et ne consomment aucune requête externe.

## Validation manuelle S4

1. vérifier les deux endpoints de santé ;
2. charger le contexte de `CLM-3972B1FD` ;
3. tester `summary`, `letter` et `response` ;
4. contrôler `success`, `modelName`, `promptVersion`, `durationMs` et `requiresHumanValidation` ;
5. vérifier les lignes correspondantes dans `GenerationLogs` ;
6. confirmer que les montants restent en TND et qu’aucune décision automatique n’est produite.
