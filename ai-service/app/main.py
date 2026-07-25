from fastapi import Depends, FastAPI, Request
from fastapi.responses import JSONResponse

from app.config import get_settings
from app.models import GenerationRequest, GenerationResponse
from app.providers import get_provider
from app.providers.base import GenerationProvider, GenerationProviderError

app = FastAPI(
    title="ASTREE Claims AI Service",
    version="1.2.0",
)


@app.exception_handler(GenerationProviderError)
async def provider_error_handler(
    _request: Request,
    exception: GenerationProviderError,
):
    return JSONResponse(
        status_code=exception.status_code,
        content={
            "error": {
                "code": exception.code,
                "message": exception.public_message,
            }
        },
    )


@app.get("/health")
def health():
    settings = get_settings()
    return {
        "service": "astree-ai-service",
        "status": "healthy",
        "provider": settings.llm_provider,
    }


@app.post("/api/v1/generate", response_model=GenerationResponse)
async def generate(
    request: GenerationRequest,
    provider: GenerationProvider = Depends(get_provider),
):
    generated = await provider.generate(request)
    return GenerationResponse(
        content=generated.content,
        model_name=generated.model_name,
        prompt_version=generated.prompt_version,
        duration_ms=generated.duration_ms,
    )
