## Entry 1130 - 2026-07-30 09:40 PDT - Making §0 resumable: it had become a chronology, and it pointed at paths a new session cannot see

**Owner asked that a cleared session pick up exactly here.** Auditing §0 against reality found two failure
classes, and the second is the one that would actually have broken a cold start.

**① §0 HAD TURNED BACK INTO A CHRONOLOGY.** "Where we are" had grown to ~170 lines of dated bullets, each
recording a wave as it landed, each carrying the GAP number *at that moment* — 3790 → 3781 → 3774 → 3773 → 3766 →
3758 → 3814. Every one of those is now wrong, and a reader has no way to tell which is current. **§0 states WHERE
WE ARE, never how we got here**; the narrative is what `DEVLOG.md` is for and it was already there in
1113–1129. Replaced with a current-state block: the shape of the review, the PB1–PB8 status, the three lessons
that keep recurring, the instruments that have lied, and the outstanding owner decision. It says outright that
**no GAP number should be read from the document** — the probe computes it. Same discipline the merge line got
yesterday, applied to the section as a whole.

Also purged: a "**THE FIX QUEUE IS EMPTY**" heading sitting directly above a paragraph explaining that the queue
is now the source of work; a battery reference pinned at 4137/953 from the previous wave; and the catalog
denominator still quoted as 3,790 in NEXT item 5. The stale-number sweep is now clean.

**⛔ ② THE POINTERS WENT TO A SESSION-SCOPED DIRECTORY — the defect that mattered.** §0 told the next session
that the Phase-B batch generator was "four lines of Python" and that the PROMPT lived in the batch-3 workflow
script "under the session scratchpad". Those files are real, and they are under
`…/projects/E--CobolSharp/<session-guid>/workflows/scripts/`. **A cleared session gets a different GUID.** The
instructions pointed at nothing, for the single most important recurring task in the project.

Fixed by moving both assets into the repo:
· `scripts/spec/phase_b_batch.py` — one self-contained input file per SUBJECT plus the slug list, EXCLUDING rules
  that already carry a verdict so a re-run after a partial batch is safe. The generator had been retyped inline
  for three batches, which is why its exclusion rule lived only in a transcript.
· `DESIGN-spec-conformance-review.md` **§9**, the batch playbook: the fan-out shape, the **nine things a batch
  prompt must carry** — each one paid for by a defect — and how to read the results without the two mistakes
  already made (reading output files before the completion notification; ignoring the `<failures>` block when a
  workflow reports "completed" with dead agents).

§9 also records the evidence for the refute stage, now three batches deep: every batch overturned verdicts and
**every overturn was a downgrade**, so an all-CONFORMS report is a red flag rather than a good result. It found
PB5 (the adjudicator had sampled only small receivers, where the clamp cannot bite) and PB7 (invisible to
output-sampling, because the program compiles and fails at run time).

**WHAT A COLD START NOW DOES**, verified by running it: read `CLAUDE.md` → §0 → `pwsh scripts/session-probe.ps1`
(3,846 rows · 3,800 GAP · 181 adjudicated · 7 CONFORMS-still-test-needed) → NEXT item 1, which names **PB8** as
the single open item, gives both repros, states the root cause already located in `CobolLexer.g4`'s
`OnDefaultLParen`, and warns that it is a lexer-mode change not to be started at the end of a session.

⚖ **The one outstanding OWNER DECISION is stated where it cannot be missed**: the catalog does not model
§13.18.40.5 "Editing rules", §13.18.40.6 "Precedence rules" or §5.3 "Rules". They are rule KINDS, not spelling
variants, and admitting them changes what the denominator MEANS.

**GATES.** Docs + one new script; no compiler change. `SpecTraceabilityInventoryDriftTests` 10/10,
`TestRepoDriftTests` 2/2, `audit_doc_citations.py` 0 misfiled across docs and source.

