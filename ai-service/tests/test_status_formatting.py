from app.models import GenerationRequest
from app.prompts import build_messages
from app.providers.deterministic import DeterministicProvider
from app.text_formatting import humanize_status


def make_request(generation_type: str = "letter") -> GenerationRequest:
    return GenerationRequest.model_validate({
        "generationType": generation_type,
        "context": {
            "claim": {"claimId": "CLM-1", "date": "2026-07-25", "type": "Accident", "description": "Collision.", "estimatedAmount": 1500, "compensationAmount": 0, "status": "En_cours_d_expertise"},
            "customer": {"clientId": "CLI-1", "firstName": "Badis", "lastName": "Fkih", "governorate": "Tunis"},
            "contract": {"contractId": "CTR-1", "coverageType": "Tous risques", "startDate": "2026-01-01", "endDate": "2026-12-31"},
            "vehicle": {"vehicleId": "VEH-1", "type": "Voiture", "brand": "Peugeot", "model": "208", "registrationNumber": "123 TUN 4567"},
        },
    })


def test_humanize_status_removes_underscores_and_formats_apostrophe():
    assert humanize_status("En_cours_d_expertise") == "En cours d’expertise"


def test_deterministic_letter_uses_readable_status():
    content = DeterministicProvider._build_content(make_request())
    assert "En cours d’expertise" in content
    assert "En_cours_d_expertise" not in content


def test_groq_prompt_receives_readable_status():
    prompt = build_messages(make_request())[1]["content"]
    assert "En cours d’expertise" in prompt
    assert "En_cours_d_expertise" not in prompt
