from time import perf_counter

from app.models import GenerationRequest
from app.providers.base import ProviderResult
from app.text_formatting import humanize_status


class DeterministicProvider:
    async def generate(self, request: GenerationRequest) -> ProviderResult:
        started = perf_counter()
        content = self._build_content(request)
        duration_ms = max(1, round((perf_counter() - started) * 1000))
        return ProviderResult(content=content, model_name="deterministic-template", prompt_version="1.0", duration_ms=duration_ms)

    @staticmethod
    def _build_content(request: GenerationRequest) -> str:
        claim = request.context.claim
        customer = request.context.customer
        contract = request.context.contract
        vehicle = request.context.vehicle
        full_name = f"{customer.first_name} {customer.last_name}".strip()
        readable_status = humanize_status(claim.status)
        instruction = f"\nInstruction complémentaire : {request.user_instruction.strip()}" if request.user_instruction and request.user_instruction.strip() else ""

        if request.generation_type == "summary":
            return (f"Synthèse du sinistre {claim.claim_id}\n\nLe dossier concerne {full_name}, assuré au titre du contrat "
                    f"{contract.contract_id} ({contract.coverage_type}). Le sinistre de type {claim.type} est survenu le {claim.date} "
                    f"avec le véhicule {vehicle.brand} {vehicle.model}, immatriculé {vehicle.registration_number}. Montant estimé : "
                    f"{claim.estimated_amount:.2f} TND. Statut actuel : {readable_status}.{instruction}\n\nBrouillon à valider par un gestionnaire.")
        if request.generation_type == "letter":
            return (f"Objet : Suivi du sinistre {claim.claim_id}\n\nMadame, Monsieur {customer.last_name},\n\n"
                    f"Nous confirmons la prise en charge de votre déclaration du {claim.date}, relative au véhicule {vehicle.brand} {vehicle.model}. "
                    f"Votre dossier est actuellement au statut « {readable_status} ».{instruction}\n\nCe courrier est un brouillon soumis à validation humaine.")
        return (f"Réponse proposée — dossier {claim.claim_id}\n\nBonjour {full_name},\n\nVotre demande concernant le sinistre du {claim.date} a bien été "
                f"prise en compte. Le dossier est actuellement au statut « {readable_status} ».{instruction}\n\nBrouillon à valider avant envoi.")
