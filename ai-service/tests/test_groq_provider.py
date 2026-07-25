import asyncio
from types import SimpleNamespace

import pytest

from app.config import Settings
from app.models import GenerationRequest
from app.prompts import build_messages
from app.providers.base import GenerationProviderError
from app.providers.groq_provider import GroqProvider


def make_request() -> GenerationRequest:
    return GenerationRequest.model_validate({
        "generationType": "summary",
        "userInstruction": "Rester concis.",
        "context": {
            "claim": {"claimId": "CLM-1", "date": "2026-07-25", "type": "Accident", "description": "Collision légère.", "estimatedAmount": 1500, "compensationAmount": 0, "status": "Ouvert"},
            "customer": {"clientId": "CLI-1", "firstName": "Test", "lastName": "Client", "governorate": "Tunis"},
            "contract": {"contractId": "CTR-1", "coverageType": "Tous risques", "startDate": "2026-01-01", "endDate": "2026-12-31"},
            "vehicle": {"vehicleId": "VEH-1", "type": "Voiture", "brand": "Hyundai", "model": "i30", "registrationNumber": "123 TUN 4567"},
        },
    })


class FakeCompletions:
    def __init__(self, result=None, error=None):
        self.result = result
        self.error = error
        self.arguments = None

    async def create(self, **kwargs):
        self.arguments = kwargs
        if self.error:
            raise self.error
        return self.result


class FakeClient:
    def __init__(self, completions):
        self.chat = SimpleNamespace(completions=completions)


class FakeStatusError(Exception):
    def __init__(self, status_code, message="sensitive-provider-message"):
        super().__init__(message)
        self.status_code = status_code


def settings(**overrides) -> Settings:
    values = {"llm_provider": "groq", "groq_api_key": "test-secret-key", "groq_model": "llama-3.3-70b-versatile"}
    values.update(overrides)
    return Settings(**values)


def test_groq_provider_requires_api_key():
    with pytest.raises(GenerationProviderError) as captured:
        GroqProvider(Settings(llm_provider="groq", groq_api_key=None))
    assert captured.value.code == "GROQ_CONFIGURATION_ERROR"


def test_groq_provider_returns_normalized_result():
    completions = FakeCompletions(SimpleNamespace(model="llama-3.3-70b-versatile", choices=[SimpleNamespace(message=SimpleNamespace(content="Brouillon Groq à valider."))]))
    provider = GroqProvider(settings(), FakeClient(completions))
    result = asyncio.run(provider.generate(make_request()))
    assert result.content == "Brouillon Groq à valider."
    assert result.model_name == "llama-3.3-70b-versatile"
    assert result.prompt_version == "2.1"
    assert result.duration_ms >= 1
    assert completions.arguments["temperature"] == 0.2
    assert completions.arguments["max_completion_tokens"] == 1000


@pytest.mark.parametrize(("status_code", "expected_code"), [(401, "GROQ_AUTHENTICATION_ERROR"), (403, "GROQ_AUTHENTICATION_ERROR"), (429, "GROQ_RATE_LIMITED"), (498, "GROQ_CAPACITY_EXCEEDED"), (503, "GROQ_UNAVAILABLE")])
def test_groq_errors_are_sanitized(status_code, expected_code):
    completions = FakeCompletions(error=FakeStatusError(status_code))
    provider = GroqProvider(settings(), FakeClient(completions))
    with pytest.raises(GenerationProviderError) as captured:
        asyncio.run(provider.generate(make_request()))
    assert captured.value.code == expected_code
    assert "sensitive-provider-message" not in captured.value.public_message
    assert "test-secret-key" not in captured.value.public_message


def test_empty_groq_content_is_rejected():
    completions = FakeCompletions(SimpleNamespace(model="llama-3.3-70b-versatile", choices=[SimpleNamespace(message=SimpleNamespace(content="  "))]))
    provider = GroqProvider(settings(), FakeClient(completions))
    with pytest.raises(GenerationProviderError) as captured:
        asyncio.run(provider.generate(make_request()))
    assert captured.value.code == "GROQ_INVALID_RESPONSE"


def test_prompt_separates_rules_context_and_user_instruction():
    messages = build_messages(make_request())
    assert messages[0]["role"] == "system"
    assert "aucune décision d'indemnisation" in messages[0]["content"]
    assert "dinars tunisiens (TND)" in messages[0]["content"]
    assert "ne change jamais la devise" in messages[0]["content"]
    assert messages[1]["role"] == "user"
    assert "<context_json>" in messages[1]["content"]
    assert "<instruction_utilisateur>" in messages[1]["content"]
    assert "estimatedAmount et compensationAmount sont en TND" in messages[1]["content"]
    assert "CLM-1" in messages[1]["content"]
