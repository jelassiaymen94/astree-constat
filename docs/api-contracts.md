# Contrats d’API

## Conventions

- JSON UTF-8 ;
- dates ISO 8601 `YYYY-MM-DD` ;
- montants JSON numériques ;
- propriétés JSON en camelCase ;
- pagination à partir de 1 ;
- taille maximale d’une page : 100.

## Endpoints implémentés

### Lister les sinistres

```http
GET /api/claims?page=1&pageSize=20&status=Ouvert&type=Accident&search=CLM-
```

Tous les paramètres sont facultatifs. `status` et `type` utilisent une égalité exacte ; `search` recherche une partie de `ClaimId`.

```json
{
  "items": [
    {
      "claimId": "CLM-001",
      "date": "2025-04-10",
      "type": "Accident",
      "description": "Accrochage léger",
      "estimatedAmount": 2500.00,
      "compensationAmount": 1500.00,
      "status": "Ouvert"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 3460
}
```

### Charger un sinistre

```http
GET /api/claims/{claimId}
```

Retourne un `ClaimDto` ou HTTP `404`.

### Charger le contexte consolidé

```http
GET /api/claims/{claimId}/context
```

```json
{
  "claim": {
    "claimId": "CLM-001",
    "date": "2025-04-10",
    "type": "Accident",
    "description": "Accrochage léger",
    "estimatedAmount": 2500.00,
    "compensationAmount": 1500.00,
    "status": "Ouvert"
  },
  "customer": {
    "clientId": "CLI-001",
    "firstName": "Prénom",
    "lastName": "Nom",
    "governorate": "Tunis"
  },
  "contract": {
    "contractId": "CON-001",
    "coverageType": "Tous Risques",
    "startDate": "2025-01-01",
    "endDate": "2025-12-31"
  },
  "vehicle": {
    "vehicleId": "VEH-001",
    "type": "Auto",
    "brand": "Peugeot",
    "model": "208",
    "registrationNumber": "123TU4567"
  }
}
```

### Santé SQL Server

```http
GET /api/health/database
```

### Santé FastAPI via .NET

```http
GET /api/health/ai
```

## Format uniforme des erreurs — S3 4B

```json
{
  "code": "CLAIM_NOT_FOUND",
  "message": "Le sinistre CLM-INEXISTANT est introuvable.",
  "traceId": "00-...",
  "errors": null
}
```

### Paramètres invalides

HTTP `400`, code `INVALID_REQUEST`. `errors` contient les champs invalides.

### Sinistre introuvable

HTTP `404`, code `CLAIM_NOT_FOUND`.

### SQL Server indisponible

HTTP `503`, code `DATABASE_UNAVAILABLE`. Aucun détail SQL n’est exposé.

### Service IA indisponible

HTTP `502`, code `AI_SERVICE_UNAVAILABLE`. L’exception réseau réelle reste dans les logs serveur.

### Erreur inattendue

HTTP `500`, code `INTERNAL_ERROR`. La stack trace reste uniquement dans les logs serveur.

| HTTP | Code | Utilisation |
|---:|---|---|
| 400 | `INVALID_REQUEST` | Paramètre ou modèle invalide |
| 404 | `CLAIM_NOT_FOUND` | Sinistre introuvable |
| 502 | `AI_SERVICE_UNAVAILABLE` | FastAPI inaccessible |
| 503 | `DATABASE_UNAVAILABLE` | SQL Server inaccessible |
| 500 | `INTERNAL_ERROR` | Erreur interne non prévue |

## Endpoints prévus pour S4 — non implémentés

```http
POST /api/claims/{claimId}/generate
GET /api/claims/{claimId}/generations
POST /api/v1/generate
```

Ils concerneront le LLM, les templates et l’historique des générations. Ils ne doivent pas être présentés comme disponibles en S3.
