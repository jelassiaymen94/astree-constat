# ASTREE Claims AI

Prototype d’assistant rédactionnel pour la gestion des sinistres automobiles. Le backend centralise les dossiers, construit un contexte structuré et génère des brouillons de synthèses, courriers et réponses. Toute génération reste soumise à une validation humaine.

## État du projet

### S3 — implémentée et validée

- 5 252 sinistres analysés, 1 792 exclus et 3 460 retenus ;
- import SQL Server transactionnel et idempotent ;
- 2 048 clients, 2 179 contrats, 2 179 véhicules et 3 460 sinistres importés ;
- consultation paginée, filtres, contexte consolidé et erreurs publiques uniformes.

### S4A — flux de génération validé

- `POST /api/claims/{claimId}/generate` et `GET /api/claims/{claimId}/generations` ;
- `POST /api/v1/generate` dans FastAPI ;
- types `summary`, `letter` et `response` ;
- historique des succès et échecs dans `GenerationLogs` ;
- générateur `deterministic-template`, prompt `1.0` ;
- 23 tests .NET réussis ;
- `requiresHumanValidation: true` sur toutes les sorties publiques.

### S4B — fournisseur Groq implémenté

- fournisseur sélectionnable avec `LLM_PROVIDER=deterministic|groq` ;
- intégration asynchrone avec le SDK officiel `groq` ;
- prompts métier centralisés et versionnés `2.1` ;
- devise TND imposée explicitement, sans conversion ;
- erreurs Groq assainies : authentification, limite, capacité, timeout et indisponibilité ;
- mode déterministe conservé pour les tests reproductibles ;
- 14 tests Python ajoutés sans appel réseau ni clé réelle ;
- contrats JSON FastAPI/.NET inchangés.

La validation manuelle avec une vraie clé Groq reste obligatoire avant de déclarer S4B validée en environnement local.

## Architecture

```text
Swagger / client HTTP
        │
        ▼
ASP.NET Core Web API (.NET 8)
├── ClaimsController
├── ClaimGenerationService
├── AiGenerationClient
└── EF Core 8 ──> SQL Server 2022 / GenerationLogs
        │
        └── HTTP/JSON ──> FastAPI
                            ├── deterministic-template (tests)
                            └── GroqProvider / GroqCloud (réel)
```

## Démarrage rapide

### 1. SQL Server

```bash
docker compose up -d sqlserver
```

### 2. FastAPI en mode déterministe

```bash
cd ai-service
python -m venv .venv
source .venv/Scripts/activate
python -m pip install -r requirements.txt
export LLM_PROVIDER=deterministic
python -m uvicorn app.main:app --reload --port 8000
```

### 3. FastAPI avec Groq

Créer une clé dans la console Groq, puis la définir uniquement dans l’environnement local :

```bash
export LLM_PROVIDER=groq
export GROQ_API_KEY="<CLE_GROQ_LOCALE>"
export GROQ_MODEL="llama-3.3-70b-versatile"
export GROQ_TEMPERATURE=0.2
export GROQ_MAX_TOKENS=1000
export GROQ_TIMEOUT_SECONDS=20
python -m uvicorn app.main:app --reload --port 8000
```

Ne jamais commiter, afficher ou journaliser `GROQ_API_KEY`.

### 4. API .NET

Depuis `backend/AstreeClaims.Api` :

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Server=127.0.0.1,1433;Database=AstreeClaimsDb;User Id=sa;Password=<MOT_DE_PASSE>;TrustServerCertificate=True'
dotnet user-secrets set "AiService:BaseUrl" "http://localhost:8000"
dotnet run
```

## Tests automatisés

```bash
# .NET — 23 tests
dotnet restore AstreeClaims.sln
dotnet test AstreeClaims.sln

# FastAPI — 14 tests, sans réseau
cd ai-service
python -m pytest -q
```

## Endpoints

```http
GET  /api/health/database
GET  /api/health/ai
GET  /api/claims?page=1&pageSize=20
GET  /api/claims/{claimId}
GET  /api/claims/{claimId}/context
POST /api/claims/{claimId}/generate
GET  /api/claims/{claimId}/generations
GET  /health                           # FastAPI
POST /api/v1/generate                  # FastAPI interne
```

Le contrat de réponse FastAPI conserve `content`, `modelName`, `promptVersion` et `durationMs`. `GenerationLogs` conserve le modèle, la version du prompt, la durée, le succès et l’erreur éventuelle.

## Sécurité

- `.env`, données brutes, CSV générés et clés LLM restent locaux ;
- aucune erreur brute Groq, stack trace ou information SQL n’est exposée publiquement ;
- le contexte et l’instruction utilisateur sont traités comme des données non fiables ;
- aucune décision d’indemnisation ni aucun envoi automatique ;
- toutes les générations nécessitent une validation humaine.

## Documentation

- `docs/architecture.md`
- `docs/api-contracts.md`
- `docs/installation.md`
- `docs/testing.md`
- `docs/journal-S4.md`

## Auteur

Aymen Jelassi — stage ASTREE ASSURANCES
