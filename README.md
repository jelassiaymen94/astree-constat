# ASTREE Claims AI

Prototype d’assistant rédactionnel pour la gestion des sinistres automobiles. Le backend centralise les dossiers et prépare un contexte structuré pour de futures synthèses, courriers et réponses assistées par LLM. Toute génération future restera soumise à une validation humaine.

## État du projet

### Implémenté et validé

- sélection du fichier synthétique `donnees_assurance_tunisie2.xlsx` ;
- 5 252 sinistres analysés, 1 792 exclus et 3 460 retenus ;
- extraction reproductible vers des CSV UTF-8 ;
- SQL Server 2022 dans Docker et schéma Database First ;
- import .NET transactionnel et idempotent ;
- 2 048 clients, 2 179 contrats, 2 179 véhicules et 3 460 sinistres importés ;
- contrôles SQL des relations, doublons et périodes contractuelles ;
- endpoints de santé SQL Server et FastAPI ;
- endpoints paginés de consultation et contexte consolidé ;
- format uniforme des erreurs ;
- validation HTTP `400` avec `INVALID_REQUEST` ;
- erreur HTTP `404` avec `CLAIM_NOT_FOUND` ;
- indisponibilité FastAPI HTTP `502` avec `AI_SERVICE_UNAVAILABLE` ;
- indisponibilité SQL HTTP `503` avec `DATABASE_UNAVAILABLE` ;
- erreur inattendue HTTP `500` avec `INTERNAL_ERROR` ;
- journalisation corrélée par `traceId`.

### Étape 5A — implémentée et validée

- projet xUnit séparé avec SQLite en mémoire ;
- 18 cas automatisés couvrant services Claims, API et import ;
- exécution indépendante de Docker et de la base de développement.

### Prévu plus tard

- intégration réelle du LLM en S4 ;
- templates de génération et `GenerationLogs` ;
- validation humaine des contenus générés.

## Architecture

```text
Swagger / client HTTP
        │
        ▼
ClaimsController
        │
        ▼
IClaimsService / ClaimsService
        │
        ▼
Entity Framework Core 8
        │
        ▼
SQL Server 2022

ASP.NET Core ──HTTP/JSON──> FastAPI ──> futur LLM
```

## Structure principale

```text
backend/AstreeClaims.Api/
├── Controllers/
├── Data/
├── DTOs/
├── ErrorHandling/
├── Exceptions/
├── Models/
├── Services/
└── Program.cs
tests/AstreeClaims.Api.Tests/
ai-service/
database/
data/
docs/
scripts/
```

## Démarrage rapide

### 1. SQL Server

```bash
docker compose up -d sqlserver
docker compose logs --tail=100 sqlserver
```

Attendre `SQL Server is now ready for client connections`.

### 2. Préparer les données

```bash
python -m venv .data-venv
source .data-venv/Scripts/activate
python -m pip install -r scripts/requirements-data.txt
python scripts/prepare_import_data.py --input data/raw/donnees_assurance_tunisie2.xlsx --output-dir data/processed
```

Résultat attendu : 2 048 clients, 2 179 contrats, 2 179 véhicules, 3 460 sinistres et 1 792 exclusions.

### 3. Configurer .NET

Depuis `backend/AstreeClaims.Api` :

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Server=127.0.0.1,1433;Database=AstreeClaimsDb;User Id=sa;Password=<MOT_DE_PASSE>;TrustServerCertificate=True'
dotnet user-secrets set "AiService:BaseUrl" "http://localhost:8000"
```

### 4. Importer

```bash
dotnet run -- --import-data --import-dir ../../data/processed
```

Relancer la commande : la deuxième exécution doit insérer zéro ligne.

### 5. Lancer l’API

```bash
dotnet build
dotnet run
```

## Tests automatisés

La suite n’utilise pas la base de développement :

```bash
dotnet test AstreeClaims.sln
```

Couverture optionnelle :

```bash
dotnet test AstreeClaims.sln --collect:"XPlat Code Coverage"
```

## Endpoints disponibles

```http
GET /api/health/database
GET /api/health/ai
GET /api/claims?page=1&pageSize=20
GET /api/claims/{claimId}
GET /api/claims/{claimId}/context
```

Filtres de la liste : `status`, `type` et `search`.

## Sécurité

- `.env`, les données brutes et les CSV générés restent locaux ;
- les secrets .NET utilisent `dotnet user-secrets` ;
- aucun secret ne doit être journalisé ou commité ;
- les réponses d’erreur publiques ne contiennent ni stack trace ni détail SQL ;
- les données utilisées sont synthétiques.

## Documentation

- `docs/architecture.md`
- `docs/api-contracts.md`
- `docs/installation.md`
- `docs/data-dictionary.md`
- `docs/import-process.md`
- `docs/testing.md`
- `docs/journal-S3.md`

## Auteur

Aymen Jelassi — stage ASTREE ASSURANCES
