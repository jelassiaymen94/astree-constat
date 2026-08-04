# Contrats d’API

## Conventions

- JSON UTF-8 et propriétés en camelCase ;
- dates ISO 8601 ;
- montants JSON numériques exprimés en TND ;
- pagination à partir de 1, taille maximale 100 ;
- toute génération est un brouillon soumis à validation humaine.

## Consultation des sinistres

### Lister

```http
GET /api/claims?page=1&pageSize=20&status=Ouvert&type=Accident&search=CLM-
```

Les paramètres sont facultatifs. `status` et `type` utilisent une égalité exacte ; `search` recherche une partie de `ClaimId`.

### Charger un dossier

```http
GET /api/claims/{claimId}
```

Retourne un `ClaimDto` ou HTTP `404 CLAIM_NOT_FOUND`.

### Charger le contexte consolidé

```http
GET /api/claims/{claimId}/context
```

Retourne `claim`, `customer`, `contract` et `vehicle`.

## Génération de brouillons — S4

### Générer

```http
POST /api/claims/{claimId}/generate
Content-Type: application/json
```

```json
{
  "generationType": "summary",
  "userInstruction": "Rester factuel et concis."
}
```

Types autorisés :

- `summary` : synthèse interne du dossier ;
- `letter` : projet de courrier destiné à l’assuré ;
- `response` : projet de réponse contextualisée.

`userInstruction` est facultatif et limité à 1 000 caractères.

Exemple de réponse Groq HTTP `200` :

```json
{
  "generationId": "8344e840-1dda-43cf-97f8-5e08373f8e16",
  "claimId": "CLM-3972B1FD",
  "generationType": "summary",
  "userInstruction": "Rester factuel et concis.",
  "generatedContent": "Brouillon de synthèse...",
  "modelName": "llama-3.3-70b-versatile",
  "promptVersion": "2.1",
  "success": true,
  "errorMessage": null,
  "createdAt": "2026-07-25T13:50:00Z",
  "durationMs": 894,
  "requiresHumanValidation": true
}
```

En mode déterministe, le contrat reste identique avec `modelName=deterministic-template` et `promptVersion=1.0`.

### Historique

```http
GET /api/claims/{claimId}/generations
```

Retourne les tentatives de la plus récente à la plus ancienne. Les succès et échecs sont conservés dans `GenerationLogs`.

## Contrat interne FastAPI

```http
POST /api/v1/generate
Content-Type: application/json
```

La requête contient `generationType`, `userInstruction` et le contexte consolidé. La réponse contient :

```json
{
  "content": "Brouillon généré...",
  "modelName": "llama-3.3-70b-versatile",
  "promptVersion": "2.1",
  "durationMs": 894
}
```

Le fournisseur est sélectionné par `LLM_PROVIDER` : `deterministic` pour les tests ou `groq` pour la génération réelle.

## Santé

```http
GET /api/health/database
GET /api/health/ai
GET /health
```

## Erreurs publiques

```json
{
  "code": "CLAIM_NOT_FOUND",
  "message": "Le sinistre CLM-INEXISTANT est introuvable.",
  "traceId": "00-...",
  "errors": null
}
```

| HTTP | Code | Utilisation |
|---:|---|---|
| 400 | `INVALID_REQUEST` | Requête ou type de génération invalide |
| 404 | `CLAIM_NOT_FOUND` | Sinistre introuvable |
| 502 | `AI_SERVICE_UNAVAILABLE` | FastAPI, Groq, timeout ou réponse invalide |
| 503 | `DATABASE_UNAVAILABLE` | SQL Server inaccessible |
| 500 | `INTERNAL_ERROR` | Erreur interne non prévue |

Les réponses publiques n’exposent aucun secret, détail SQL, stack trace ou message brut Groq.
