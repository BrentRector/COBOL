export const meta = {
  name: 'lane3-refute-from-disk',
  description: 'Adjudication lane, resume-safe stage 2: adjudicate any subject whose out-file is missing (checkpointed per rule), then refute the CONFORMS verdicts read FROM DISK four files at a time, each refuter checkpointing per rule — nothing depends on a prior run\'s in-memory results',
  phases: [
    { title: 'Adjudicate', detail: 'only the subjects with no out-<slug>.json yet' },
    { title: 'Refute', detail: 'four at a time; CONFORMS records read from out-<slug>.json; checkpoint per rule' },
  ],
}

const IN = args.inDir, OUT = args.outDir, PIN = args.pinnedTree
const FILES = args.files, MISSING = args.adjudicateFirst || []
const N = args.refuteConcurrency || 4
if (!IN || !OUT || !PIN || !Array.isArray(FILES) || !FILES.length) throw new Error('pass args.inDir, args.outDir, args.pinnedTree, args.files')

const RECORD = { type: 'object', properties: {
  'rule-id': { type: 'string' },
  verdict: { type: 'string', enum: ['CONFORMS', 'PARTIAL', 'DIVERGES', 'NOT-IMPLEMENTED', 'NEEDS-OWNER-DECISION'] },
  'code-location': { type: 'string' }, 'test-ref': { type: 'string' }, editions: { type: 'string' }, notes: { type: 'string' },
}, required: ['rule-id', 'verdict', 'code-location', 'test-ref', 'editions', 'notes'] }
const ADJ_OUT = { type: 'object', properties: {
  slug: { type: 'string' }, records: { type: 'array', items: RECORD },
  findings: { type: 'array', items: { type: 'object', properties: {
    mechanism: { type: 'string' }, rule_ids: { type: 'array', items: { type: 'string' } }, harm: { type: 'string', enum: ['wrong-answer', 'crashes', 'rejects-legal-source', 'under-rejects', 'silent'] },
    repro: { type: 'string' }, expected_from_spec: { type: 'string' }, observed_or_reasoning: { type: 'string' }, code_site: { type: 'string' }, clause: { type: 'string' },
  }, required: ['mechanism', 'rule_ids', 'harm', 'repro', 'expected_from_spec', 'observed_or_reasoning', 'code_site', 'clause'] } },
  dossier_gaps: { type: 'array', items: { type: 'string' } }, citations_checked: { type: 'array', items: { type: 'string' } }, summary: { type: 'string' },
  // OPTIONAL. Rule-ids you did not decide because you reached the 160-turn cap. Never a count — the ids, so the
  // registrar can dispatch exactly them and reconcile read == decided + |deferred|.
  deferred: { type: 'array', items: { type: 'string' } },
}, required: ['slug', 'records', 'findings', 'dossier_gaps', 'citations_checked', 'summary'] }
const REF_OUT = { type: 'object', properties: {
  slug: { type: 'string' },
  verdicts: { type: 'array', items: { type: 'object', properties: {
    'rule-id': { type: 'string' }, upheld: { type: 'boolean' }, corrected_verdict: { type: 'string' }, why: { type: 'string' }, evidence: { type: 'string' },
  }, required: ['rule-id', 'upheld', 'corrected_verdict', 'why', 'evidence'] } },
  what_i_tried: { type: 'string' }, closing_count_read: { type: 'integer', description: 'how many CONFORMS records you read from out-<slug>.json (0 => say so; do not invent)' },
  // OPTIONAL. Rule-ids you read but did not refute because you reached the 160-turn cap — as ids, never a count.
  deferred: { type: 'array', items: { type: 'string' } },
}, required: ['slug', 'verdicts', 'what_i_tried', 'closing_count_read'] }

const STOP = args.stopFile || ''
const COMMON = `
⛔ PUBLIC SKILLS (MANDATORY-PRACTICES P10): read E:\\claude-skills\\skills\\spec-compliance-audit\\SKILL.md and E:\\claude-skills\\skills\\spec-oracle\\SKILL.md first; the project's spec-lookup skill and this prompt win on conflict.
${STOP ? `⛔ GRACEFUL STOP (MANDATORY-PRACTICES P3): before EACH rule check for the file ${STOP}; if it exists, make sure every decided rule is
in your checkpoint file and return at once with what is decided (summary starting "STOPPED"). Never start a probe once it exists.
` : ''}You work in the COBOL.NET project. SOURCE OF TRUTH FOR CODE: the PINNED worktree ${PIN} (read-only snapshot; it carries its
OWN built compiler at ${PIN}/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.exe — use THAT for probes, never the main tree's).
SPEC: E:\\COBOL\\specs\\ISO_COBOL.md (ISO/IEC 1989:2023). Read E:\\COBOL\\CLAUDE.md first — its eight rules bind you.
You may run python scripts under ${PIN}/scripts (cite.py) and the pinned cobol.exe (scratch programs under ${OUT}/probe/<slug>/).
You MUST NOT run dotnet build/test, git, or write inside any repository tree; write only under ${OUT}.
⛔ CHECKPOINT PER RULE: your checkpoint file is named below. FIRST read it if it exists and SKIP every rule already in it;
then APPEND one JSON line the moment each rule is decided (python open(path,'a'); never rewrite the file). Your final
structured return is the union of the file's lines and your new ones. A session-limit kill can come at any moment; the
file is what survives.
THE SPEC IS THE ONLY ORACLE; NIST/GnuCOBOL/legacy are regression nets. VALIDATE EVERY CITATION with
python ${PIN}/scripts/spec/cite.py --check <clause> "<text>". EDITIONS from the spec, never from the code under review.
⛔ HARD CAP: 160 TURNS (read-only work; owner decision 2026-09-04). Agent cost is QUADRATIC in turns
(tokens ~ 0.115*T + 0.00031*T^2, n=239), so a long transcript is the most expensive thing this project runs and the
work is SPLIT, never extended. As you approach the cap: append every decided rule to your checkpoint file, then
RETURN what you have. Rules you did not decide go in the optional "deferred" array of your return, as rule-ids —
naming them is REQUIRED, a silent short population is the failure mode (a missing observation is not a negative
one). The merge/registrar step reads "deferred" and dispatches a fresh agent for exactly those ids, from your
checkpoint file. Stopping at the cap with an honest deferred list is the CORRECT outcome, never a failure.
CONFORMS COSTS SOMETHING (unlocated => NOT-IMPLEMENTED; an unverified edge => PARTIAL; two halves one verified => PARTIAL).
DOCUMENTED-NON-SUPPORT is never yours (owner, D13; declined modules are owned by PB260/PB261/PB281–PB283 — say so in notes).
ONLY A SPEC-DERIVED TEST CLOSES A ROW (conformance:/unit:/conformance-test: with spec-computed expectations; nist:/
characterization:/*_MatchesLegacy never). CONFORMS-but-untested (empty test-ref) is expected; do not write goldens here.
The dossier is a MAP: its silence is not evidence.`

phase('Adjudicate')
const adj = []
for (let i = 0; i < MISSING.length; i += N) {
  const chunk = MISSING.slice(i, i + N)
  const rs = await parallel(chunk.map(slug => () => agent(`${COMMON}

YOUR INPUT FILE: ${IN}/in-${slug}.json — read it whole (rules, sibling parts, clause map, DOSSIER with grammar/source/
tests/determinations/register notes with status — never re-report a landed fix; reference an open note that already owns
a row — and DIAGNOSTICS rows). YOUR CHECKPOINT FILE: ${OUT}/adjudicate-${slug}.jsonl (one RECORD per line).
Per rule: read the rule text and its general format; locate the code from the dossier and confirm by reading it; verify
across the applicable editions and the adversarial edges the text implies (probe with the pinned cobol.exe when reading
cannot decide); assign the verdict; name an EXISTING spec-derived covering test or leave test-ref empty. Cluster defective
rows by MECHANISM into findings (register-ready paragraph each, with repro + spec-derived expected result); write them to
${OUT}/findings-${slug}.json as you go. When done, write the whole structured result to ${OUT}/out-${slug}.json and return it.`,
  { label: `adjudicate:${slug}`, phase: 'Adjudicate', model: 'opus', agentType: 'cobol-adjudicator', schema: ADJ_OUT })))
  rs.forEach(r => { if (r) adj.push(r) })
  log(`Adjudicate: ${Math.min(i + N, MISSING.length)}/${MISSING.length}`)
}
log(`Adjudicate: ${adj.length}/${MISSING.length} subjects completed`)

phase('Refute')
const results = []
for (let i = 0; i < FILES.length; i += N) {
  const chunk = FILES.slice(i, i + N)
  const rs = await parallel(chunk.map(slug => () => agent(`${COMMON}

You are the INDEPENDENT REFUTER for subject file ${slug}. READ THE ADJUDICATION FROM DISK: ${OUT}/out-${slug}.json (if it
does not exist, read every line of ${OUT}/adjudicate-${slug}.jsonl instead; if neither exists, return zero verdicts and
closing_count_read = 0 — do not invent). Refute ONLY its CONFORMS records — they move the burn-down and are the only ones
dangerous when wrong (a wrong PARTIAL is re-probed at fix time by rule). Every batch so far overturned some of them, always
DOWNWARD; an all-upheld report is a red flag — say what you did to break it. For each: read the rule text yourself FIRST
(${IN}/in-${slug}.json carries it) and derive what the code must do; then read the code at the adjudicator's code-location
in ${PIN}; probe with the pinned cobol.exe when reading cannot decide (edge values, other editions, the operand shapes the
rule names); check the cited test-ref really pins THIS rule's branch and is spec-derived; re-run the citation through
cite.py. Default to refuted when uncertain and name the corrected verdict.
YOUR CHECKPOINT FILE: ${OUT}/refute-${slug}.jsonl (one verdict object per line; read-and-skip first).`,
    { label: `refute:${slug}`, phase: 'Refute', model: 'opus', agentType: 'cobol-refuter', schema: REF_OUT })))
  rs.forEach((r, j) => results.push({ slug: chunk[j], refutation: r }))
  log(`Refute: ${Math.min(i + N, FILES.length)}/${FILES.length}`)
}
return { adjudicated: adj.filter(Boolean).map(a => a.slug), refutations: results }
