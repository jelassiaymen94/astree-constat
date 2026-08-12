import { FormEvent, useEffect, useState } from 'react';
import { claims, context, generate, history } from './api';
import type { Claim, ClaimContext, Generation, GenType, Page } from './types';

type View = 'claims' | 'claim' | 'assistant';

const money = (value: number) => new Intl.NumberFormat('fr-TN', {
  style: 'currency', currency: 'TND', minimumFractionDigits: 3,
}).format(value);
const date = (value: string) => new Intl.DateTimeFormat('fr-TN', { dateStyle: 'medium' }).format(new Date(value));
const readableStatus = (value: string) => value.replace(/_+/g, ' ').replace(/\bd\s+([aeiouyhàâäéèêëîïôöùûü])/gi, 'd’$1').trim();
const labels: Record<GenType, string> = {
  summary: 'Synthèse interne', letter: 'Courrier à l’assuré', response: 'Réponse contextualisée',
};

export default function App() {
  const [data, setData] = useState<Page<Claim>>();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [draftSearch, setDraftSearch] = useState('');
  const [status, setStatus] = useState('');
  const [type, setType] = useState('');
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState('');
  const [view, setView] = useState<View>('claims');
  const [selected, setSelected] = useState<string>();
  const [ctx, setCtx] = useState<ClaimContext>();
  const [logs, setLogs] = useState<Generation[]>([]);
  const [kind, setKind] = useState<GenType>('summary');
  const [instruction, setInstruction] = useState('');
  const [result, setResult] = useState<Generation>();
  const [generating, setGenerating] = useState(false);

  useEffect(() => {
    let live = true;
    setBusy(true);
    setError('');
    claims(page, search, status, type)
      .then(value => live && setData(value))
      .catch(reason => live && setError(reason.message))
      .finally(() => live && setBusy(false));
    return () => { live = false; };
  }, [page, search, status, type]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setPage(1);
      setSearch(draftSearch.trim());
    }, 350);
    return () => window.clearTimeout(timer);
  }, [draftSearch]);

  function showClaims() {
    setView('claims');
    setSelected(undefined);
    setCtx(undefined);
    setResult(undefined);
    setError('');
  }

  async function openClaim(id: string) {
    setSelected(id);
    setView('claim');
    setCtx(undefined);
    setResult(undefined);
    setError('');
    scrollTo(0, 0);
    try {
      const [claimContext, generations] = await Promise.all([context(id), history(id)]);
      setCtx(claimContext);
      setLogs(generations);
    } catch (reason) {
      setError((reason as Error).message);
    }
  }

  function showAssistant() {
    if (!selected) return;
    setView('assistant');
    setError('');
    scrollTo(0, 0);
  }

  async function make(event: FormEvent) {
    event.preventDefault();
    if (!selected) return;
    setGenerating(true);
    setError('');
    try {
      const generation = await generate(selected, kind, instruction);
      setResult(generation);
      setLogs(current => [generation, ...current.filter(item => item.generationId !== generation.generationId)]);
    } catch (reason) {
      setError((reason as Error).message);
    } finally {
      setGenerating(false);
    }
  }

  return <div className="shell">
    <aside>
      <div className="brand"><b>A</b><div><strong>ASTREE</strong><span>Claims AI</span></div></div>
      <nav aria-label="Navigation principale">
        <button className={view === 'claims' || view === 'claim' ? 'active' : ''} onClick={showClaims}>▣ Sinistres</button>
        <button className={view === 'assistant' ? 'active' : ''} disabled={!selected} onClick={showAssistant}>✦ Assistant IA</button>
        <button disabled title="L’historique sera disponible prochainement">◷ Historique</button>
      </nav>
      <div className="notice"><strong>Validation humaine</strong><span>L’IA assiste, le gestionnaire décide.</span></div>
    </aside>
    <main>
      <header><span>● Environnement de démonstration</span><div className="user">AJ</div></header>
      {view === 'claims' && <ClaimsList data={data} page={page} busy={busy} error={error} draftSearch={draftSearch} status={status} type={type} setDraftSearch={setDraftSearch} setStatus={setStatus} setType={setType} setPage={setPage} setSearch={setSearch} openClaim={openClaim} />}
      {view === 'claim' && <ClaimDetail ctx={ctx} logs={logs} error={error} showClaims={showClaims} showAssistant={showAssistant} />}
      {view === 'assistant' && <AssistantView ctx={ctx} kind={kind} instruction={instruction} result={result} generating={generating} error={error} setKind={setKind} setInstruction={setInstruction} make={make} />}
    </main>
  </div>;
}

function ClaimsList({ data, page, busy, error, draftSearch, status, type, setDraftSearch, setStatus, setType, setPage, setSearch, openClaim }: {
  data?: Page<Claim>; page: number; busy: boolean; error: string; draftSearch: string; status: string; type: string;
  setDraftSearch: (value: string) => void; setStatus: (value: string) => void; setType: (value: string) => void;
  setPage: (value: number) => void; setSearch: (value: string) => void; openClaim: (id: string) => void;
}) {
  return <section>
    <div className="heading"><div><small>GESTION DES DOSSIERS</small><h1>Sinistres automobiles</h1><p>Consultez les dossiers et préparez des brouillons avec l’assistant IA.</p></div><div className="metric"><span>Dossiers disponibles</span><b>{data?.total ?? '—'}</b><small>Base de démonstration</small></div></div>
    <form className="filters" onSubmit={event => { event.preventDefault(); setPage(1); setSearch(draftSearch.trim()); }}>
      <input value={draftSearch} onChange={event => setDraftSearch(event.target.value)} placeholder="⌕  Rechercher une référence ou un assuré…" aria-label="Rechercher par référence ou nom de l’assuré" />
      <input value={status} onChange={event => { setStatus(event.target.value); setPage(1); }} placeholder="Tous les statuts" />
      <input value={type} onChange={event => { setType(event.target.value); setPage(1); }} placeholder="Tous les types" />
      <button>Rechercher</button>
    </form>
    <div className="panel"><div className="panelHead"><div><h2>Liste des sinistres</h2><span>{data?.total ?? 0} dossiers</span></div><i>● API connectée</i></div>
      {error ? <State text={error} /> : busy ? <State text="Recherche en cours…" /> : data?.items.length ? <><div className="table"><table><thead><tr><th>Référence</th><th>Date</th><th>Type</th><th>Statut</th><th>Montant estimé</th><th /></tr></thead><tbody>{data.items.map(claim => <tr key={claim.claimId} onClick={() => openClaim(claim.claimId)}><td><b>{claim.claimId}</b></td><td>{date(claim.date)}</td><td>{claim.type}</td><td><em>{readableStatus(claim.status)}</em></td><td><strong>{money(claim.estimatedAmount)}</strong></td><td>Ouvrir →</td></tr>)}</tbody></table></div><div className="pager"><button disabled={page === 1} onClick={() => setPage(page - 1)}>← Précédent</button><span>Page {page} sur {Math.max(1, Math.ceil(data.total / 12))}</span><button disabled={page * 12 >= data.total} onClick={() => setPage(page + 1)}>Suivant →</button></div></> : <State text="Aucun sinistre trouvé." />}
    </div>
  </section>;
}

function ClaimDetail({ ctx, logs, error, showClaims, showAssistant }: { ctx?: ClaimContext; logs: Generation[]; error: string; showClaims: () => void; showAssistant: () => void }) {
  if (!ctx) return <section><State text={error || 'Chargement du dossier…'} /></section>;
  return <section>
    <button className="back" onClick={showClaims}>← Retour aux sinistres</button>
    <div className="detailTitle"><div><h1>Dossier {ctx.claim.claimId}</h1><p>{ctx.claim.type} · déclaré le {date(ctx.claim.date)}</p></div><em>{readableStatus(ctx.claim.status)}</em></div>
    <div className="cards"><article className="panel claim"><small>SINISTRE</small><h2>Résumé du dossier</h2><p>{ctx.claim.description}</p><div className="amounts"><span>Montant estimé<b>{money(ctx.claim.estimatedAmount)}</b></span><span>Indemnisation<b>{money(ctx.claim.compensationAmount)}</b></span></div></article><Card title="ASSURÉ" name={`${ctx.customer.firstName} ${ctx.customer.lastName}`} lines={[ctx.customer.clientId, ctx.customer.governorate]} /><Card title="CONTRAT" name={ctx.contract.contractId} lines={[ctx.contract.coverageType, `${date(ctx.contract.startDate)} – ${date(ctx.contract.endDate)}`]} /><Card title="VÉHICULE" name={`${ctx.vehicle.brand} ${ctx.vehicle.model}`} lines={[ctx.vehicle.registrationNumber, ctx.vehicle.type]} /></div>
    <article className="panel assistantCta"><small>ASSISTANT RÉDACTIONNEL</small><h2>Préparer un brouillon à partir de ce dossier</h2><p>L’assistant utilisera uniquement le contexte consolidé affiché ci-dessus.</p><button className="assistantOpenButton" type="button" onClick={showAssistant}>✦ Ouvrir l’assistant IA</button></article>
    <article className="panel history"><small>TRAÇABILITÉ</small><h2>Dernières générations</h2>{logs.length ? logs.slice(0, 3).map(item => <details key={item.generationId}><summary><b>{item.success ? '✓' : '!'} {labels[item.generationType]}</b><span>{date(item.createdAt)} · {item.modelName || '—'} · {item.durationMs ?? '—'} ms</span></summary><p>{item.generatedContent || item.errorMessage}</p></details>) : <p>Aucune génération enregistrée.</p>}</article>
  </section>;
}

function AssistantView({ ctx, kind, instruction, result, generating, error, setKind, setInstruction, make }: { ctx?: ClaimContext; kind: GenType; instruction: string; result?: Generation; generating: boolean; error: string; setKind: (value: GenType) => void; setInstruction: (value: string) => void; make: (event: FormEvent) => void }) {
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    setCopied(false);
  }, [result?.generationId]);

  async function copyDraft() {
    if (!result?.generatedContent) return;
    await navigator.clipboard.writeText(result.generatedContent);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 3000);
  }

  if (!ctx) return <section><State text={error || 'Chargement du contexte…'} /></section>;
  return <section>
    <div className="detailTitle"><div><small>ASSISTANT RÉDACTIONNEL</small><h1>Assistant IA</h1><p>Dossier {ctx.claim.claimId} · {ctx.customer.firstName} {ctx.customer.lastName}</p></div><em>{readableStatus(ctx.claim.status)}</em></div>
    <div className="assistantContext"><span><small>SINISTRE</small><b>{ctx.claim.type}</b></span><span><small>VÉHICULE</small><b>{ctx.vehicle.brand} {ctx.vehicle.model}</b></span><span><small>MONTANT ESTIMÉ</small><b>{money(ctx.claim.estimatedAmount)}</b></span></div>
    <article className="panel assistant"><small>GÉNÉRATION ASSISTÉE</small><h2>Préparer un brouillon</h2><p>L’assistant utilise uniquement le contexte consolidé de ce dossier. Le résultat devra être relu avant toute utilisation.</p>
      <form onSubmit={make}><div className="choices">{(Object.keys(labels) as GenType[]).map(value => <label key={value} className={kind === value ? 'on' : ''}><input type="radio" checked={kind === value} onChange={() => setKind(value)} /><b>{labels[value]}</b></label>)}</div><textarea value={instruction} onChange={event => setInstruction(event.target.value)} maxLength={1000} placeholder="Instruction complémentaire (facultatif)" /><div className="instructionCount">{instruction.length}/1000</div><button disabled={generating}>{generating ? 'Génération en cours…' : '✦ Générer le brouillon'}</button></form>
      {error && <p className="error">{error}</p>}{result?.generatedContent && <div className="result"><div><b>Brouillon généré</b><button className={copied ? 'copyButton copied' : 'copyButton'} type="button" onClick={copyDraft} aria-live="polite">{copied ? '✓ Brouillon copié' : 'Copier'}</button></div><p>{result.generatedContent}</p><aside><b>Validation humaine obligatoire</b><span>Relisez ce contenu avant toute utilisation.</span></aside></div>}
    </article>
  </section>;
}

function Card({ title, name, lines }: { title: string; name: string; lines: string[] }) { return <article className="panel card"><small>{title}</small><h2>{name}</h2>{lines.map(line => <p key={line}>{line}</p>)}</article>; }
function State({ text }: { text: string }) { return <div className="state">{text}</div>; }
