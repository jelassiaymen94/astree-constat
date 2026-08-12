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

Validation historique du 25 juillet 2026 :

- 23 tests .NET réussis dans GitHub Actions ;
- 14 tests Python réussis sans appel réseau ;
- couverture des trois types, contrats, erreurs, TND et règles de sécurité.

Validation locale du 12 août 2026 :

- 27 tests .NET réussis en configuration Release, y compris les tests du service e-mail ;
- 17 tests Python réussis en mode déterministe, sans appel réseau ;
- compilation TypeScript et build Vite réussis avec 36 modules transformés.

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

## Alignement frontend et e-mails — 12 août 2026

La documentation a été réalignée sur l’implémentation actuelle, qui dépasse le périmètre initial de S4 :

- ajout de l’architecture React 19 / TypeScript / Vite et des parcours de consultation, génération et historique ;
- documentation du flux `React → .NET → SMTP → Mailtrap → EmailLogs` ;
- ajout des contrats `POST /api/claims/{claimId}/emails/send` et `GET /api/claims/{claimId}/emails` ;
- description de la confirmation explicite, de l’idempotence par `ClientRequestId`, de la redirection de démonstration et de l’assainissement HTML ;
- ajout de `Clients.Email` et `EmailLogs` au dictionnaire des données ;
- ajout de la migration idempotente `database/upgrade-email.sql` et du démarrage frontend aux instructions ;
- distinction entre `VITE_API_BASE_URL`, préfixe public des clients HTTP, et `VITE_BACKEND_URL`, cible du proxy Vite ;
- correction de `validate-s4.ps1` pour définir temporairement `PYTHONPATH` vers `ai-service` pendant pytest, puis restaurer l’environnement précédent.

Le frontend reste un prototype de démonstration sans authentification, rôles, pièces jointes, relais SMTP de production ni suite de tests unitaires dédiée.

## Commandes de régression

```powershell
dotnet test .\AstreeClaims.sln -c Release
$env:PYTHONPATH = (Resolve-Path .\ai-service).Path
$env:LLM_PROVIDER = "deterministic"
.\ai-service\.venv\Scripts\python.exe -m pytest .\ai-service\tests -q
npm run build --prefix .\frontend
```

S4 est considérée terminée : moteur de génération fonctionnel, trois cas d’usage, journalisation, sécurité, tests et documentation cohérente.
