from fastapi.testclient import TestClient
import pytest

from app.main import app
from app.providers import get_provider
from app.providers.deterministic import DeterministicProvider


@pytest.fixture
def client():
    app.dependency_overrides[get_provider] = lambda: DeterministicProvider()
    with TestClient(app) as test_client:
        yield test_client
    app.dependency_overrides.clear()


@pytest.fixture
def payload():
    return {
        "generationType": "summary",
        "userInstruction": "Rester factuel et concis.",
        "context": {
            "claim": {"claimId": "CLM-TEST-1", "date": "2026-07-25", "type": "Accident", "description": "Collision légère.", "estimatedAmount": 1500.0, "compensationAmount": 0.0, "status": "Ouvert"},
            "customer": {"clientId": "CLI-1", "firstName": "Test", "lastName": "Client", "governorate": "Tunis"},
            "contract": {"contractId": "CTR-1", "coverageType": "Tous risques", "startDate": "2026-01-01", "endDate": "2026-12-31"},
            "vehicle": {"vehicleId": "VEH-1", "type": "Voiture", "brand": "Hyundai", "model": "i30", "registrationNumber": "123 TUN 4567"},
        },
    }


def test_health_exposes_configured_provider(client):
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json()["provider"] == "deterministic"


@pytest.mark.parametrize("generation_type", ["summary", "letter", "response"])
def test_deterministic_generation_preserves_contract(client, payload, generation_type):
    payload["generationType"] = generation_type
    response = client.post("/api/v1/generate", json=payload)
    assert response.status_code == 200
    body = response.json()
    assert body["content"]
    assert body["modelName"] == "deterministic-template"
    assert body["promptVersion"] == "1.0"
    assert body["durationMs"] >= 1
    assert "Brouillon" in body["content"] or "brouillon" in body["content"]


def test_invalid_generation_type_is_rejected(client, payload):
    payload["generationType"] = "automatic-decision"
    response = client.post("/api/v1/generate", json=payload)
    assert response.status_code == 422
