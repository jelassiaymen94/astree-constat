# Installation et démarrage

Ce guide décrit la mise en place locale du prototype sous Windows avec Git Bash, Docker Desktop et SQL Server Management Studio.

## 1. Prérequis

- Docker Desktop
- SQL Server Management Studio
- .NET 8 SDK
- Python 3
- Git
- Visual Studio Code

Vérification :

```bash
docker --version
docker compose version
dotnet --version
python --version
git --version
```

## 2. Configuration

Créer `.env` à partir du modèle :

```bash
cp .env.example .env
```

Définir un mot de passe SQL Server fort. Ne jamais ajouter `.env` à Git.

## 3. SQL Server

Démarrer le conteneur :

```bash
docker compose up -d
docker compose ps
```

Consulter les logs :

```bash
docker compose logs -f sqlserver
```

Attendre :

```text
SQL Server is now ready for client connections
```

Dans SSMS :

- serveur : `localhost,1433` ;
- authentification : SQL Server Authentication ;
- utilisateur : `sa` ;
- certificat serveur approuvé.

Ouvrir et exécuter `database/schema.sql`.

## 4. API ASP.NET Core

Restaurer les dépendances :

```bash
dotnet tool restore
dotnet restore
```

Configurer la connexion dans le dossier `backend/AstreeClaims.Api` :

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Server=localhost,1433;Database=AstreeClaimsDb;User Id=sa;Password=MOT_DE_PASSE;TrustServerCertificate=True'
dotnet user-secrets set "AiService:BaseUrl" "http://localhost:8000"
```

Les guillemets simples évitent l’interprétation du caractère `!` par Git Bash.

Lancer :

```bash
cd backend/AstreeClaims.Api
dotnet build
dotnet run
```

## 5. FastAPI

Créer l’environnement virtuel si nécessaire :

```bash
python -m venv ai-service/.venv
```

Activer et installer :

```bash
cd ai-service
source .venv/Scripts/activate
python -m pip install --upgrade pip
python -m pip install -r requirements.txt
```

Lancer :

```bash
uvicorn app.main:app --reload --port 8000
```

Documentation interactive : `http://localhost:8000/docs`.

## 6. Tests

Avec FastAPI et .NET actifs :

```http
GET /api/health/database
GET /api/health/ai
GET http://localhost:8000/health
```

Les deux connexions doivent être signalées comme opérationnelles.

## 7. Arrêt

Arrêter .NET et FastAPI avec `Ctrl + C` dans leurs terminaux.

Arrêter SQL Server :

```bash
docker compose down
```

Les données SQL restent conservées dans le volume Docker.

## 8. Problèmes fréquents

### Port SQL occupé

Remplacer `1433:1433` par `1434:1433` dans `docker-compose.yml`, puis utiliser `localhost,1434`.

### `dotnet-ef` non trouvé

```bash
dotnet tool restore
dotnet tool run dotnet-ef --version
```

### Erreur Git Bash avec `!`

Entourer toute la chaîne de connexion avec des guillemets simples.

### FastAPI inaccessible depuis .NET

Vérifier que FastAPI écoute sur `http://localhost:8000` et que son terminal reste actif.
