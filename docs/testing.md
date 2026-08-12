# Plan et résultats de tests — données, IA, frontend et e-mails

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

La suite actuelle comprend **27 tests** couvrant :

- services Claims et contexte relationnel ;
- API HTTP 200, 400 et 404 ;
- structure de `ApiErrorDto` et absence de stack trace ;
- import initial, idempotence, relations, dates et rollback ;
- endpoints de génération et journalisation ;
- envoi d’e-mail de démonstration, persistance et assainissement du HTML.

La validation GitHub Actions du 25 juillet 2026 comptait 23 tests. La suite actuelle, incluant les tests e-mail, a été rejouée localement le 12 août 2026 en configuration Release : 27 tests réussis.

## Tests Python

La suite FastAPI comprend **17 tests** couvrant :

- le contrat `/api/v1/generate` ;
- `summary`, `letter` et `response` ;
- le fournisseur déterministe ;
- Groq simulé sans appel réseau ;
- erreurs assainies, timeout et réponse vide ;
- convention monétaire TND ;
- séparation entre règles système, contexte et instruction utilisateur.

La validation historique du 25 juillet 2026 comptait 14 tests. La suite actuelle a été rejouée localement le 12 août 2026 : 17 tests réussis, sans appel réseau ni clé réelle.

## Frontend

Le frontend ne possède pas encore de suite de tests unitaires dédiée. La régression minimale exécute `tsc` puis `vite build`, ce qui vérifie les types, les imports et la production du bundle. Le build a été validé localement le 12 août 2026 avec 36 modules transformés.

Les parcours recherche, pagination, ouverture du contexte, génération, édition, aperçu, confirmation et historique des e-mails restent à vérifier manuellement lors de la démonstration.

## Commandes de régression

```powershell
dotnet restore .\AstreeClaims.sln
dotnet test .\AstreeClaims.sln -c Release
$env:PYTHONPATH = (Resolve-Path .\ai-service).Path
$env:LLM_PROVIDER = "deterministic"
.\ai-service\.venv\Scripts\python.exe -m pytest .\ai-service\tests -q
npm run build --prefix .\frontend
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
