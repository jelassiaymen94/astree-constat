# Installation et démarrage

Environnement de référence : Windows, Git Bash, Docker Desktop, SSMS, VS Code, .NET 8 et Python 3.12.

## Prérequis

```bash
docker --version
docker compose version
dotnet --version
python --version
git --version
```

## SQL Server

Créer `.env` à la racine avec un vrai mot de passe local, puis démarrer SQL Server :

```dotenv
SQLSERVER_SA_PASSWORD=<MOT_DE_PASSE_FORT>
AI_SERVICE_BASE_URL=http://localhost:8000
```

```bash
docker compose config --quiet
docker compose up -d sqlserver
docker compose logs --tail=100 sqlserver
```

Ne pas relancer `database/schema.sql` sur une base déjà initialisée.

## Configuration .NET

```bash
cd backend/AstreeClaims.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Server=127.0.0.1,1433;Database=AstreeClaimsDb;User Id=sa;Password=<MOT_DE_PASSE>;TrustServerCertificate=True'
dotnet user-secrets set "AiService:BaseUrl" "http://localhost:8000"
```

Ne pas capturer ni partager la sortie de `dotnet user-secrets list`.

## FastAPI

```bash
cd ai-service
python -m venv .venv
source .venv/Scripts/activate
python -m pip install -r requirements.txt
```

### Mode déterministe

Ce mode ne requiert ni réseau ni clé et doit être utilisé pour les tests :

```bash
export LLM_PROVIDER=deterministic
python -m uvicorn app.main:app --reload --port 8000
```

### Mode Groq

Créer une clé sur `https://console.groq.com/keys`, puis définir les variables dans le terminal ou dans un fichier `.env` local ignoré par Git :

```bash
export LLM_PROVIDER=groq
export GROQ_API_KEY="<CLE_GROQ_LOCALE>"
export GROQ_MODEL="llama-3.3-70b-versatile"
export GROQ_TEMPERATURE=0.2
export GROQ_MAX_TOKENS=1000
export GROQ_TIMEOUT_SECONDS=20
python -m uvicorn app.main:app --reload --port 8000
```

`GROQ_API_KEY` ne doit jamais être ajoutée à Git, copiée dans la documentation ou affichée dans les logs.

## Tests automatisés

Depuis la racine :

```bash
dotnet restore AstreeClaims.sln
dotnet test AstreeClaims.sln
```

Depuis `ai-service` :

```bash
export LLM_PROVIDER=deterministic
python -m pytest -q
```

Les tests Python mockent le client Groq et ne consomment aucune requête externe.

## Validation manuelle Groq

1. démarrer SQL Server ;
2. démarrer FastAPI avec `LLM_PROVIDER=groq` ;
3. démarrer l’API .NET ;
4. appeler `POST /api/claims/{claimId}/generate` ;
5. vérifier que la sortie reste factuelle et indique qu’elle est un brouillon ;
6. contrôler `modelName`, `promptVersion`, `durationMs` et `requiresHumanValidation` ;
7. vérifier la ligne correspondante dans `GenerationLogs`.

## Arrêt

```bash
docker compose stop sqlserver
```

Ne pas utiliser `docker compose down -v`, car `-v` supprime le volume de données.
