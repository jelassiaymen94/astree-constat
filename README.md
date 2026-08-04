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

### S4B — fournisseur Groq implémenté et validé localement

- fournisseur sélectionnable avec `LLM_PROVIDER=deterministic|groq` ;
- intégration asynchrone avec le SDK officiel `groq` ;
- prompts métier centralisés et versionnés `2.1` ;
- devise TND imposée explicitement, sans conversion ;
- erreurs Groq assainies : authentification, limite, capacité, timeout et indisponibilité ;
- mode déterministe conservé pour les tests reproductibles ;
- 14 tests Python réussis sans appel réseau ni clé réelle ;
- contrats JSON FastAPI/.NET inchangés ;
- flux `.NET → FastAPI → Groq → GenerationLogs` validé avec le modèle `llama-3.3-70b-versatile` ;
- persistance, prompt `2.1`, TND et validation humaine confirmés.


## Démarrage optimisé — une commande

Sous Windows, après avoir renseigné le fichier `.env` à la racine :

```powershell
.\start.cmd
```

Le lanceur charge le `.env`, démarre SQL Server, FastAPI et l’API .NET, vérifie les connexions puis ouvre Swagger. Il crée l’environnement Python si nécessaire et réinstalle les dépendances uniquement lorsqu’elles sont manquantes.

Pour tout arrêter sans supprimer les données SQL :

```powershell
.\stop.cmd
```

Les secrets sont centralisés dans un seul fichier `.env`. Ne jamais le commiter ni afficher son contenu.

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

## Démarrage manuel de dépannage

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

Sous Windows, la validation complète de S4 peut être rejouée en une commande :

```powershell
.\validate-s4.cmd
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
- `docs/validation-S4.md`

## Auteur

Aymen Jelassi — stage ASTREE ASSURANCES
