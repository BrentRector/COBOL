export const meta = {
  name: 'lane1-golden-draft',
  description: 'Golden lane stage 1: one Opus writer per input file derives spec-derived goldens for CONFORMS-untested and DNS-witness rows into the scratchpad; an independent refuter re-derives every expected value; a fixer applies upheld corrections',
  phases: [
    { title: 'Write', detail: 'one agent per family file: existing-golden reuse or a new spec-derived .cob/.out draft' },
    { title: 'Refute', detail: 'independent re-derivation of every expected value; default to refuted when uncertain' },
    { title: 'Fix', detail: 'apply the refuter corrections to the drafts' },
  ],
}

const SCRATCH = '{SCRATCH}/lane1'
const FILES = args.files
if (!Array.isArray(FILES) || !FILES.length) throw new Error('pass args.files = [slug, ...]')
log(`Lane 1 draft over ${FILES.length} input files`)

const ROW = {
  type: 'object',
  properties: {
    rule_id: { type: 'string' },
    disposition: { type: 'string', enum: ['existing-golden', 'new-golden', 'defect-suspected', 'not-closable'] },
    test_ref: { type: 'string', description: 'conformance:<edition>/<case> or conformance:negative/<case>; empty when not closable' },
    files: { type: 'array', items: { type: 'string' }, description: 'draft files written under out/<slug>/, repo-relative destination paths' },
    derivation: { type: 'string', description: 'how each expected line follows from the quoted rule text — the spec is the only oracle' },
    citations_checked: { type: 'array', items: { type: 'string' }, description: 'each `cite.py --check <clause> "<text>"` command and its verdict line' },
    editions: { type: 'string' },
    notes: { type: 'string' },
  },
  required: ['rule_id', 'disposition', 'test_ref', 'files', 'derivation', 'citations_checked', 'editions', 'notes'],
}
const WRITER_OUT = {
  type: 'object',
  properties: {
    slug: { type: 'string' },
    rows: { type: 'array', items: ROW },
    manifest_entries: { type: 'object', description: 'edition dir -> [case names] for tests/conformance/<edition>/manifest.json enabled', additionalProperties: { type: 'array', items: { type: 'string' } } },
    negative_manifest_entries: { type: 'array', items: { type: 'string' } },
    records: { type: 'array', description: 'record_verdicts.py records for rows that would CLOSE once the goldens are validated', items: { type: 'object', additionalProperties: { type: 'string' } } },
    defects: { type: 'array', items: { type: 'object', properties: { rule_id: { type: 'string' }, repro: { type: 'string' }, expected: { type: 'string' }, observed_or_reasoning: { type: 'string' }, clause: { type: 'string' } }, required: ['rule_id', 'repro', 'expected', 'observed_or_reasoning', 'clause'] } },
    summary: { type: 'string' },
    // OPTIONAL. Rule-ids you did not draft because you reached the 160-turn cap — the IDS, never a count, so the
    // registrar can dispatch exactly them and reconcile read == decided + |deferred|.
    deferred: { type: 'array', items: { type: 'string' } },
  },
  required: ['slug', 'rows', 'manifest_entries', 'negative_manifest_entries', 'records', 'defects', 'summary'],
}
const REFUTE_OUT = {
  type: 'object',
  properties: {
    slug: { type: 'string' },
    verdicts: { type: 'array', items: { type: 'object', properties: {
      rule_id: { type: 'string' },
      upheld: { type: 'boolean' },
      correction: { type: 'string', description: 'what is wrong and exactly what the file/record should say; empty when upheld' },
      kind: { type: 'string', enum: ['expected-value', 'citation', 'does-not-exercise-rule', 'editions', 'not-spec-derived', 'nondeterministic', 'convention', 'none'] },
    }, required: ['rule_id', 'upheld', 'correction', 'kind'] } },
    all_upheld_flag: { type: 'string', description: 'if every row is upheld, say what you did to try to break it' },
    // OPTIONAL. Rule-ids you did not refute because you reached the 160-turn cap — the IDS, never a count.
    deferred: { type: 'array', items: { type: 'string' } },
  },
  required: ['slug', 'verdicts', 'all_upheld_flag'],
}
const FIX_OUT = {
  type: 'object',
  properties: {
    slug: { type: 'string' }, applied: { type: 'array', items: { type: 'string' } }, refused: { type: 'array', items: { type: 'string' } },
    // OPTIONAL. Corrections you did not reach because of the 160-turn cap — the rule-ids, never a count.
    deferred: { type: 'array', items: { type: 'string' } },
  },
  required: ['slug', 'applied', 'refused'],
}

const COMMON = `
You work in the WiseOwl COBOL repository at E:\\COBOL (read CLAUDE.md first — its eight rules bind you).
THE TREE IS FROZEN: a comprehensive battery is running. You may READ anything in the repo and run python scripts
(scripts/spec/cite.py). You MUST NOT run dotnet, cobol.exe, git, or write ANY file inside E:\\COBOL. Every
file you produce goes under ${SCRATCH}/out/<slug>/ using the repo-relative destination path as a subpath
(e.g. ${SCRATCH}/out/<slug>/tests/conformance/2023/<name>.cob). A director copies validated drafts into the tree.
THE SPEC IS THE ONLY ORACLE: specs/ISO_COBOL.md (ISO/IEC 1989:2023). Derive every expected output value from the
rule text BEFORE thinking about what the compiler does; NIST goldens and GnuCOBOL are regression
nets, never authority; a *_MatchesLegacy test or a nist: ref cannot close a row. VALIDATE EVERY CITATION:
python scripts/spec/cite.py --check <clause> "<quoted text>"  — a citation you did not run --check on is not a
citation, and a REAL clause that answers a DIFFERENT question is the failure mode to fear most.
⛔ HARD CAP: 160 TURNS (read-only work; owner decision 2026-09-04). Agent cost is QUADRATIC in turns
(tokens ~ 0.115*T + 0.00031*T^2, n=239), so the work is SPLIT, never extended. As you approach the cap: checkpoint
everything decided to disk, then RETURN what you have. Rule-ids you did not decide go in the optional "deferred"
array of your return — the IDS, never a count; a silent short population is the failure mode. The merge/registrar
step reads "deferred" and dispatches a fresh agent for exactly those ids. Stopping at the cap with an honest
deferred list is the CORRECT outcome, never a failure.`

phase('Write')
const results = await pipeline(
  FILES,
  slug => agent(`${COMMON}

YOUR INPUT FILE: ${SCRATCH}/in/in-${slug}.json — read it whole. It holds inventory rows already adjudicated
(CONFORMS-but-untested, or DOCUMENTED-NON-SUPPORT owing a witness), each with its rule text, the adjudicator's
notes (context only — derive from the RULE TEXT, not from the notes), code-location, editions, and the existing
goldens that mention the subject.

YOUR JOB, per row, in this order:
1. Read the rule text; open specs/ISO_COBOL.md at the section for the surrounding context (general format,
   argument rules, returned-value rules). cite.py --check the rule.
2. Look at the listed existing goldens. If one already exercises EXACTLY this rule's branch with a spec-derived
   expected value, disposition = existing-golden and test_ref names it (quote the lines that exercise the rule).
   Coverage means the rule's branch, not merely the function being called.
3. Otherwise write a NEW golden: a minimal deterministic COBOL program (unique PROGRAM-ID prefixed L1, e.g.
   L1FDT01) that exercises the rule and DISPLAYs a result, plus its .out with every expected line DERIVED from the
   rule text (show the derivation). Positive: tests/conformance/<edition>/<name>.cob + .out, first line
   "*> ISO §<clause> <rule> — <what is exercised>"; use the 2023 directory unless the rule is edition-limited, and
   write one golden per edition where behaviour differs across the row's editions. Negative (a rule that must be
   REJECTED, or a DNS witness that must DIAGNOSE): tests/conformance/negative/<name>.cob + <name>.err holding the
   expected diagnostic substring; FIRST line "*> reject-at: <year> ...". Several rows may share one program when
   the same output pins them all — say which lines pin which rule.
   Never use CURRENT-DATE-dependent values as literal expectations: pin SHAPE (length, separators, digit classes)
   via DISPLAY of derived facts, or use FORMATTED-* over fixed inputs.
4. If, while deriving, the rule has a branch the named code-location cannot satisfy, or an existing golden's
   expectation contradicts the rule, disposition = defect-suspected with a repro program and the spec-derived
   expected result — do NOT bend the golden toward the implementation, and do not change the verdict yourself.
5. A rule no program can exercise (pure documentation, implementor-defined latitude with no observable): not-closable
   with the reason; do not manufacture a test.
For the dns-witness file: read kb/Work/PB260.md and kb/Work/PB261.md FIRST — they carry the licence and the shapes
to measure; the witness must prove the compiler DIAGNOSES the declined construct (name the diagnostic you expect
from docs/DIAGNOSTICS.md, or say that none exists yet and the witness therefore needs a compiler change — that is a
finding, report it under defects).
Return the manifest entries (per edition directory) and negative entries you need registered, and a
record_verdicts.py record per row that would close: {"rule-id","verdict","code-location","test-ref","editions",
"notes"} — verdict unchanged from the input, test-ref your new/existing ref, editions from the row, notes a
one-line derivation pointer. Strings only.`,
    { label: `write:${slug}`, phase: 'Write', model: 'opus', agentType: 'cobol-adjudicator', schema: WRITER_OUT }),

  (w, slug) => w ? agent(`${COMMON}

You are the INDEPENDENT REFUTER for the golden drafts of file ${slug}. The writer's report is:
${JSON.stringify(w).slice(0, 60000)}
Its draft files are under ${SCRATCH}/out/${slug}/ ; its input rows are ${SCRATCH}/in/in-${slug}.json.
For EVERY row: (a) re-derive each expected output line yourself from the rule text in specs/ISO_COBOL.md — do not
read the writer's derivation first; compare after; (b) run each citation through cite.py --check yourself; (c)
check the program exercises THE RULE'S BRANCH, not just the function (a TRIM call does not pin r3); (d) check the
editions and the edition directory against the row's editions; (e) check the expectation is not copied from an
implementation (no NIST/legacy provenance, no "observed" values); (f) check determinism; (g) for existing-golden
dispositions, open the golden and confirm the quoted lines pin the rule. Every overturn in this project's history
was a DOWNGRADE; an all-upheld report is a red flag — say what you did to break it. Default to refuted when
uncertain. Corrections must be exact (the line, the value, the reason).`,
    { label: `refute:${slug}`, phase: 'Refute', model: 'opus', agentType: 'cobol-refuter', schema: REFUTE_OUT }).then(r => ({ w, r })) : null,

  (x, slug) => {
    if (!x || !x.r) return x
    const bad = x.r.verdicts.filter(v => !v.upheld)
    if (!bad.length) return { ...x, f: { slug, applied: [], refused: [] } }
    return agent(`${COMMON}

You are the FIXER for file ${slug}. Apply these refuter corrections to the draft files under ${SCRATCH}/out/${slug}/
and, where a correction changes a test_ref/record, write the corrected writer report to
${SCRATCH}/out/${slug}/REPORT.json (start from the writer's report below). Refuse a correction only if you can show
from the spec text that the refuter is wrong — then say exactly why, with cite.py --check output.
Writer report: ${JSON.stringify(x.w).slice(0, 40000)}
Corrections: ${JSON.stringify(bad)}`,
      { label: `fix:${slug}`, phase: 'Fix', model: 'opus', agentType: 'cobol-adjudicator', schema: FIX_OUT }).then(f => ({ ...x, f }))
  },
)

const done = results.filter(Boolean)
log(`Lane 1 draft: ${done.length}/${FILES.length} files completed`)
return done.map(x => ({
  slug: x.w.slug,
  rows: x.w.rows.map(r => ({ rule_id: r.rule_id, disposition: r.disposition, test_ref: r.test_ref })),
  manifest_entries: x.w.manifest_entries,
  negative_manifest_entries: x.w.negative_manifest_entries,
  records: x.w.records,
  defects: x.w.defects,
  refuted: x.r ? x.r.verdicts.filter(v => !v.upheld) : 'REFUTER MISSING',
  fixes: x.f,
  summary: x.w.summary,
}))
