# Journal de développement — S4

## Étape S4A — flux de génération

Mise en place du premier flux complet de génération de brouillons :

```text
ClaimsController
→ ClaimGenerationService
→ AiGenerationClient
→ FastAPI /api/v1/generate
→ GenerationLogs
```

Le service FastAPI utilise d’abord un générateur déterministe. Ce choix permet de valider les contrats JSON, la gestion des erreurs, la persistance et l’historique sans clé API externe.

Trois types sont disponibles : `summary`, `letter` et `response`. Chaque réponse indique `requiresHumanValidation: true`. Aucun envoi automatique ni décision d’indemnisation n’est réalisé.

Les tentatives réussies et échouées sont enregistrées avec le modèle, la version du prompt, la durée et la date UTC.

## Validation locale

1. lancer FastAPI sur le port 8000 ;
2. exécuter `dotnet test AstreeClaims.sln` ;
3. vérifier les 23 tests ;
4. générer un brouillon avec `AstreeClaims.Api.http` ;
5. vérifier la ligne correspondante dans `GenerationLogs`.
