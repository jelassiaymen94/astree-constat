# Architecture technique

## Objectif

La solution sépare les données et règles métier du moteur de génération. L’API ASP.NET Core construit un contexte contrôlé à partir de SQL Server, FastAPI applique les règles de prompt et un fournisseur interchangeable produit le brouillon. Aucune génération n’est envoyée automatiquement et toute sortie exige une validation humaine.

## Vue opérationnelle

```text
Swagger / client HTTP
        │
        ▼
ASP.NET Core Web API (.NET 8)
├── ClaimsController
├── ClaimsService
├── ClaimGenerationService
├── AiGenerationClient
├── gestion globale des erreurs
└── Entity Framework Core 8
        │                         │
        ▼                         └── HTTP/JSON ──> FastAPI
SQL Server 2022                                      ├── DeterministicProvider
├── données métier                                   └── GroqProvider → GroqCloud
└── GenerationLogs
```

## Backend métier

### Contrôleur

`ClaimsController` expose :

```http
GET  /api/claims
GET  /api/claims/{claimId}
GET  /api/claims/{claimId}/context
POST /api/claims/{claimId}/generate
GET  /api/claims/{claimId}/generations
```

Les entités Entity Framework ne sont jamais retournées directement.

### Services

`ClaimsService` centralise la pagination, les filtres, la recherche et la construction du contexte consolidé. Les lectures utilisent `AsNoTracking()` et un tri stable.

`ClaimGenerationService` orchestre le flux de génération :

```text
ClaimsController
→ ClaimGenerationService
→ ClaimsService / contexte consolidé
→ AiGenerationClient
→ FastAPI /api/v1/generate
→ GenerationLogs
```

`AiGenerationClient` maintient le contrat HTTP entre .NET et FastAPI et convertit les indisponibilités du service IA en erreur publique assainie.

## Import S3

`DataImportService` lit les CSV préparés, vérifie les en-têtes, valeurs obligatoires, dates, montants, identifiants, relations et cohérence temporelle. Une transaction couvre l’insertion des clients, contrats, véhicules et sinistres. Les identifiants existants sont ignorés afin de rendre l’import idempotent.

```bash
dotnet run --project backend/AstreeClaims.Api -- --import-data --import-dir data/processed
```

## SQL Server

La base `AstreeClaimsDb` contient :

- `Clients` ;
- `Contrats` ;
- `Vehicules` ;
- `Sinistres` ;
- `GenerationLogs`.

`GenerationLogs` journalise chaque tentative : type, instruction, contenu, modèle, version du prompt, durée, succès, erreur et date. Les types autorisés sont `summary`, `letter` et `response`.

## FastAPI et fournisseurs

FastAPI expose :

```http
GET  /health
POST /api/v1/generate
```

L’abstraction de fournisseur permet deux modes :

```text
GenerationProvider
├── DeterministicProvider  # tests reproductibles, sans réseau
└── GroqProvider           # génération réelle, asynchrone
```

Le fournisseur est choisi avec `LLM_PROVIDER=deterministic|groq`. Les prompts Groq sont centralisés dans `ai-service/app/prompts.py` et utilisent la version `2.1`.

## Gestion des erreurs

```text
Validation ASP.NET             → 400 INVALID_REQUEST
ClaimNotFoundException         → 404 CLAIM_NOT_FOUND
AiServiceUnavailableException  → 502 AI_SERVICE_UNAVAILABLE
DbException                    → 503 DATABASE_UNAVAILABLE
Exception inattendue           → 500 INTERNAL_ERROR
```

Chaque erreur publique contient un `traceId` sans stack trace, secret, chaîne de connexion ni message brut du fournisseur.

## Tests

```text
.NET / xUnit
├── services Claims
├── API et erreurs publiques
├── import transactionnel
└── génération et journalisation

Python / pytest
├── contrat FastAPI
├── summary, letter et response
├── fournisseur déterministe
├── fournisseur Groq simulé
└── sécurité des prompts et erreurs
```

Les tests .NET utilisent SQLite en mémoire. Les tests Python n’effectuent aucun appel Groq réel.

## Sécurité et limites

- secrets centralisés dans `.env`, ignoré par Git ;
- données de démonstration synthétiques ;
- montants explicitement conservés en TND ;
- contexte et instruction utilisateur traités comme données non fiables ;
- aucune décision automatique d’indemnisation ;
- aucun envoi automatique ;
- validation humaine obligatoire.

Restent hors périmètre de la V1 : RAG sans documents métier disponibles, analyse PDF, fine-tuning et frontend complet.
