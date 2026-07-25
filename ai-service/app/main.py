from time import perf_counter
from typing import Literal

from fastapi import FastAPI
from pydantic import BaseModel, ConfigDict, Field


class ApiModel(BaseModel):
    model_config = ConfigDict(alias_generator=lambda value: "".join(
        word if index == 0 else word.capitalize()
        for index, word in enumerate(value.split("_"))
    ), populate_by_name=True)


class ClaimData(ApiModel):
    claim_id: str
    date: str
    type: str
    description: str
    estimated_amount: float
    compensation_amount: float
    status: str


class CustomerData(ApiModel):
    client_id: str
    first_name: str
    last_name: str
    governorate: str


class ContractData(ApiModel):
    contract_id: str
    coverage_type: str
    start_date: str
    end_date: str


class VehicleData(ApiModel):
    vehicle_id: str
    type: str
    brand: str
    model: str
    registration_number: str


class ClaimContext(ApiModel):
    claim: ClaimData
    customer: CustomerData
    contract: ContractData
    vehicle: VehicleData


class GenerationRequest(ApiModel):
    generation_type: Literal["summary", "letter", "response"]
    user_instruction: str | None = Field(default=None, max_length=1000)
    context: ClaimContext


class GenerationResponse(ApiModel):
    content: str
    model_name: str
    prompt_version: str
    duration_ms: int


app = FastAPI(
    title="ASTREE Claims AI Service",
    version="1.1.0",
)


@app.get("/health")
def health():
    return {
        "service": "astree-ai-service",
        "status": "healthy",
    }


def build_content(request: GenerationRequest) -> str:
    claim = request.context.claim
    customer = request.context.customer
    contract = request.context.contract
    vehicle = request.context.vehicle
    full_name = f"{customer.first_name} {customer.last_name}".strip()
    instruction = (
        f"\nInstruction complémentaire : {request.user_instruction.strip()}"
        if request.user_instruction and request.user_instruction.strip()
        else ""
    )

    if request.generation_type == "summary":
        return (
            f"Synthèse du sinistre {claim.claim_id}\n\n"
            f"Le dossier concerne {full_name}, assuré au titre du contrat "
            f"{contract.contract_id} ({contract.coverage_type}). "
            f"Le sinistre de type {claim.type} est survenu le {claim.date} "
            f"avec le véhicule {vehicle.brand} {vehicle.model}, immatriculé "
            f"{vehicle.registration_number}. Montant estimé : "
            f"{claim.estimated_amount:.2f} TND. Statut actuel : {claim.status}."
            f"{instruction}\n\nBrouillon à valider par un gestionnaire."
        )

    if request.generation_type == "letter":
        return (
            f"Objet : Suivi du sinistre {claim.claim_id}\n\n"
            f"Madame, Monsieur {customer.last_name},\n\n"
            f"Nous confirmons la prise en charge de votre déclaration du "
            f"{claim.date}, relative au véhicule {vehicle.brand} {vehicle.model}. "
            f"Votre dossier est actuellement au statut « {claim.status} »."
            f"{instruction}\n\n"
            "Ce courrier est un brouillon soumis à validation humaine."
        )

    return (
        f"Réponse proposée — dossier {claim.claim_id}\n\n"
        f"Bonjour {full_name},\n\n"
        f"Votre demande concernant le sinistre du {claim.date} a bien été "
        f"prise en compte. Le dossier est actuellement au statut "
        f"« {claim.status} »."
        f"{instruction}\n\nBrouillon à valider avant envoi."
    )


@app.post("/api/v1/generate", response_model=GenerationResponse)
def generate(request: GenerationRequest):
    started = perf_counter()
    content = build_content(request)
    duration_ms = max(1, round((perf_counter() - started) * 1000))

    return GenerationResponse(
        content=content,
        model_name="deterministic-template",
        prompt_version="1.0",
        duration_ms=duration_ms,
    )
