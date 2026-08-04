# Journal de développement — S4

## S4A — flux de génération

Flux implémenté :

```text
ClaimsController
→ ClaimGenerationService
→ AiGenerationClient
→ FastAPI /api/v1/generate
→ GenerationLogs
```

Trois types sont disponibles : `summary`, `letter` et `response`. Le fournisseur initial `deterministic-template` utilise le prompt `1.0`. Chaque réponse publique indique `requiresHumanValidation: true`.

## S4B — fournisseur Groq

FastAPI utilise une abstraction interchangeable :

```text
GenerationProvider
├── DeterministicProvider
└── GroqProvider → AsyncGroq → GroqCloud
```

Le fournisseur est sélectionné avec `LLM_PROVIDER`. Le mode `deterministic` ne requiert aucun secret ; le mode `groq` utilise la clé locale et le modèle configuré.

Les prompts Groq version `2.1` imposent : usage exclusif du contexte, montants en TND sans conversion, aucune décision d’indemnisation, aucun envoi automatique et validation humaine obligatoire.

Les erreurs d’authentification, limite, capacité, timeout, requête invalide et indisponibilité sont converties en messages publics assainis.

## Tests enregistrés

- 23 tests .NET réussis dans GitHub Actions ;
- 14 tests Python réussis sans appel réseau ;
- couverture des trois types, contrats, erreurs, TND et règles de sécurité.

## Validation Groq

Une première génération avait inventé une devise en euros. Le prompt a été renforcé et versionné `2.1`.

La validation du 25 juillet 2026 a confirmé :

- flux `.NET → FastAPI → Groq → GenerationLogs` ;
- modèle `llama-3.3-70b-versatile` ;
- prompt `2.1` ;
- montants en TND ;
- `success=true` et `errorMessage=null` ;
- `requiresHumanValidation=true` ;
- aucune décision ni aucun envoi automatique.

## Incident corrigé — cohérence du type `letter`

Le 31 juillet 2026, `letter` retournait HTTP 500 alors que Groq produisait correctement le contenu.

Cause : .NET et FastAPI utilisaient `letter`, mais la contrainte SQL `CK_GenerationLogs_Type` autorisait encore `status_letter`. L’enregistrement dans `GenerationLogs` échouait.

Correction :

- remplacement de `status_letter` par `letter` dans la base active ;
- mise à jour de `database/schema.sql` ;
- cohérence vérifiée pour `summary`, `letter` et `response` ;
- nouvelle validation de la génération et de la persistance de `letter`.

## Démarrage stabilisé

Les secrets sont centralisés dans le `.env` racine. `start.cmd` charge la configuration, vérifie les dépendances Python, démarre les trois services, contrôle les endpoints de santé et ouvre Swagger. Cette vérification évite qu’un environnement virtuel existant mais incomplet provoque une erreur `ModuleNotFoundError`.

## Commandes de régression

```powershell
dotnet test .\AstreeClaims.sln
.\ai-service\.venv\Scripts\python.exe -m pytest .\ai-service\tests -q
```

S4 est considérée terminée : moteur de génération fonctionnel, trois cas d’usage, journalisation, sécurité, tests et documentation cohérente.
