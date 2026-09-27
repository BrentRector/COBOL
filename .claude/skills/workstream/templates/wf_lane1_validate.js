export const meta = {
  name: 'lane1-golden-validate',
  description: 'Golden lane stage 2: one Opus validator per draft file compiles and runs every drafted golden with the built cobol.exe at its edition and compares to the SPEC-DERIVED .out; a mismatch is a suspected compiler defect with a repro, never a golden edit',
  phases: [
    { title: 'Validate', detail: 'compile-and-run each draft at its --std; negatives must reject at every reject-at edition with the .err substring' },
  ],
}

const SCRATCH = '{SCRATCH}/lane1'
const FILES = args.files
if (!Array.isArray(FILES) || !FILES.length) throw new Error('pass args.files = [slug, ...]')

const OUT = {
  type: 'object',
  properties: {
    slug: { type: 'string' },
    results: { type: 'array', items: { type: 'object', properties: {
      file: { type: 'string', description: 'repo-relative destination path of the .cob' },
      kind: { type: 'string', enum: ['positive', 'negative'] },
      editions_run: { type: 'array', items: { type: 'string' } },
      status: { type: 'string', enum: ['pass', 'output-mismatch', 'compile-error', 'runtime-error', 'negative-not-rejected', 'negative-wrong-diagnostic', 'not-run'] },
      observed: { type: 'string', description: 'the compiler/program output, trimmed to what matters' },
      expected: { type: 'string' },
      classification: { type: 'string', enum: ['pass', 'suspected-compiler-defect', 'draft-error', 'environment'], description: 'a mismatch against a refuter-upheld spec-derived expectation is a suspected COMPILER defect unless you can show the draft misread the rule' },
      repro_note: { type: 'string', description: 'for a suspected defect: the minimal program, the rule text, expected vs observed — ready to become a kb/Work note' },
      rule_ids: { type: 'array', items: { type: 'string' } },
    }, required: ['file', 'kind', 'editions_run', 'status', 'observed', 'expected', 'classification', 'repro_note', 'rule_ids'] } },
    summary: { type: 'string' },
    // OPTIONAL. Draft files (or rule-ids) you did not validate because you reached the 160-turn cap — the NAMES,
    // never a count, so the registrar can dispatch exactly them and reconcile read == decided + |deferred|.
    deferred: { type: 'array', items: { type: 'string' } },
  },
  required: ['slug', 'results', 'summary'],
}

phase('Validate')
const N = args.concurrency || 4
const res = []
for (let i = 0; i < FILES.length; i += N) {
  const chunk = FILES.slice(i, i + N)
  const rs = await parallel(chunk.map(slug => () => agent(`
You validate golden DRAFTS for the COBOL.NET compiler (repo E:\\COBOL; read CLAUDE.md first). The drafts and
the writer/refuter report for file "${slug}" are under ${SCRATCH}/out/${slug}/ (destination paths mirror the repo:
tests/conformance/<edition>/<name>.cob|.out, tests/conformance/negative/<name>.cob|.err; REPORT.json if a fixer
rewrote the report, else read the writer report in ${SCRATCH}/reports/${slug}.json).
RULES. Do NOT write anything inside E:\\COBOL; do NOT run dotnet build/test; do NOT edit any draft .out/.err —
the expected values are SPEC-DERIVED and refuter-upheld; if the compiler disagrees, the compiler is the suspect.
Compiler: E:\\COBOL\\.claude\\worktrees\\lane3-pin-5fe593a0\\src\\Cobol.Net.Cli\\bin\\Debug\\net10.0\\cobol.exe (a PINNED build of battery-green tree 5fe593a0 — landings cannot move it under you; never use the main tree's exe).
FOR EACH POSITIVE DRAFT: copy the .cob to a fresh scratch dir under ${SCRATCH}/run/${slug}/<name>/ and run
  cobol.exe <name>.cob --std <edition-from-its-directory> --run
capturing stdout+stderr. Compare stdout to the draft .out: trailing whitespace per line and the final newline are
ignored, CRLF == LF (the corpus runner's rule). Anything else is output-mismatch.
FOR EACH NEGATIVE DRAFT: read its first line "*> reject-at: <years>"; for EACH listed edition run
  cobol.exe <name>.cob --std <year>
and require a non-zero exit AND the .err substring in the diagnostics; also run it at one edition NOT listed (if
any exists in {85,2002,2014,2023}) and record whether it compiles clean there (report only; it is evidence about the
edition band, not a pass/fail criterion unless the rule is edition-limited).
⛔ CHECKPOINT PER DRAFT: your checkpoint file is ${SCRATCH}/validate/${slug}.jsonl — FIRST read it if it exists and skip
every draft file already in it; APPEND one result object (the schema's results item) the moment each draft is judged
(python open(path,'a')); your final return is the union. A session-limit kill can come at any moment.
⛔ HARD CAP: 160 TURNS (read-only work; owner decision 2026-09-04). Agent cost is QUADRATIC in turns
(tokens ~ 0.115*T + 0.00031*T^2, n=239), so the work is SPLIT, never extended. As you approach the cap: finish the
draft in hand, append it, then RETURN. Drafts you did not validate go in the optional "deferred" array of your
return, BY NAME — never a count, and never silence; the merge/registrar step reads it and dispatches a fresh agent
for exactly those. Stopping at the cap with an honest deferred list is the CORRECT outcome, never a failure.
CLASSIFY every mismatch: re-read the rule text in specs/ISO_COBOL.md yourself; if the draft's expectation follows
from the rule, classification = suspected-compiler-defect with a repro_note (minimal program, rule text quoted,
cite.py --check line, expected vs observed); if you can show the draft misread the rule, classification =
draft-error and say exactly what the rule requires (you still do not edit the draft). Environment problems (missing
exe, timeouts) are 'environment'. Return every draft, including passes.`,
  { label: `validate:${slug}`, phase: 'Validate', model: 'opus', agentType: 'cobol-adjudicator', schema: OUT })))
  rs.forEach(r => { if (r) res.push(r) })
  log(`Validate: ${Math.min(i + N, FILES.length)}/${FILES.length}`)
}
return res
