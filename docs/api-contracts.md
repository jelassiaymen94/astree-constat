# Contrats d’API

## 1. Conventions

- Format : JSON
- Encodage : UTF-8
- Dates : ISO 8601 (`YYYY-MM-DD`)
- Montants : nombres décimaux
- API métier : ASP.NET Core
- API interne de génération : FastAPI

## 2. Endpoints ASP.NET Core

### Lister les sinistres

```http
GET /api/claims?status=Ouvert&type=Accident&page=1&pageSize=20
```

Réponse prévue :

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "total": 0
}
```

### Charger un sinistre

```http
GET /api/claims/{claimId}
```

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
    "status": "En_cours_d_expertise"
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

### Générer un contenu

```http
POST /api/claims/{claimId}/generate
```

```json
{
  "generationType": "summary",
  "tone": "professional",
  "detailLevel": "standard",
  "userInstruction": null
}
```

Valeurs de `generationType` :

- `summary`
- `status_letter`
- `response`

Réponse prévue :

```json
{
  "generationId": "c081ba41-f7d5-4bb3-9e35-d8087238c256",
  "claimId": "CLM-001",
  "generationType": "summary",
  "content": "Texte généré...",
  "warnings": [],
  "model": "configured-model",
  "promptVersion": "1.0",
  "durationMs": 1430
}
```

### Charger l’historique

```http
GET /api/claims/{claimId}/generations
```

## 3. Endpoint FastAPI interne

```http
POST /api/v1/generate
```

Requête :

```json
{
  "generationType": "response",
  "tone": "professional",
  "detailLevel": "standard",
  "userInstruction": "L’assuré demande pourquoi le dossier est encore en expertise.",
  "claimContext": {
    "claim": {},
    "customer": {},
    "contract": {},
    "vehicle": {}
  }
}
```

Réponse :

```json
{
  "content": "Madame, Monsieur...",
  "warnings": [],
  "model": "configured-model",
  "promptVersion": "1.0",
  "tokensUsed": 620
}
```

## 4. Endpoints de santé déjà validés

### Base de données

```http
GET /api/health/database
```

```json
{
  "database": "AstreeClaimsDb",
  "connected": true
}
```

### FastAPI

```http
GET http://localhost:8000/health
```

```json
{
  "service": "astree-ai-service",
  "status": "healthy"
}
```

### Communication .NET vers FastAPI

```http
GET /api/health/ai
```

La réponse doit contenir un statut HTTP `200` et `connected: true`.

## 5. Codes d’erreur prévus

| HTTP | Code | Utilisation |
|---:|---|---|
| 400 | `INVALID_REQUEST` | Paramètres invalides |
| 404 | `CLAIM_NOT_FOUND` | Sinistre introuvable |
| 422 | `INCOMPLETE_CONTEXT` | Contexte insuffisant |
| 502 | `LLM_UNAVAILABLE` | Service IA indisponible |
| 504 | `LLM_TIMEOUT` | Délai d’attente dépassé |
| 500 | `INTERNAL_ERROR` | Erreur interne non prévue |
