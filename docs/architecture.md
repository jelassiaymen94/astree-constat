# Architecture technique

## Objectif

La solution sépare l’interface utilisateur, les données et règles métier, le moteur de génération et la livraison des e-mails. L’API ASP.NET Core construit un contexte contrôlé à partir de SQL Server, FastAPI applique les règles de prompt et un fournisseur interchangeable produit le brouillon. Le gestionnaire doit relire le contenu et confirmer explicitement l’envoi : aucune génération n’est expédiée automatiquement.

## Vue opérationnelle

```text
Navigateur
    │
    ▼
React 19 / TypeScript / Vite
├── consultation, recherche et pagination
├── détail et contexte consolidé
├── génération, copie et historique
└── édition, aperçu et confirmation d’e-mail
    │ HTTP/JSON
    ▼
ASP.NET Core Web API (.NET 8)
├── ClaimsController / ClaimsService
├── ClaimGenerationService / AiGenerationClient ──> FastAPI
│                                                     ├── DeterministicProvider
│                                                     └── GroqProvider → GroqCloud
├── ClaimEmailsController / ClaimEmailService ─────> SMTP / Mailtrap Sandbox
├── gestion globale des erreurs
└── Entity Framework Core 8 ────────────────────────> SQL Server 2022
                                                      ├── données métier
                                                      ├── GenerationLogs
                                                      └── EmailLogs
```

## Frontend

Le frontend `frontend/` est une application React 19 et TypeScript construite avec Vite. Il propose :

- une liste paginée des sinistres avec recherche temporisée et filtres ;
- le détail consolidé du sinistre, de l’assuré, du contrat et du véhicule ;
- la sélection de `summary`, `letter` ou `response`, avec instruction facultative ;
- l’affichage, la copie et l’historique des brouillons ;
- un éditeur d’e-mail, un aperçu du modèle HTML ASTREE, une confirmation explicite et l’historique des envois.

`frontend/src/api.ts` centralise les appels Claims et Generation. `frontend/src/emailApi.ts` gère les appels d’e-mail. `VITE_API_BASE_URL` définit le préfixe public utilisé par ces clients HTTP. Lorsqu’il reste vide en développement, les requêtes relatives `/api` passent par le proxy Vite, dont la cible est configurée séparément avec `VITE_BACKEND_URL`.

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

### E-mails

`ClaimEmailsController` expose l’envoi et l’historique des e-mails d’un sinistre. `ClaimEmailService` :

1. contrôle l’existence du dossier et l’idempotence via `ClientRequestId` ;
2. choisit l’adresse du client ou une adresse fictive dérivée de `ClientId` ;
3. redirige éventuellement la livraison vers `EMAIL_DEMO_RECIPIENT` ;
4. assainit le HTML, applique le modèle ASTREE et produit une alternative texte ;
5. crée un journal `pending`, appelle `SmtpEmailSender`, puis enregistre `sent` ou `failed`.

`SmtpEmailSender` envoie un message `multipart/alternative` texte et HTML via Mailtrap Sandbox. Une confirmation `true` est obligatoire dans le contrat d’entrée ; l’envoi n’est jamais déclenché par la génération elle-même.

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
- `GenerationLogs` ;
- `EmailLogs`.

`GenerationLogs` journalise chaque tentative : type, instruction, contenu, modèle, version du prompt, durée, succès, erreur et date. Les types autorisés sont `summary`, `letter` et `response`.

`EmailLogs` conserve la clé d’idempotence, le sinistre et la génération associés, les destinataires logique et réel, le sujet, les versions HTML et texte, le statut, l’identifiant fournisseur, l’erreur éventuelle et les dates. `Clients.Email` est facultatif ; une adresse fictive est construite lorsque les données importées n’en contiennent pas.

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
├── génération et journalisation
└── envoi e-mail, persistance et assainissement HTML

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
- aucun envoi déclenché par la génération ;
- confirmation explicite obligatoire avant l’appel SMTP ;
- redirection vers une adresse contrôlée en mode démonstration ;
- HTML assaini avant rendu et livraison ;
- validation humaine obligatoire.

Restent hors périmètre de la V1 : authentification et rôles, relais e-mail de production, pièces jointes, bibliothèque d’assainissement HTML auditée, RAG sans documents métier disponibles, analyse PDF et fine-tuning.
