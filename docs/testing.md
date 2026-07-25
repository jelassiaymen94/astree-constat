# Plan et résultats de tests S3

Statuts : `Réussi`, `Automatisé`, `À exécuter`, `Non commencé`.

## Données et import

| Test | Résultat attendu | Statut |
|---|---|---|
| Préparation Excel | 3 460 valides, 1 792 exclus | Réussi |
| Relations source | zéro relation cassée | Réussi |
| Premier import | 2 048 / 2 179 / 2 179 / 3 460 insertions | Réussi |
| Deuxième import | zéro insertion | Réussi |
| Vérification SQL | tous les contrôles à zéro | Réussi |
| Doublons SQL | aucune ligne | Réussi |

Preuves : captures SSMS des comptages, contrôles relationnels, statuts et types.

## Santé technique

| Test | Résultat attendu | Statut |
|---|---|---|
| `/api/health/database` | `connected: true` | Réussi |
| FastAPI `/health` | `status: healthy` | Réussi |
| `/api/health/ai` | `connected: true` | Réussi |

## Endpoints Claims — 4A

| Test | Résultat attendu | Statut |
|---|---|---|
| `GET /api/claims?page=1&pageSize=5` | 5 éléments, total 3 460 | Réussi |
| Page 2 | éléments différents | Réussi |
| `status=Ouvert` | total 996 | Réussi |
| `type=Accident` | total 1 878 | Réussi |
| Recherche identifiant | uniquement les identifiants correspondants | Réussi |
| Détail existant | HTTP 200 | Réussi |
| Contexte consolidé | quatre objets cohérents | Réussi |

## Gestion des erreurs — 4B

| Test | Requête / action | Résultat attendu | Statut |
|---|---|---|---|
| Page invalide | `page=0` | 400 `INVALID_REQUEST` | Réussi |
| Taille invalide | `pageSize=101` | 400 `INVALID_REQUEST` | Réussi |
| Sinistre absent | `CLM-INEXISTANT` | 404 `CLAIM_NOT_FOUND` | Réussi |
| Contexte absent | `CLM-INEXISTANT/context` | 404 `CLAIM_NOT_FOUND` | Réussi |
| FastAPI arrêté | `GET /api/health/ai` | 502 `AI_SERVICE_UNAVAILABLE` | Réussi |
| SQL arrêté | `GET /api/claims` | 503 `DATABASE_UNAVAILABLE` | Réussi |
| Erreur publique | réponse sans stack trace | Réussi |
| Corrélation | `traceId` présent dans la réponse et les logs | Réussi |
| Régression | services redémarrés, liste HTTP 200 | Réussi |

Swagger bloque localement les valeurs hors contraintes OpenAPI, comme `page=0`. La réponse HTTP 400 réelle a donc été vérifiée avec curl ou le fichier `AstreeClaims.Api.http`.

## Tests automatisés — 5A

Le projet `tests/AstreeClaims.Api.Tests/` utilise xUnit, `Microsoft.AspNetCore.Mvc.Testing` et SQLite en mémoire. Il ne dépend ni de SQL Server Docker ni des 3 460 lignes de développement.

### Couverture ajoutée

- services Claims : pagination, deuxième page, filtres, recherche, détail existant ou absent et contexte relationnel ;
- API : HTTP 200, validations HTTP 400, HTTP 404, structure de `ApiErrorDto`, `traceId` et absence de stack trace ;
- import : premier import, idempotence, date hors contrat, relation cassée et rollback transactionnel.

La suite contient 18 cas de test. Exécution locale validée le 25 juillet 2026 : 18 tests réussis, 0 échec, 0 ignoré.

### Exécution

Depuis la racine :

```bash
dotnet restore
dotnet test AstreeClaims.sln
```

Avec couverture :

```bash
dotnet test AstreeClaims.sln --collect:"XPlat Code Coverage"
```

Pour exécuter uniquement le projet de tests :

```bash
dotnet test tests/AstreeClaims.Api.Tests/AstreeClaims.Api.Tests.csproj
```

## Critères de clôture 5A

- `dotnet build` sans erreur ;
- 18 tests réussis ;
- aucun accès à la base de développement ;
- aucune dépendance à Docker ;
- documentation cohérente avec les résultats réellement observés.
