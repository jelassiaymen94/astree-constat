# Installation et démarrage

Environnement de référence : Windows, Docker Desktop, .NET 8, Python et PowerShell.

## Prérequis

```powershell
docker --version
docker compose version
dotnet --version
python --version
node --version
npm --version
```

## Configuration unique

Copier `.env.example` vers `.env`, puis renseigner uniquement les secrets locaux. Le fichier `.env` racine centralise la configuration de Docker, .NET et FastAPI.

Variables principales. Les variables Mailtrap sont nécessaires uniquement pour tester l’envoi d’e-mails :

```dotenv
SQLSERVER_SA_PASSWORD="<MOT_DE_PASSE_SQL_FORT>"
ConnectionStrings__DefaultConnection="Server=127.0.0.1,1433;Database=AstreeClaimsDb;User Id=sa;Password=<MOT_DE_PASSE_SQL_FORT>;TrustServerCertificate=True"
AiService__BaseUrl="http://localhost:8000"
LLM_PROVIDER="groq"
GROQ_API_KEY="<CLE_GROQ>"

MAILTRAP_SMTP_HOST="<HOTE_MAILTRAP>"
MAILTRAP_SMTP_PORT="2525"
MAILTRAP_SMTP_USERNAME="<USERNAME_MAILTRAP>"
MAILTRAP_SMTP_PASSWORD="<PASSWORD_MAILTRAP>"
EMAIL_FROM_ADDRESS="sinistres-demo@astree.local"
EMAIL_FROM_NAME="ASTREE Assurances — Démonstration"
EMAIL_DEMO_MODE="true"
EMAIL_DEMO_RECIPIENT="demo@astree.local"
```

Ne jamais commiter, afficher ou journaliser `.env`.

## Mise à niveau de la base pour les e-mails

Sur une base créée avant l’ajout du module e-mail, exécuter une fois le script idempotent `database/upgrade-email.sql`. Il ajoute `Clients.Email`, crée `EmailLogs`, sa contrainte de statuts et ses index.

```powershell
docker cp .\database\upgrade-email.sql astree-sqlserver:/tmp/upgrade-email.sql
docker exec -it astree-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:SQLSERVER_SA_PASSWORD" -C -i /tmp/upgrade-email.sql
```

Selon l’image SQL Server installée, le client peut se trouver sous `/opt/mssql-tools/bin/sqlcmd`. Le script peut être rejoué sans recréer les objets existants.

## Démarrage recommandé — services backend

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

## Démarrage du frontend

Dans une seconde fenêtre PowerShell :

```powershell
.\frontend\start.cmd
```

Le script exige Node.js 20 ou supérieur, installe les dépendances uniquement si `node_modules` est absent, puis démarre Vite sur `http://localhost:5173`. Dans `frontend/.env`, `VITE_BACKEND_URL` définit la cible du proxy de développement `/api` et vaut par défaut `http://localhost:5294`. `VITE_API_BASE_URL`, vide par défaut, sert de préfixe public pour les appels directs lorsque le frontend n’utilise pas ce proxy.

Pour une compilation de contrôle sans démarrer le serveur :

```powershell
npm run build --prefix .\frontend
```

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
dotnet test .\AstreeClaims.sln -c Release
$env:PYTHONPATH = (Resolve-Path .\ai-service).Path
$env:LLM_PROVIDER = "deterministic"
.\ai-service\.venv\Scripts\python.exe -m pytest .\ai-service\tests -q
npm run build --prefix .\frontend
```

Les tests Python simulent Groq et ne consomment aucune requête externe.

## Validation manuelle S4

1. vérifier les deux endpoints de santé ;
2. charger le contexte de `CLM-3972B1FD` ;
3. tester `summary`, `letter` et `response` ;
4. contrôler `success`, `modelName`, `promptVersion`, `durationMs` et `requiresHumanValidation` ;
5. vérifier les lignes correspondantes dans `GenerationLogs` ;
6. confirmer que les montants restent en TND et qu’aucune décision automatique n’est produite.
