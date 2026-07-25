# Installation et démarrage

Environnement : Windows, Git Bash, Docker Desktop, SSMS, VS Code, .NET 8 et Python.

## Prérequis

```bash
docker --version
docker compose version
dotnet --version
python --version
git --version
```

## Configuration Docker

Créer `.env` à la racine avec uniquement des variables `NOM=valeur`. Ne jamais y placer une commande `dotnet user-secrets`.

```dotenv
SQLSERVER_SA_PASSWORD=<MOT_DE_PASSE_FORT>
AI_SERVICE_BASE_URL=http://localhost:8000
```

Valider :

```bash
docker compose config --quiet
```

## SQL Server

```bash
docker compose up -d sqlserver
docker compose ps
docker compose logs --tail=100 sqlserver
```

Attendre `SQL Server is now ready for client connections`.

Connexion SSMS : serveur `127.0.0.1,1433`, authentification SQL, utilisateur `sa`, certificat serveur approuvé.

Exécuter `database/schema.sql` uniquement lors de l’initialisation. Le script de tables n’est pas destiné à être relancé sur une base déjà créée.

## Préparation des données

Depuis la racine :

```bash
python -m venv .data-venv
source .data-venv/Scripts/activate
python -m pip install --upgrade pip
python -m pip install -r scripts/requirements-data.txt
python scripts/prepare_import_data.py --input data/raw/donnees_assurance_tunisie2.xlsx --output-dir data/processed
```

Résultat attendu :

```text
Clients   : 2048
Contrats  : 2179
Vehicules : 2179
Sinistres : 3460
Exclus    : 1792
```

## Configuration .NET

```bash
cd backend/AstreeClaims.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Server=127.0.0.1,1433;Database=AstreeClaimsDb;User Id=sa;Password=<MOT_DE_PASSE>;TrustServerCertificate=True'
dotnet user-secrets set "AiService:BaseUrl" "http://localhost:8000"
```

Ne pas capturer ni partager la sortie de `dotnet user-secrets list`.

## Compilation

```bash
dotnet restore
dotnet build
```

## Tests automatisés

Les tests requièrent uniquement le SDK .NET 8. SQL Server, Docker et FastAPI peuvent rester arrêtés. Depuis la racine :

```bash
dotnet restore
dotnet test AstreeClaims.sln
```

Résultat attendu pour le bloc 5A : 18 tests réussis. Pour produire un fichier de couverture :

```bash
dotnet test AstreeClaims.sln --collect:"XPlat Code Coverage"
```

Les résultats sont créés sous `tests/AstreeClaims.Api.Tests/TestResults/` et ne doivent pas être commités.

## Import SQL

```bash
dotnet run -- --import-data --import-dir ../../data/processed
```

Première exécution : toutes les lignes sont insérées. Deuxième exécution : `insertedRows` doit être égal à zéro pour chaque table.

Dans SSMS, exécuter `database/verify-import.sql`. Résultats attendus : 2 048 clients, 2 179 contrats, 2 179 véhicules, 3 460 sinistres, aucun doublon et tous les contrôles relationnels à zéro.

## FastAPI

```bash
cd ../../ai-service
python -m venv .venv
source .venv/Scripts/activate
python -m pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```

## API .NET

Dans un autre terminal :

```bash
cd backend/AstreeClaims.Api
dotnet run
```

Tester Swagger avec l’URL affichée, puis :

```http
GET /api/health/database
GET /api/health/ai
GET /api/claims?page=1&pageSize=5
```

## Arrêt

```bash
docker compose stop sqlserver
```

Redémarrage :

```bash
docker compose start sqlserver
```

Ne pas utiliser `docker compose down -v`, car `-v` supprime le volume de données.

## Incidents connus

- erreur Compose `unexpected character` : retirer les commandes shell de `.env` ;
- `ToHashSetAsync` absent : utiliser `ToListAsync(...).ToHashSet(...)` ;
- alias SQL `RowCount` : utiliser `TotalRows` ;
- erreur avec `!` dans Git Bash : entourer la chaîne passée à `dotnet user-secrets` de guillemets simples.
