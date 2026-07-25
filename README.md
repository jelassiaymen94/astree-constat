# ASTREE Claims AI

Prototype d’assistant rédactionnel pour la gestion des sinistres automobiles. Le backend centralise les dossiers, construit un contexte structuré et génère des brouillons de synthèses, courriers et réponses. Toute génération reste soumise à une validation humaine.

## État du projet

### S3 — implémentée et validée

- sélection du fichier synthétique `donnees_assurance_tunisie2.xlsx` ;
- 5 252 sinistres analysés, 1 792 exclus et 3 460 retenus ;
- extraction reproductible vers des CSV UTF-8 ;
- SQL Server 2022 dans Docker et schéma Database First ;
- import .NET transactionnel et idempotent ;
- 2 048 clients, 2 179 contrats, 2 179 véhicules et 3 460 sinistres importés ;
- consultation paginée, filtres et contexte consolidé ;
- erreurs publiques uniformes avec `traceId` ;
- HTTP `400 INVALID_REQUEST`, `404 CLAIM_NOT_FOUND`, `502 AI_SERVICE_UNAVAILABLE`, `503 DATABASE_UNAVAILABLE` et `500 INTERNAL_ERROR`.

### Tests automatisés — validés

- projet xUnit séparé avec SQLite en mémoire ;
- 18 tests S3 couvrant services Claims, API, erreurs et import ;
- 5 tests S4A couvrant génération, historique, validation, sinistre absent et panne IA ;
- **23 tests réussis, 0 échec, 0 ignoré** ;
- exécution indépendante de Docker et de la base de développement.

### S4A — flux de génération implémenté et validé

- endpoint FastAPI `POST /api/v1/generate` ;
- types de brouillon `summary`, `letter` et `response` ;
- client HTTP typé entre ASP.NET Core et FastAPI ;
- endpoint `POST /api/claims/{claimId}/generate` ;
- endpoint `GET /api/claims/{claimId}/generations` ;
- persistance des succès et échecs dans `GenerationLogs` ;
- modèle actuel `deterministic-template`, prompt `1.0` ;
- `requiresHumanValidation: true` sur toutes les sorties ;
- validation manuelle HTTP `200` et historique SQL réussie.

### Prochaine étape — S4B

- intégrer un véritable fournisseur LLM derrière l’interface existante ;
- conserver le générateur déterministe pour les tests ;
- configurer les clés uniquement par variables d’environnement ou `user-secrets` ;
- versionner les prompts et conserver la validation humaine obligatoire.

## Architecture

```text
Swagger / client HTTP
        │
        ▼
ASP.NET Core Web API (.NET 8)
├── ClaimsController
├── ClaimsService
├── ClaimGenerationService
├── AiGenerationClient
├── gestion uniforme des erreurs
└── Entity Framework Core 8
        │
        ├──────────────> SQL Server 2022 / AstreeClaimsDb
        │                  └── GenerationLogs
        │
        └── HTTP/JSON ──> FastAPI
                            └── deterministic-template / futur LLM
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

### 2. FastAPI

```bash
cd ai-service
python -m venv .venv
source .venv/Scripts/activate
python -m pip install -r requirements.txt
python -m uvicorn app.main:app --reload --port 8000
```

### 3. Configurer et lancer .NET

Depuis `backend/AstreeClaims.Api` :

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Server=127.0.0.1,1433;Database=AstreeClaimsDb;User Id=sa;Password=<MOT_DE_PASSE>;TrustServerCertificate=True'
dotnet user-secrets set "AiService:BaseUrl" "http://localhost:8000"
dotnet run
```

Ne jamais copier littéralement `<MOT_DE_PASSE>` : le remplacer localement par le vrai secret.

## Tests automatisés

```bash
dotnet restore AstreeClaims.sln
dotnet test AstreeClaims.sln
```

Résultat attendu :

```text
Failed: 0, Passed: 23, Skipped: 0, Total: 23
```

## Endpoints disponibles

```http
GET  /api/health/database
GET  /api/health/ai
GET  /api/claims?page=1&pageSize=20
GET  /api/claims/{claimId}
GET  /api/claims/{claimId}/context
POST /api/claims/{claimId}/generate
GET  /api/claims/{claimId}/generations
POST /api/v1/generate                  # FastAPI interne
```

Filtres de la liste : `status`, `type` et `search`. Types de génération : `summary`, `letter` et `response`.

## Sécurité

- `.env`, données brutes, CSV générés et clés LLM restent locaux ;
- les secrets .NET utilisent `dotnet user-secrets` ;
- aucun secret ne doit être journalisé ou commité ;
- aucune stack trace ni aucun détail SQL dans les réponses publiques ;
- aucune décision ou aucun envoi automatique ;
- toutes les générations nécessitent une validation humaine.

## Documentation

- `docs/architecture.md`
- `docs/api-contracts.md`
- `docs/installation.md`
- `docs/data-dictionary.md`
- `docs/import-process.md`
- `docs/testing.md`
- `docs/journal-S3.md`
- `docs/journal-S4.md`

## Auteur

Aymen Jelassi — stage ASTREE ASSURANCES
