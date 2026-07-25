# Contrats d’API

## Conventions

- JSON UTF-8 ;
- dates ISO 8601 ;
- montants JSON numériques ;
- propriétés JSON en camelCase ;
- pagination à partir de 1 ;
- taille maximale d’une page : 100 ;
- toute génération est un brouillon soumis à validation humaine.

## Consultation des sinistres

### Lister les sinistres

```http
GET /api/claims?page=1&pageSize=20&status=Ouvert&type=Accident&search=CLM-
```

Tous les paramètres sont facultatifs. `status` et `type` utilisent une égalité exacte ; `search` recherche une partie de `ClaimId`.

### Charger un sinistre

```http
GET /api/claims/{claimId}
```

Retourne un `ClaimDto` ou HTTP `404 CLAIM_NOT_FOUND`.

### Charger le contexte consolidé

```http
GET /api/claims/{claimId}/context
```

Retourne les objets `claim`, `customer`, `contract` et `vehicle` nécessaires à la génération.

## Génération de brouillons — S4A

### Générer un brouillon

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

Valeurs acceptées :

- `summary` : synthèse interne ;
- `letter` : projet de courrier ;
- `response` : projet de réponse au client.

`userInstruction` est facultatif et limité à 1 000 caractères.

Réponse HTTP `200` :

```json
{
  "generationId": "4d47802e-817b-4dce-bb7e-1395543b74a7",
  "claimId": "CLM-3972B1FD",
  "generationType": "summary",
  "userInstruction": "Rester factuel et concis.",
  "generatedContent": "Synthèse du sinistre...",
  "modelName": "deterministic-template",
  "promptVersion": "1.0",
  "success": true,
  "errorMessage": null,
  "createdAt": "2026-07-25T01:02:07Z",
  "durationMs": 1,
  "requiresHumanValidation": true
}
```

Le contenu n’est jamais envoyé automatiquement et ne constitue pas une décision d’indemnisation.

### Historique des générations

```http
GET /api/claims/{claimId}/generations
```

Retourne un tableau de `GenerationDto`, de la tentative la plus récente à la plus ancienne. Les succès et les échecs sont conservés dans `GenerationLogs`.

### Contrat interne FastAPI

```http
POST /api/v1/generate
Content-Type: application/json
```

La requête contient `generationType`, `userInstruction` et le contexte consolidé. FastAPI retourne :

```json
{
  "content": "Brouillon généré...",
  "modelName": "deterministic-template",
  "promptVersion": "1.0",
  "durationMs": 1
}
```

Le modèle `deterministic-template` valide le flux HTTP et la persistance avant l’intégration d’un fournisseur LLM réel en S4B.

## Endpoints de santé

```http
GET /api/health/database
GET /api/health/ai
```

## Format uniforme des erreurs

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
| 400 | `INVALID_REQUEST` | Paramètre, pagination ou type de génération invalide |
| 404 | `CLAIM_NOT_FOUND` | Sinistre introuvable |
| 502 | `AI_SERVICE_UNAVAILABLE` | FastAPI inaccessible, timeout ou réponse invalide |
| 503 | `DATABASE_UNAVAILABLE` | SQL Server inaccessible |
| 500 | `INTERNAL_ERROR` | Erreur interne non prévue |

Les réponses publiques n’exposent ni stack trace, ni chaîne de connexion, ni détail SQL.
