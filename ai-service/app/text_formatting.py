import re


def humanize_status(value: str) -> str:
    """Convertit une valeur technique en libellé français lisible."""
    normalized = re.sub(r"_+", " ", value).strip()
    normalized = re.sub(r"\bd\s+([aeiouyhàâäéèêëîïôöùûü])", r"d’\1", normalized, flags=re.IGNORECASE)
    return normalized
