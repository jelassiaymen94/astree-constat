from dataclasses import dataclass
from typing import Protocol

from app.models import GenerationRequest


@dataclass(frozen=True)
class ProviderResult:
    content: str
    model_name: str
    prompt_version: str
    duration_ms: int


class GenerationProvider(Protocol):
    async def generate(self, request: GenerationRequest) -> ProviderResult:
        ...


class GenerationProviderError(Exception):
    def __init__(self, code: str, public_message: str, status_code: int = 503):
        super().__init__(public_message)
        self.code = code
        self.public_message = public_message
        self.status_code = status_code
