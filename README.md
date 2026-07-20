# ASTREE Claims AI

Prototype d’assistant rédactionnel basé sur l’IA générative pour la gestion des sinistres automobiles.

Le projet vise à produire, à partir des données d’un dossier, des **synthèses**, des **courriers de statut** et des **réponses contextualisées** destinées à assister les gestionnaires. Chaque contenu généré reste soumis à une validation humaine avant utilisation.

## Objectifs

- Réduire le temps consacré à la rédaction.
- Standardiser les communications adressées aux assurés.
- Améliorer la cohérence et la qualité rédactionnelle.
- Centraliser le contexte utile d’un dossier sinistre.
- Conserver une trace des générations effectuées.

## Cas d’usage de la V1

1. **Synthèse de dossier** : assuré, contrat, véhicule, circonstances, montants et statut.
2. **Courrier de statut** : réception, expertise, clôture, indemnisation ou refus.
3. **Réponse contextualisée** : réponse à une question saisie par le gestionnaire, fondée sur les données disponibles.

## Architecture

```text
Swagger / interface de démonstration
                │
                ▼
      ASP.NET Core Web API
         │              │
         ▼              ▼
    SQL Server     FastAPI / Python
                        │
                        ▼
                       LLM
```

- **ASP.NET Core (.NET 8)** : accès aux données, règles métier, orchestration et journalisation.
- **SQL Server** : clients, contrats, véhicules, sinistres et historique des générations.
- **FastAPI** : construction des prompts, appel du LLM et validation des réponses.

## Technologies

- .NET 8 et ASP.NET Core Web API
- Entity Framework Core 8
- Python et FastAPI
- SQL Server 2022
- Docker Compose
- Swagger / OpenAPI
- Git

## Structure du repository

```text
astree-claims-ai/
├── backend/
│   └── AstreeClaims.Api/
├── ai-service/
│   ├── app/
│   └── requirements.txt
├── database/
│   └── schema.sql
├── docs/
│   ├── architecture.md
│   ├── api-contracts.md
│   ├── installation.md
│   └── journal-2026-07-20.md
├── .config/
│   └── dotnet-tools.json
├── .env.example
├── .gitignore
├── AstreeClaims.sln
├── docker-compose.yml
└── README.md
```

## Données retenues

Le prototype utilise un jeu de données synthétique d’assurance automobile tunisienne.

Après contrôle :

- 5 252 sinistres analysés ;
- 1 792 sinistres exclus pour incohérence temporelle ;
- 3 460 sinistres valides retenus ;
- aucune relation cassée entre les clients, contrats, véhicules et sinistres retenus.

Les différents jeux de données fournis ne sont pas fusionnés, car leurs identifiants et leurs structures ne sont pas compatibles.

## Démarrage rapide

### Prérequis

- Docker Desktop
- SQL Server Management Studio
- .NET 8 SDK
- Python 3
- Git

### 1. Démarrer SQL Server

```bash
docker compose up -d
docker compose ps
```

Exécuter ensuite `database/schema.sql` dans SQL Server Management Studio.

### 2. Restaurer .NET

```bash
dotnet tool restore
dotnet restore
```

Configurer localement la connexion avec `dotnet user-secrets` ; aucun mot de passe ne doit être ajouté au repository.

### 3. Démarrer FastAPI

Dans Git Bash sous Windows :

```bash
cd ai-service
source .venv/Scripts/activate
uvicorn app.main:app --reload --port 8000
```

Documentation interactive : `http://localhost:8000/docs`.

### 4. Démarrer l’API .NET

Dans un deuxième terminal :

```bash
cd backend/AstreeClaims.Api
dotnet run
```

Ouvrir ensuite l’URL Swagger affichée dans le terminal.

## Vérifications disponibles

### SQL Server

```http
GET /api/health/database
```

Résultat attendu :

```json
{
  "database": "AstreeClaimsDb",
  "connected": true
}
```

### FastAPI

```http
GET http://localhost:8000/health
```

### Communication .NET vers FastAPI

```http
GET /api/health/ai
```

La réponse doit contenir `"connected": true`.

## Sécurité

- `.env` et les secrets locaux sont exclus de Git.
- Les clés LLM seront fournies par variables d’environnement.
- Les mots de passe ne sont jamais écrits dans la documentation.
- Les données actuelles sont synthétiques.
- Toute production de l’IA doit être validée par un gestionnaire.

## Périmètre exclu de la V1

- RAG et base vectorielle
- Analyse de documents PDF
- Fine-tuning
- Envoi automatique de courriers
- Décision automatique d’indemnisation
- Interface frontend complète

## État du projet

- [x] Analyse et sélection des données
- [x] Architecture technique
- [x] Schéma SQL Server
- [x] API ASP.NET Core initialisée
- [x] Entity Framework Core connecté
- [x] FastAPI initialisé
- [x] Communication .NET vers FastAPI validée
- [ ] Importation des 3 460 dossiers valides
- [ ] Endpoints métier
- [ ] Intégration du LLM
- [ ] Templates de génération
- [ ] Tests de qualité

## Auteur

**Aymen Jelassi** — Stage ASTREE ASSURANCES
