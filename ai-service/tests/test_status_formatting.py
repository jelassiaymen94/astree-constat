from app.models import GenerationRequest
from app.prompts import build_messages
from app.providers.deterministic import DeterministicProvider
from app.text_formatting import humanize_status, normalize_plain_text_draft, sanitize_html_draft


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


def test_groq_prompt_requires_allowlisted_html_output():
    system_prompt = build_messages(make_request())[0]["content"]
    assert "uniquement un fragment HTML" in system_prompt
    assert "sans Markdown" in system_prompt
    assert "<strong>" in system_prompt
    assert "aucun lien, image, style, script" in system_prompt


def test_normalize_plain_text_draft_keeps_legacy_compatibility():
    raw_content = """**Détails du sinistre**
- **Date** : 30 / 12 / 2025
- **Sinistre** : CLM‑72243F24"""

    normalized = normalize_plain_text_draft(raw_content)

    assert normalized == """Détails du sinistre
• Date : 30/12/2025
• Sinistre : CLM-72243F24"""


def test_sanitize_html_draft_preserves_formatting_and_removes_dangerous_markup():
    raw_content = (
        '<h3 onclick="alert(1)">Détails du sinistre</h3>'
        '<ul><li><strong>Type</strong> : Responsabilité civile</li></ul>'
        '<script>alert("xss")</script>'
        '<p style="color:red"><em>Brouillon à valider.</em></p>'
    )

    sanitized = sanitize_html_draft(raw_content)

    assert sanitized == (
        '<h3>Détails du sinistre</h3>'
        '<ul><li><strong>Type</strong> : Responsabilité civile</li></ul>'
        '<p><em>Brouillon à valider.</em></p>'
    )
    assert "script" not in sanitized
    assert "onclick" not in sanitized
    assert "style=" not in sanitized


def test_sanitize_html_draft_converts_legacy_plain_text_to_html():
    sanitized = sanitize_html_draft("Bonjour\n\n• Statut : Ouvert\n• Montant : 100 TND")

    assert sanitized == "<p>Bonjour</p><ul><li>Statut : Ouvert</li><li>Montant : 100 TND</li></ul>"
