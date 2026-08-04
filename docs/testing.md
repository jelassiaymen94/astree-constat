# Plan et résultats de tests — S3 et S4

## Données et import

| Test | Résultat attendu | Statut |
|---|---|---|
| Préparation Excel | 3 460 valides, 1 792 exclus | Réussi |
| Relations source | zéro relation cassée | Réussi |
| Premier import | 2 048 / 2 179 / 2 179 / 3 460 insertions | Réussi |
| Deuxième import | zéro insertion | Réussi |
| Vérifications SQL | contrôles à zéro, aucun doublon | Réussi |

## Santé technique

| Test | Résultat attendu | Statut |
|---|---|---|
| `/api/health/database` | `connected: true` | Réussi |
| FastAPI `/health` | `status: healthy` | Réussi |
| `/api/health/ai` | `connected: true` | Réussi |

## Consultation et erreurs

Sont couverts : pagination, filtres, recherche, détail, contexte consolidé, paramètres invalides, sinistre absent, indisponibilité SQL/FastAPI, réponse publique sans stack trace et corrélation par `traceId`.

## Tests .NET

Le projet `tests/AstreeClaims.Api.Tests/` utilise xUnit, `WebApplicationFactory<Program>` et SQLite en mémoire. Il ne dépend ni de SQL Server Docker ni des données de développement.

La suite comprend **23 tests** couvrant :

- services Claims et contexte relationnel ;
- API HTTP 200, 400 et 404 ;
- structure de `ApiErrorDto` et absence de stack trace ;
- import initial, idempotence, relations, dates et rollback ;
- endpoints de génération et journalisation.

Dernière validation enregistrée dans le projet : 23 tests réussis dans GitHub Actions le 25 juillet 2026.

## Tests Python

La suite FastAPI comprend **14 tests** couvrant :

- le contrat `/api/v1/generate` ;
- `summary`, `letter` et `response` ;
- le fournisseur déterministe ;
- Groq simulé sans appel réseau ;
- erreurs assainies, timeout et réponse vide ;
- convention monétaire TND ;
- séparation entre règles système, contexte et instruction utilisateur.

Dernière validation enregistrée : 14 tests réussis le 25 juillet 2026.

## Commandes de régression

```powershell
dotnet restore .\AstreeClaims.sln
dotnet test .\AstreeClaims.sln
.\ai-service\.venv\Scripts\python.exe -m pytest .\ai-service\tests -q
```

Avec couverture .NET :

```powershell
dotnet test .\AstreeClaims.sln --collect:"XPlat Code Coverage"
```

## Validation manuelle end-to-end S4

Dossier de référence : `CLM-3972B1FD`.

| Test | Attendu |
|---|---|
| `summary` | HTTP 200, contenu factuel |
| `letter` | HTTP 200, courrier professionnel |
| `response` | HTTP 200, réponse contextualisée |
| Persistance | trois types présents dans `GenerationLogs` |
| Métadonnées | modèle, prompt, durée et succès renseignés |
| Sécurité | `requiresHumanValidation=true` |
| Devise | montants uniquement en TND |
| Limites | aucune décision ou promesse de paiement |

Le 31 juillet 2026, la validation manuelle a détecté puis corrigé l’incohérence SQL `status_letter`/`letter`. Après mise à jour de `CK_GenerationLogs_Type`, la génération `letter` et sa persistance ont été validées.

## Critères de clôture S4

- les trois types sont cohérents sur .NET, FastAPI et SQL ;
- la génération réelle Groq fonctionne de bout en bout ;
- le mode déterministe reste disponible ;
- les générations sont auditables ;
- aucun secret n’est versionné ;
- la documentation correspond au comportement actuel.
