from pydantic import BaseModel, ConfigDict, Field
from typing import Literal


class ApiModel(BaseModel):
    model_config = ConfigDict(
        alias_generator=lambda value: "".join(
            word if index == 0 else word.capitalize()
            for index, word in enumerate(value.split("_"))
        ),
        populate_by_name=True,
    )


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
