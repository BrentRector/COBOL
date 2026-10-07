# Cloud adjudication brief (one batch of rows, one cloud session or one subagent)

The in-cloud counterpart of `wf_lane3_adjudicate.js` + `wf_lane3_refute.js`, for claude.ai/code cloud sessions
(kb/Work PB1522). Proven on 90 Annex A.1 rows on 2026-09-24 (calibration #1 + wave 1): ~11–19 min and ~$0.34 per
row. A batch is ~20 rows. Fill in `{BATCH}` (a short tag, e.g. `adj-w2-a1-s1`) and `{SLUGS}` (the input-file slugs)
before use.

## Setup

Work in `/home/user/CobolSharp`. Read `CLAUDE.md` first; its eight rules bind you. The spec text
(`specs/ISO_COBOL.md`) and `cite.py` are in this repo. The `specs-private` submodule holds only the licensed PDF,
needed to RENDER a page when a general-format diagram is load-bearing (`scripts/render-spec-page.py`); it is checked
out only when BrentRector/COBOL-private is attached (web-page sessions; `claude --cloud` attaches one repo).
Without it, a verdict that hinges on a diagram is PARTIAL with "diagram not rendered" in notes. Record `git rev-parse HEAD` as the PINNED sha. Build once: `dotnet build Cobol.Net.sln -c Debug` (probe
compiler: `src/Cobol.Net.Cli/bin/Debug/net10.0/cobol`, usage `cobol prog.cob --run [--std 85|2002|2014|2023]`; write
probes under `/tmp/adj/{BATCH}/probe/`). Inputs: `python scripts/spec/phase_b_batch.py <clauses> --max-rules 10
--out /tmp/adj/{BATCH}/in` and use ONLY the files `in-<slug>.json` for:

{SLUGS}

## What each row kind means

- **DOC-A.1-N (Annex A.1 implementor-defined item).** The item's own sentence in `specs/ISO_COBOL.md` says it "shall
  be documented" or "need not be documented". This project's documentation is `docs/CONFORMANCE.md` (a
  `| DOC-A.1-N |` determination row). CONFORMS only when (a) a determination for exactly this item exists — unless it
  need not be documented — AND (b) the compiler/runtime behaves as stated or required (read the code; probe when
  reading cannot decide). A behavior the code fixes but nothing documents under its own key ⇒ DIVERGES (harm
  silent). Facility absent ⇒ NOT-IMPLEMENTED. Determination present but the code differs, or an edge unverified ⇒
  DIVERGES / PARTIAL. An item that cannot arise because its module is declined (screen handling A.4.2, commit A.4.3)
  ⇒ NEEDS-OWNER-DECISION, citing kb/Work A11.
- **GR / SR / FMT rows (clause rules).** Read the rule and its general format (render the PDF page with
  `scripts/render-spec-page.py <page>` when a diagram is load-bearing); locate the implementing code; verify across the
  editions the SPEC gives (Annex E / the clause text, never the code); probe edge cases with the built compiler.

## Rules (the production lane-3 bar)

- THE SPEC IS THE ONLY ORACLE. NIST and GnuCOBOL are regression nets, never authority.
- VALIDATE EVERY CITATION: `python scripts/spec/cite.py --check <clause> "<quoted text>"`. A real clause that
  answers a DIFFERENT question is the failure mode to fear most.
- CONFORMS COSTS SOMETHING: code not located ⇒ NOT-IMPLEMENTED; an edge unverified ⇒ PARTIAL; a two-part rule with
  one part verified ⇒ PARTIAL.
- DOCUMENTED-NON-SUPPORT is never yours to choose; use NEEDS-OWNER-DECISION where `docs/CONFORMANCE.md` §5 declares
  the facility Not claimed and no derived selector covers it.
- ONLY A SPEC-DERIVED TEST CLOSES A ROW: `conformance:<edition>/<case>`, `unit:<Class>.<Method>`,
  `conformance-test:<Class>.<Method>`; never `nist:` / `characterization:` / `*_MatchesLegacy`. An empty test-ref
  (test-needed) is a legitimate outcome — do not write goldens, do not edit any repo file outside your batch dir.
- The dossier in each input file is a MAP, not the territory; its silence is not evidence. For A.1 its
  determinations/register lists are generic — FIND OWNING NOTES YOURSELF:
  `grep -l "<rule-id>\b\|item <N>\b" kb/Work/*.md` plus the clause keywords. An open/half note that owns a row is
  cited in notes, never re-reported; a landed fix is not re-reported.

## Checkpoint per row (mandatory)

The moment a row is decided, APPEND one line to `/tmp/adj/{BATCH}/out/adjudicate.jsonl` (python `open(p,'a')`, one
line, flush) — never batch at the end — in the `record_verdicts.py` record shape:
`{"rule-id","verdict" (CONFORMS|PARTIAL|DIVERGES|NOT-IMPLEMENTED|NEEDS-OWNER-DECISION),"code-location" (path#Symbol;
…),"test-ref","editions","notes"}` — notes carry the rule text quoted, the CONFORMANCE.md line or its absence, what
the code does, the owning kb/Work note, and the `cite.py --check` line. Every 5 rows copy `out/*` into
`adjudication/{BATCH}/`, commit ONLY that directory on branch `claude/{BATCH}`, push
(`git push -u origin claude/{BATCH}`). Cluster defects by MECHANISM into `out/findings.json`:
`[{mechanism, rule_ids, harm (wrong-answer|crashes|rejects-legal-source|under-rejects|silent), repro,
expected_from_spec, observed_or_reasoning, code_site, clause, owning_note}]`.

## Refute

When every row is decided, ONE independent refuter (a fresh subagent) attacks ONLY the CONFORMS rows: re-derive each
from the rule text and the code, probe, re-run the citation, check the test-ref really pins this branch, default to
refuted when uncertain, name the corrected verdict. Every batch so far overturned some (wave 1: 3 of 7, always
downward). Write `out/refute.jsonl`; apply overturns into `out/final.jsonl` (the record shape above).

## Deliver

`adjudication/{BATCH}/` holds `README.md` (pinned sha, wall-clock per phase, verdict counts, overturns),
`final.jsonl`, `adjudicate.jsonl`, `refute.jsonl`, `findings.json`; committed on `claude/{BATCH}` and pushed.
NEVER commit to or push `main`; never touch another file. Final report ≤ 30 lines: verdict counts, per-row
one-liners, findings by mechanism with owning notes, overturns, branch/commit, wall-clock.
