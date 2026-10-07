import DOMPurify from 'dompurify';

const ALLOWED_TAGS = ['p', 'br', 'strong', 'em', 'h3', 'ul', 'ol', 'li', 'b', 'i', 'u', 'div'];
const ALLOWED_HTML_PATTERN = /<(?:p|br|strong|em|h3|ul|ol|li|b|i|u|div)\b/i;

export const sanitizeDraftHtml = (value: string): string => DOMPurify.sanitize(value, {
  ALLOWED_TAGS,
  ALLOWED_ATTR: [],
});

const escapeHtml = (value: string): string => value
  .replace(/&/g, '&amp;')
  .replace(/</g, '&lt;')
  .replace(/>/g, '&gt;')
  .replace(/"/g, '&quot;')
  .replace(/'/g, '&#39;');

export function renderDraftHtml(value: string): string {
  if (ALLOWED_HTML_PATTERN.test(value)) {
    return sanitizeDraftHtml(value);
  }

  return value
    .split(/\n{2,}/)
    .filter(paragraph => paragraph.trim())
    .map(paragraph => `<p>${escapeHtml(paragraph.trim()).replace(/\n/g, '<br>')}</p>`)
    .join('');
}

export function draftToPlainText(value: string): string {
  const container = document.createElement('div');
  container.innerHTML = renderDraftHtml(value);
  return (container.innerText || container.textContent || '').trim();
}

export async function copyDraft(value: string): Promise<void> {
  const html = renderDraftHtml(value);
  const plainText = draftToPlainText(value);

  if (typeof ClipboardItem !== 'undefined' && navigator.clipboard.write) {
    try {
      await navigator.clipboard.write([new ClipboardItem({
        'text/html': new Blob([html], { type: 'text/html' }),
        'text/plain': new Blob([plainText], { type: 'text/plain' }),
      })]);
      return;
    } catch {
      // Le presse-papiers riche peut être refusé ; le texte brut reste disponible.
    }
  }

  await navigator.clipboard.writeText(plainText);
}
