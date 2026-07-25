from time import perf_counter
from typing import Any

from groq import AsyncGroq

from app.config import Settings
from app.models import GenerationRequest
from app.prompts import PROMPT_VERSION, build_messages
from app.providers.base import GenerationProviderError, ProviderResult


class GroqProvider:
    def __init__(self, settings: Settings, client: Any | None = None):
        if settings.groq_api_key is None:
            raise GenerationProviderError(
                "GROQ_CONFIGURATION_ERROR",
                "La configuration Groq est incomplète.",
            )
        self._settings = settings
        self._client = client or AsyncGroq(
            api_key=settings.groq_api_key.get_secret_value(),
            timeout=settings.groq_timeout_seconds,
        )

    async def generate(self, request: GenerationRequest) -> ProviderResult:
        started = perf_counter()
        try:
            completion = await self._client.chat.completions.create(
                model=self._settings.groq_model,
                messages=build_messages(request),
                temperature=self._settings.groq_temperature,
                max_completion_tokens=self._settings.groq_max_tokens,
            )
        except Exception as exception:
            raise self._map_error(exception) from exception

        content = completion.choices[0].message.content if completion.choices else None
        if not content or not content.strip():
            raise GenerationProviderError(
                "GROQ_INVALID_RESPONSE",
                "Groq a retourné une réponse vide ou invalide.",
            )

        duration_ms = max(1, round((perf_counter() - started) * 1000))
        return ProviderResult(
            content=content.strip(),
            model_name=getattr(completion, "model", None) or self._settings.groq_model,
            prompt_version=PROMPT_VERSION,
            duration_ms=duration_ms,
        )

    @staticmethod
    def _map_error(exception: Exception) -> GenerationProviderError:
        status_code = getattr(exception, "status_code", None)
        exception_name = type(exception).__name__.lower()

        if "timeout" in exception_name or isinstance(exception, TimeoutError):
            return GenerationProviderError(
                "GROQ_TIMEOUT",
                "La requête Groq a expiré.",
            )
        if status_code in (401, 403):
            return GenerationProviderError(
                "GROQ_AUTHENTICATION_ERROR",
                "L'authentification Groq a échoué.",
            )
        if status_code == 429:
            return GenerationProviderError(
                "GROQ_RATE_LIMITED",
                "La limite de requêtes Groq a été atteinte.",
            )
        if status_code == 498:
            return GenerationProviderError(
                "GROQ_CAPACITY_EXCEEDED",
                "La capacité Groq est temporairement indisponible.",
            )
        if status_code in (400, 413, 422):
            return GenerationProviderError(
                "GROQ_INVALID_REQUEST",
                "Groq a refusé la requête de génération.",
            )
        if isinstance(status_code, int) and status_code >= 500:
            return GenerationProviderError(
                "GROQ_UNAVAILABLE",
                "Le service Groq est temporairement indisponible.",
            )
        return GenerationProviderError(
            "GROQ_UNEXPECTED_ERROR",
            "La génération Groq a échoué.",
        )
