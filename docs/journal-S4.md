# Journal de développement — S4

## Étape S4A — flux de génération

Le flux validé est :

```text
ClaimsController
→ ClaimGenerationService
→ AiGenerationClient
→ FastAPI /api/v1/generate
→ GenerationLogs
```

Trois types sont disponibles : `summary`, `letter` et `response`. Le fournisseur initial `deterministic-template` utilise le prompt `1.0`. Chaque réponse publique indique `requiresHumanValidation: true`.

## Étape S4B — fournisseur Groq

S4B introduit une abstraction de fournisseur dans FastAPI :

```text
GenerationProvider
├── DeterministicProvider
└── GroqProvider → AsyncGroq → GroqCloud
```

Le fournisseur est sélectionné avec `LLM_PROVIDER`. Le mode `deterministic` reste la valeur par défaut et ne requiert aucun secret. Le mode `groq` utilise `GROQ_API_KEY`, `GROQ_MODEL`, `GROQ_TEMPERATURE`, `GROQ_MAX_TOKENS` et `GROQ_TIMEOUT_SECONDS`.

Les prompts Groq sont centralisés. La version `2.1` impose l’usage exclusif du contexte, la devise TND sans conversion, l’interdiction de toute décision d’indemnisation et de tout envoi automatique, ainsi que la validation humaine obligatoire.

Les erreurs d’authentification, de limite, de capacité, de timeout et d’indisponibilité sont converties en messages assainis. La clé et les messages bruts du fournisseur ne sont jamais exposés.

Quatorze tests Python couvrent le contrat FastAPI, les trois types de génération, le fournisseur Groq simulé, les erreurs assainies, la réponse vide, la convention TND et la séparation entre règles, contexte et instruction utilisateur. Aucun test ne réalise d’appel Groq réel.

## Validation manuelle

Une première génération Groq complète a réussi pour `CLM-3972B1FD` avec persistance dans `GenerationLogs`, modèle `llama-3.3-70b-versatile`, durée `663 ms`, succès et validation humaine. Elle a toutefois révélé une devise euro inventée alors que les montants du projet sont en TND. Le prompt a donc été corrigé et versionné `2.1`; une nouvelle génération doit confirmer la devise TND avant validation finale.

## Validation attendue

```bash
dotnet test AstreeClaims.sln
cd ai-service && python -m pytest -q
```

La validation finale S4B requiert une nouvelle génération manuelle avec le prompt `2.1`, puis un contrôle de `GenerationLogs`. Aucun secret ne doit être commité.
