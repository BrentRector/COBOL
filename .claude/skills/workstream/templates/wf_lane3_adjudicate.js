export const meta = {
  name: 'lane3-phase-b-adjudicate',
  description: 'Adjudication lane: one Opus adjudicator per dossier-fed input file verifies every rule against the ISO text and the pinned source tree; an independent refuter attacks ONLY the closing verdicts (CONFORMS / DOCUMENTED-NON-SUPPORT). Both stages CHECKPOINT per rule to disk so a session-limit kill loses at most one rule, and refuters run four at a time',
  phases: [
    { title: 'Adjudicate', detail: 'per subject file: locate, verify across editions, verdict, spec-derived test-ref or test-needed — checkpointed per rule' },
    { title: 'Refute', detail: 'only the closing verdicts, four refuters at a time, checkpointed per rule' },
  ],
}

const IN = args.inDir
const OUT = args.outDir
const PIN = args.pinnedTree
const FILES = args.files
const REFUTE_CONCURRENCY = args.refuteConcurrency || 4
if (!IN || !OUT || !PIN || !Array.isArray(FILES) || !FILES.length) throw new Error('pass args.inDir, args.outDir, args.pinnedTree, args.files')
log(`Lane 3 adjudication over ${FILES.length} subject files against pinned tree ${PIN}; refuters ${REFUTE_CONCURRENCY} at a time`)

const RECORD = {
  type: 'object',
  properties: {
    'rule-id': { type: 'string' },
    verdict: { type: 'string', enum: ['CONFORMS', 'PARTIAL', 'DIVERGES', 'NOT-IMPLEMENTED', 'NEEDS-OWNER-DECISION'] },
    'code-location': { type: 'string', description: 'path#Symbol into src/Cobol.Net.*; several separated by "; "; empty only for NOT-IMPLEMENTED' },
    'test-ref': { type: 'string', description: 'a SPEC-DERIVED covering test that already exists (conformance:<edition>/<case>, unit:<Class>.<Method>, conformance-test:<Class>.<Method>) or empty = test-needed; nist:/characterization: never close' },
    editions: { type: 'string', description: 'from the SPEC (Annex E / the clause), never from the code under review' },
    notes: { type: 'string', description: 'the derivation: the rule text quoted, what the code does, the cite.py --check line; for a defective verdict the repro program and the spec-derived expected result' },
  },
  required: ['rule-id', 'verdict', 'code-location', 'test-ref', 'editions', 'notes'],
}
const ADJ_OUT = {
  type: 'object',
  properties: {
    slug: { type: 'string' },
    records: { type: 'array', items: RECORD },
    findings: { type: 'array', description: 'register-ready defect paragraphs, one per MECHANISM (cluster the rows)', items: { type: 'object', properties: {
      mechanism: { type: 'string' }, rule_ids: { type: 'array', items: { type: 'string' } }, harm: { type: 'string', enum: ['wrong-answer', 'crashes', 'rejects-legal-source', 'under-rejects', 'silent'] },
      repro: { type: 'string' }, expected_from_spec: { type: 'string' }, observed_or_reasoning: { type: 'string' }, code_site: { type: 'string' }, clause: { type: 'string' },
    }, required: ['mechanism', 'rule_ids', 'harm', 'repro', 'expected_from_spec', 'observed_or_reasoning', 'code_site', 'clause'] } },
    dossier_gaps: { type: 'array', items: { type: 'string' } },
    citations_checked: { type: 'array', items: { type: 'string' } },
    summary: { type: 'string' },
  },
  required: ['slug', 'records', 'findings', 'dossier_gaps', 'citations_checked', 'summary'],
}
const REF_OUT = {
  type: 'object',
  properties: {
    slug: { type: 'string' },
    verdicts: { type: 'array', items: { type: 'object', properties: {
      'rule-id': { type: 'string' }, upheld: { type: 'boolean' },
      corrected_verdict: { type: 'string', description: 'the verdict that survives; empty when upheld' },
      why: { type: 'string' }, evidence: { type: 'string' },
    }, required: ['rule-id', 'upheld', 'corrected_verdict', 'why', 'evidence'] } },
    what_i_tried: { type: 'string' },
  },
  required: ['slug', 'verdicts', 'what_i_tried'],
}

// A stop is SCOPED (MANDATORY-PRACTICES P3, kb/Work PB2483): args.stopFile is THIS fleet's own STOP-<scope> file and
// args.globalStopFile the owner's global stop (<coord>\\scratch\\STOP); an agent obeys either, and no other session's.
const STOPS = [args.stopFile, args.globalStopFile].filter(Boolean)
const COMMON = `
⛔ PUBLIC SKILLS (MANDATORY-PRACTICES P10): read E:\\claude-skills\\skills\\spec-compliance-audit\\SKILL.md and E:\\claude-skills\\skills\\spec-oracle\\SKILL.md first; the project's spec-lookup skill and this prompt win on conflict.
${STOPS.length ? `⛔ GRACEFUL STOP (MANDATORY-PRACTICES P3): before EACH rule check for the file(s) ${STOPS.join(' and ')}; if one exists, make sure every decided rule is
in your checkpoint file and return at once with what is decided (summary starting "STOPPED"). Never start a probe once it exists.
` : ''}You work in the WiseOwl COBOL project. SOURCE OF TRUTH FOR CODE: the PINNED worktree ${PIN} (read-only snapshot, so verdicts
are attributable; it carries its OWN built compiler at ${PIN}/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.exe — use THAT
for probes, never the main tree's). SPEC: E:\\COBOL\\specs\\ISO_COBOL.md (ISO/IEC 1989:2023). Read E:\\COBOL\\CLAUDE.md
first — its eight rules bind you. You may run python scripts under ${PIN}/scripts (cite.py) and the pinned cobol.exe
(compile-and-run a scratch program under ${OUT}/probe/<slug>/). You MUST NOT run dotnet build/test, git, or write inside
any repository tree; write only under ${OUT}.
⛔ CHECKPOINT PER RULE. The session limit can kill you at any moment and a killed agent's transcript is worthless; the
checkpoint file is what survives. Your checkpoint file is named below. FIRST read it if it exists and SKIP every rule
already present in it; then, the moment you decide a rule, APPEND one JSON line for it (one line = one rule, flush
immediately — write with python's open(path,'a') or a single-line append; never rewrite the whole file). Your final
structured return is the union of the file's lines and your new ones.
THE SPEC IS THE ONLY ORACLE. NIST goldens and GnuCOBOL are regression nets with known holes.
VALIDATE EVERY CITATION: python ${PIN}/scripts/spec/cite.py --check <clause> "<quoted text>" — a citation you did not
--check is not a citation, and a REAL clause that answers a DIFFERENT question is the failure mode to fear most.
EDITIONS come from the spec (Annex E, the clause's own text), never from the code under review.
CONFORMS COSTS SOMETHING: code not located => NOT-IMPLEMENTED; an edge unverified => PARTIAL; a rule with two halves
where one was verified => PARTIAL, always. DOCUMENTED-NON-SUPPORT is NEVER yours to choose (owner decision, D13):
where a rule belongs to a facility docs/CONFORMANCE.md §5 declares Not claimed, say so in notes and give
NEEDS-OWNER-DECISION only if no derived selector already covers it (the dossier lists the register notes that own rows —
PB260/PB261/PB281–PB283 own declined-module witness debt).
ONLY A SPEC-DERIVED TEST CLOSES A ROW: conformance:<edition>/<case>, unit:<Class>.<Method>, conformance-test:<Class>.<Method>
whose expected values are computed from the spec; nist:, characterization: and *_MatchesLegacy methods never qualify.
CONFORMS-but-untested (empty test-ref) is a legitimate, expected outcome — do not write goldens here.
The dossier in your input file is a MAP, not the territory: its silence is not evidence — a NOT-IMPLEMENTED verdict
needs your own search of ${PIN}/src.`

phase('Adjudicate')
const adjudicated = await pipeline(FILES, slug => agent(`${COMMON}

YOUR INPUT FILE: ${IN}/in-${slug}.json — read it whole (rules with id/kind/text/section, sibling parts, clause map, the
DOSSIER: grammar rules, citing source with lines, citing tests/goldens, CONFORMANCE.md determinations, kb/Work notes with
status — do NOT re-report a landed fix; an open/half note already owns its rows: reference it in notes — and
DIAGNOSTICS.md rows).
YOUR CHECKPOINT FILE: ${OUT}/adjudicate-${slug}.jsonl — one RECORD (the schema's record object) per line, appended the
moment each rule is decided; read-and-skip first.
Per rule: read the rule text and its general format; locate the implementing code from the dossier and confirm by
reading it; verify across the applicable editions and the adversarial edge cases the text implies (probe with the pinned
cobol.exe when reading cannot decide); assign the verdict; name an EXISTING spec-derived covering test or leave test-ref
empty. Cluster defective rows by MECHANISM into findings (one register-ready paragraph per mechanism, with a repro and
the spec-derived expected result); write the findings to ${OUT}/findings-${slug}.json as you go too.
When every rule is decided, also write the whole structured result to ${OUT}/out-${slug}.json and return it.`,
  { label: `adjudicate:${slug}`, phase: 'Adjudicate', model: 'opus', agentType: 'cobol-adjudicator', schema: ADJ_OUT }))

phase('Refute')
const items = adjudicated.map((a, i) => ({ a, slug: FILES[i] })).filter(x => x.a)
const toRefute = items.filter(x => x.a.records.some(r => r.verdict === 'CONFORMS'))
log(`Refute: ${toRefute.length} of ${items.length} files carry CONFORMS verdicts; running ${REFUTE_CONCURRENCY} at a time`)
const refutations = new Map()
for (let i = 0; i < toRefute.length; i += REFUTE_CONCURRENCY) {
  const chunk = toRefute.slice(i, i + REFUTE_CONCURRENCY)
  const results = await parallel(chunk.map(x => () => {
    const closing = x.a.records.filter(r => r.verdict === 'CONFORMS')
    return agent(`${COMMON}

You are the INDEPENDENT REFUTER for subject file ${x.slug}. Refute ONLY the CONFORMS verdicts below — they move the
burn-down and are the only ones dangerous when wrong (a wrong PARTIAL is re-probed at fix time by rule). Every batch so
far overturned some of them, always DOWNWARD; an all-upheld report is a red flag — say what you did to break it. For
each: read the rule text yourself FIRST and derive what the code must do; then read the code at the adjudicator's
code-location in ${PIN}; probe with the pinned cobol.exe when reading cannot decide (edge values, other editions, the
operand shapes the rule names); check the cited test-ref really pins THIS rule's branch and is spec-derived; re-run
the citation through cite.py. Default to refuted when uncertain and name the corrected verdict.
YOUR CHECKPOINT FILE: ${OUT}/refute-${x.slug}.jsonl — one verdict object (the schema's verdicts item) per line, appended
the moment each rule is decided; read-and-skip first.
Input rows: ${IN}/in-${x.slug}.json. CONFORMS records to attack: ${JSON.stringify(closing).slice(0, 50000)}`,
      { label: `refute:${x.slug}`, phase: 'Refute', model: 'opus', agentType: 'cobol-refuter', schema: REF_OUT })
  }))
  results.forEach((r, j) => { if (r) refutations.set(chunk[j].slug, r) })
  log(`Refute: ${Math.min(i + REFUTE_CONCURRENCY, toRefute.length)}/${toRefute.length} done`)
}

return items.map(({ a, slug }) => {
  const r = refutations.get(slug)
  const overturned = new Map((r ? r.verdicts : []).filter(v => !v.upheld).map(v => [v['rule-id'], v]))
  const records = a.records.map(rec => overturned.has(rec['rule-id'])
    ? { ...rec, verdict: overturned.get(rec['rule-id']).corrected_verdict || 'PARTIAL', notes: rec.notes + ' || REFUTED: ' + overturned.get(rec['rule-id']).why }
    : rec)
  const hadClosing = a.records.some(x => x.verdict === 'CONFORMS')
  return { slug, records, findings: a.findings, dossier_gaps: a.dossier_gaps, refuted: [...overturned.values()],
    refuter_tried: r ? r.what_i_tried : (hadClosing ? 'REFUTER MISSING — do not record the CONFORMS rows' : 'no closing verdicts'), summary: a.summary }
})
