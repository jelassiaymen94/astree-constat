import html
import re

import bleach


ALLOWED_DRAFT_TAGS = ["p", "br", "strong", "em", "h3", "ul", "ol", "li"]
_ALLOWED_TAG_PATTERN = re.compile(r"</?(?:p|br|strong|em|h3|ul|ol|li)\b", re.IGNORECASE)
_DANGEROUS_BLOCK_PATTERN = re.compile(
    r"<(script|style|iframe|object|embed)\b[^>]*>.*?</\1\s*>",
    re.IGNORECASE | re.DOTALL,
)


def humanize_status(value: str) -> str:
    """Convertit une valeur technique en libellé français lisible."""
    normalized = re.sub(r"_+", " ", value).strip()
    normalized = re.sub(r"\bd\s+([aeiouyhàâäéèêëîïôöùûü])", r"d’\1", normalized, flags=re.IGNORECASE)
    return normalized


def normalize_plain_text_draft(value: str) -> str:
    """Normalise un brouillon texte, notamment pour la compatibilité historique."""
    normalized = _normalize_characters(value)

    normalized = re.sub(r"^[ \t]*```[^\n]*$", "", normalized, flags=re.MULTILINE)
    normalized = re.sub(r"^[ \t]*(?:[-*_][ \t]*){3,}$", "", normalized, flags=re.MULTILINE)
    normalized = re.sub(r"^[ \t]{0,3}#{1,6}[ \t]+", "", normalized, flags=re.MULTILINE)
    normalized = re.sub(r"^[ \t]{0,3}>[ \t]?", "", normalized, flags=re.MULTILINE)
    normalized = re.sub(r"^[ \t]{0,3}[-*+][ \t]+", "• ", normalized, flags=re.MULTILINE)
    normalized = re.sub(r"!\[([^\]\n]*)\]\([^\n)]*\)", r"\1", normalized)
    normalized = re.sub(r"\[([^\]\n]+)\]\([^\n)]*\)", r"\1", normalized)
    normalized = re.sub(r"\*\*([^*\n]+)\*\*", r"\1", normalized)
    normalized = re.sub(r"__([^_\n]+)__", r"\1", normalized)
    normalized = re.sub(r"(?<!\*)\*([^*\n]+)\*(?!\*)", r"\1", normalized)
    normalized = normalized.replace("`", "")
    normalized = "\n".join(re.sub(r"[ \t]+", " ", line).strip() for line in normalized.split("\n"))
    normalized = re.sub(r"\n{3,}", "\n\n", normalized)
    return normalized.strip()


def sanitize_html_draft(value: str) -> str:
    """Retourne un fragment HTML sûr limité aux balises de mise en forme métier."""
    normalized = _normalize_characters(value).strip()
    normalized = re.sub(r"^[ \t]*```(?:html)?[ \t]*$", "", normalized, flags=re.IGNORECASE | re.MULTILINE)
    normalized = _DANGEROUS_BLOCK_PATTERN.sub("", normalized)

    if _ALLOWED_TAG_PATTERN.search(normalized):
        fragment = bleach.clean(
            normalized,
            tags=ALLOWED_DRAFT_TAGS,
            attributes={},
            protocols=[],
            strip=True,
            strip_comments=True,
        ).strip()
    else:
        fragment = _plain_text_to_html(normalize_plain_text_draft(normalized))

    visible_text = html.unescape(bleach.clean(fragment, tags=[], attributes={}, strip=True)).strip()
    return fragment if visible_text else ""


def _normalize_characters(value: str) -> str:
    normalized = value.replace("\r\n", "\n").replace("\r", "\n")
    normalized = normalized.translate(str.maketrans({
        "\u00a0": " ",
        "\u202f": " ",
        "\u2010": "-",
        "\u2011": "-",
    }))
    return re.sub(r"(?<=\d)\s*/\s*(?=\d)", "/", normalized)


def _plain_text_to_html(value: str) -> str:
    if not value:
        return ""

    output: list[str] = []
    paragraph: list[str] = []
    list_items: list[str] = []

    def flush_paragraph() -> None:
        if paragraph:
            output.append(f"<p>{'<br>'.join(html.escape(line) for line in paragraph)}</p>")
            paragraph.clear()

    def flush_list() -> None:
        if list_items:
            items = "".join(f"<li>{html.escape(item)}</li>" for item in list_items)
            output.append(f"<ul>{items}</ul>")
            list_items.clear()

    for line in [*value.split("\n"), ""]:
        if not line:
            flush_paragraph()
            flush_list()
        elif line.startswith("• "):
            flush_paragraph()
            list_items.append(line[2:].strip())
        else:
            flush_list()
            paragraph.append(line)

    return "".join(output)
