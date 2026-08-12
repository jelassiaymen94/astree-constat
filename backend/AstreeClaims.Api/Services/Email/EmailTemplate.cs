using System.Net;

namespace AstreeClaims.Api.Services.Email;

public static class EmailTemplate
{
    public static string Render(string subject, string claimId, string contentHtml)
    {
        var safeSubject = WebUtility.HtmlEncode(subject);
        var safeClaimId = WebUtility.HtmlEncode(claimId);
        return $$"""
<!doctype html>
<html lang="fr">
<head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
<body style="margin:0;padding:0;background:#f2f5f4;font-family:Arial,Helvetica,sans-serif;color:#24312d;">
  <div style="display:none;max-height:0;overflow:hidden;color:transparent;">{{safeSubject}}</div>
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="width:100%;background:#f2f5f4;">
    <tr><td align="center" style="padding:32px 16px;">
      <table role="presentation" width="600" cellspacing="0" cellpadding="0" border="0" style="width:100%;max-width:600px;background:#ffffff;border:1px solid #dce5e1;border-radius:12px;overflow:hidden;">
        <tr><td style="background:#0f5b46;padding:24px 30px;color:#ffffff;">
          <table role="presentation" width="100%" cellspacing="0" cellpadding="0"><tr>
            <td style="font-size:22px;font-weight:bold;letter-spacing:1.5px;">ASTREE</td>
            <td align="right" style="font-size:12px;color:#cde7dc;">ASSURANCES</td>
          </tr></table>
        </td></tr>
        <tr><td style="padding:30px 30px 10px;">
          <div style="font-size:12px;font-weight:bold;letter-spacing:1px;color:#0f654c;text-transform:uppercase;">Suivi de dossier</div>
          <h1 style="margin:8px 0 10px;font-size:25px;line-height:1.25;color:#1f2b27;">{{safeSubject}}</h1>
          <p style="margin:0;color:#66736e;font-size:14px;">Référence : <strong style="color:#0f654c;">{{safeClaimId}}</strong></p>
        </td></tr>
        <tr><td style="padding:18px 30px 8px;font-size:15px;line-height:1.7;color:#35423d;">{{contentHtml}}</td></tr>
        <tr><td style="padding:18px 30px;">
          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#edf7f2;border-left:4px solid #2c8a67;border-radius:6px;">
            <tr><td style="padding:14px 16px;color:#315a4b;font-size:13px;line-height:1.5;">
              <strong>Besoin d’informations complémentaires ?</strong><br>
              Notre équipe reste disponible pour vous accompagner dans le suivi de votre dossier.
            </td></tr>
          </table>
        </td></tr>
        <tr><td style="padding:8px 30px 28px;color:#6d7974;font-size:12px;line-height:1.55;">
          Cordialement,<br><strong style="color:#25332e;">Service Sinistres — ASTREE Assurances</strong>
        </td></tr>
        <tr><td style="background:#f7f9f8;border-top:1px solid #e3e9e6;padding:18px 30px;text-align:center;color:#7a8580;font-size:11px;line-height:1.5;">
          Message préparé avec assistance rédactionnelle et validé par un gestionnaire.<br>
          Démonstration ASTREE Claims AI — aucun envoi automatique.
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>
""";
    }
}
