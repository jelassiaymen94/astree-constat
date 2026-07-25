from functools import lru_cache

from app.config import get_settings
from app.providers.base import GenerationProvider, GenerationProviderError
from app.providers.deterministic import DeterministicProvider
from app.providers.groq_provider import GroqProvider


@lru_cache
def get_provider() -> GenerationProvider:
    settings = get_settings()
    if settings.llm_provider == "deterministic":
        return DeterministicProvider()
    if settings.llm_provider == "groq":
        return GroqProvider(settings)
    raise GenerationProviderError(
        "LLM_PROVIDER_UNSUPPORTED",
        "Le fournisseur LLM configuré n'est pas pris en charge.",
    )
