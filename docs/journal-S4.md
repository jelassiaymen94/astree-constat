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

Les prompts Groq sont centralisés en version `2.0`. Ils imposent l’usage exclusif du contexte, interdisent toute décision d’indemnisation et tout envoi automatique, et maintiennent la validation humaine obligatoire.

Les erreurs d’authentification, de limite, de capacité, de timeout et d’indisponibilité sont converties en messages assainis. La clé et les messages bruts du fournisseur ne sont jamais exposés.

Quatorze tests Python couvrent le contrat FastAPI, les trois types de génération, le fournisseur Groq simulé, les erreurs assainies, la réponse vide et la séparation entre règles, contexte et instruction utilisateur. Aucun test ne réalise d’appel Groq réel.

## Validation attendue

```bash
dotnet test AstreeClaims.sln
cd ai-service && python -m pytest -q
```

La validation finale S4B requiert aussi une génération manuelle avec une clé Groq locale, puis un contrôle de `GenerationLogs`. Aucun secret ne doit être commité.
