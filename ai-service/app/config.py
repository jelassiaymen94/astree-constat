from functools import lru_cache
from typing import Literal

from pydantic import Field, SecretStr
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )

    llm_provider: Literal["deterministic", "groq"] = "deterministic"
    groq_api_key: SecretStr | None = None
    groq_model: str = "llama-3.3-70b-versatile"
    groq_temperature: float = Field(default=0.2, ge=0.0, le=2.0)
    groq_max_tokens: int = Field(default=1000, ge=1, le=8192)
    groq_timeout_seconds: float = Field(default=20.0, gt=0.0, le=120.0)


@lru_cache
def get_settings() -> Settings:
    return Settings()
