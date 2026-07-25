# Architecture technique

## Objectif

La V1 sépare les données et règles métier .NET de la future génération LLM dans FastAPI. La solution reste simple, démontrable et adaptée à un stage de six semaines.

## Vue opérationnelle

```text
Swagger / client HTTP
        │
        ▼
ASP.NET Core Web API (.NET 8)
├── ClaimsController
├── DTOs
├── ClaimsService
├── gestion globale des erreurs
├── importeur de données
└── Entity Framework Core 8
        │
        ▼
SQL Server 2022 / AstreeClaimsDb

ASP.NET Core ──HTTP/JSON──> FastAPI
                                  └── futur LLM en S4
```

## Backend métier

### Contrôleur

`ClaimsController` expose :

```http
GET /api/claims
GET /api/claims/{claimId}
GET /api/claims/{claimId}/context
```

Il ne retourne jamais directement les entités Entity Framework.

### DTOs

- `ClaimDto`
- `CustomerDto`
- `ContractDto`
- `VehicleDto`
- `ClaimContextDto`
- `ClaimListQueryDto`
- `PagedResultDto<T>`
- `ApiErrorDto`

### Service

`IClaimsService` et `ClaimsService` centralisent :

- pagination ;
- filtres ;
- recherche par identifiant ;
- projections EF vers les DTOs ;
- contexte consolidé.

Les lectures utilisent `AsNoTracking()` et un tri stable par date décroissante puis identifiant.

## Gestion des erreurs — 4B

`GlobalExceptionHandler` utilise `IExceptionHandler` de .NET 8.

```text
ClaimNotFoundException       → 404 CLAIM_NOT_FOUND
AiServiceUnavailableException → 502 AI_SERVICE_UNAVAILABLE
DbException                   → 503 DATABASE_UNAVAILABLE
Exception                     → 500 INTERNAL_ERROR
Validation ASP.NET     → 400 INVALID_REQUEST
```

Chaque réponse contient un `traceId`. Les erreurs serveur sont journalisées sans exposer de stack trace, secret ou chaîne de connexion dans la réponse HTTP.

## Import S3

`DataImportService` lit les CSV préparés et valide :

- en-têtes ;
- valeurs obligatoires ;
- dates et montants ;
- unicité des identifiants ;
- relations ;
- cohérence temporelle.

L’ordre d’insertion est : clients, contrats, véhicules, sinistres. Une transaction couvre l’ensemble. Les identifiants existants sont ignorés pour assurer l’idempotence.

L’import est déclenché manuellement :

```bash
dotnet run -- --import-data --import-dir ../../data/processed
```

## Architecture des tests — 5A

Le projet `tests/AstreeClaims.Api.Tests` référence l’API sans modifier son stockage de production.

```text
xUnit
├── ClaimsServiceTests ──> DbContext SQLite en mémoire
├── ClaimsApiTests ──────> WebApplicationFactory<Program>
└── DataImportServiceTests ──> CSV temporaires + transaction SQLite
```

`ClaimsApiFactory` remplace l’enregistrement SQL Server par une connexion SQLite en mémoire ouverte pendant la durée des tests. Un petit jeu cohérent de clients, contrats, véhicules et sinistres est créé avec `EnsureCreated`. L’environnement `Testing` désactive uniquement la redirection HTTPS afin que le client d’intégration appelle directement l’application en mémoire.

Cette séparation garantit des tests rapides, reproductibles et indépendants de Docker et de la base `AstreeClaimsDb`.

## SQL Server

Tables :

- `Clients`
- `Contrats`
- `Vehicules`
- `Sinistres`
- `GenerationLogs`

`GenerationLogs` est préparée mais sera utilisée avec l’intégration LLM en S4.

## FastAPI

FastAPI expose actuellement `/health`. Il n’accède pas directement à SQL Server. L’appel réel au LLM est hors périmètre S3.

## Sécurité

- secrets SQL dans `.env` pour Docker et `dotnet user-secrets` pour .NET ;
- aucune clé dans Git ;
- données synthétiques ;
- journalisation sans secret ;
- validation humaine obligatoire pour toute future génération.

## Hors périmètre S3

- RAG ;
- analyse PDF ;
- fine-tuning ;
- frontend complet ;
- envoi automatique ;
- décision automatique d’indemnisation.
