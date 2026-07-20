# Architecture technique

## 1. Objectif

L’architecture doit permettre de générer des contenus métier à partir d’un dossier sinistre tout en séparant clairement :

- la gestion des données et des règles métier ;
- la génération par IA ;
- le stockage et l’audit des résultats.

La V1 privilégie une solution simple, démontrable et compatible avec la durée du stage.

## 2. Vue d’ensemble

```text
┌──────────────────────────────┐
│ Swagger / interface de démo │
└──────────────┬───────────────┘
               │ HTTP
               ▼
┌──────────────────────────────┐
│ ASP.NET Core Web API (.NET 8)│
│ - dossiers et règles métier │
│ - contexte consolidé        │
│ - orchestration             │
│ - journalisation            │
└───────────┬──────────┬───────┘
            │          │ HTTP/JSON
            ▼          ▼
┌───────────────┐  ┌──────────────────┐
│  SQL Server   │  │ FastAPI / Python │
│ données + logs│  │ prompts + LLM    │
└───────────────┘  └────────┬─────────┘
                            ▼
                           LLM
```

## 3. Flux principal

1. Le gestionnaire sélectionne un sinistre et un type de génération.
2. L’API .NET charge le client, le contrat, le véhicule et le sinistre.
3. Elle valide le dossier et construit un contexte JSON consolidé.
4. Elle appelle le service FastAPI.
5. FastAPI sélectionne le template et construit le prompt.
6. Le LLM produit une proposition de texte.
7. L’API .NET journalise la demande et le résultat.
8. Le gestionnaire vérifie le contenu avant utilisation.

## 4. Responsabilités

### ASP.NET Core Web API

- Exposer les endpoints métier.
- Accéder à SQL Server avec Entity Framework Core.
- Appliquer les validations métier.
- Construire le contexte d’un dossier.
- Appeler FastAPI avec `HttpClient`.
- Gérer les erreurs et délais d’attente.
- Enregistrer les générations dans `GenerationLogs`.

### FastAPI

- Recevoir un contexte structuré.
- Sélectionner le template selon le cas d’usage.
- Construire le prompt système et le prompt utilisateur.
- Appeler un fournisseur LLM configurable.
- Retourner le contenu, le modèle, la version du prompt et les avertissements.

FastAPI n’accède pas directement à SQL Server dans la V1.

### SQL Server

| Table | Rôle |
|---|---|
| `Clients` | Identité minimale de l’assuré |
| `Contrats` | Couverture et période contractuelle |
| `Vehicules` | Informations du véhicule assuré |
| `Sinistres` | Circonstances, montants et statut |
| `GenerationLogs` | Historique et audit des générations |

## 5. Règles de conception

- La date du sinistre doit appartenir à la période du contrat.
- Les montants doivent être positifs ou nuls.
- Le LLM ne décide jamais de l’indemnisation.
- Une information absente ne doit pas être inventée.
- Chaque génération doit être associée à un sinistre.
- Toute sortie doit être validée par un gestionnaire.

## 6. Stratégie LLM

Le fournisseur LLM est encapsulé afin de pouvoir utiliser une API compatible OpenAI, Azure OpenAI ou un modèle local sans modifier la logique métier.

Paramètres initiaux :

- langue : français ;
- température recommandée : `0.2` ;
- version de prompt journalisée ;
- délai d’attente contrôlé ;
- réponse structurée avec avertissements.

## 7. Sécurité

- Secrets stockés dans `.env` ou `dotnet user-secrets`.
- `.env` exclu de Git.
- Aucun mot de passe dans le code ou la documentation.
- Journalisation sans clé API.
- Données synthétiques pendant le prototypage.
- Réévaluation obligatoire avant utilisation de données réelles.

## 8. Hors périmètre V1

- RAG et base vectorielle
- Analyse automatique de PDF
- Fine-tuning
- Envoi de courriers sans validation
- Décision automatique d’indemnisation
- Frontend complet
