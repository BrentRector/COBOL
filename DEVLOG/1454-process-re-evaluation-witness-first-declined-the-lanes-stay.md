## Entry 1454 — 2026-09-04 16:19 PDT — Process re-evaluation: witness-first declined, the lanes stay separate, and the burn-down gets turn caps and landing trains

The owner asked whether there is a more efficient process that finishes the compiler — the PHASE-14 traceability
inventory at zero GAP — more quickly, with efficiency defined as rows closed per day AND per token and the
definition of DONE (`DESIGN-spec-conformance-review.md` §1) explicitly not up for weakening. Three proposals were
written independently — throughput-first, cost-first, risk-first — and reconciled into one synthesis measured on
the tree. Both documents are now frozen verbatim in
`docs/rearchitecture/evidence/PROCESS-REVIEW-2026-09-04.md`; this entry records what was decided on them.

**The measurement, because the plan had been built on the wrong number.** The 138 rows/day headline is not
repeatable: over 19 trend points and 18 GAP-moving landings the mean is 40.0 and the median 14.0, four landings
closed zero, and **305 of the 720 rows (42 %) came from three module-decline witness landings** — screen handling
152, witness B2 104, witness cluster A 49 — with 203 more from the golden lane. The **earned rate is 69 rows/day**,
and there is no fourth module to decline: `CONFORMANCE.md` records A.4.11 Report Writer as Partial with a shipped
implementation. Three further numbers changed the shape of the problem. The evidence is derived **twice** —
adjudication emits a verdict with no evidence and the golden lane later re-reads the same rule and the same code to
produce it — which is why batch b1 adjudicated 184 rules for a GAP delta of −15 (8 %) and b2 adjudicated 292 for
−32 (11 %) while taking the CONFORMS-but-untested band from 9 rows to 55. The **fix lane, not the evidence lane, is
the binding constraint**: two batches opened 169 notes while the fix lane closed ~16 mechanisms in the same six
days, and it ran at **under 15 % utilization** of the three-implementer cap the owner had already authorized —
~2.7 mechanisms/day delivered against ~24/day of capacity. And agent cost is **quadratic in turns**,
`tokens ≈ 0.115·T + 0.00031·T²` fitted over n = 239 agents and reproduced independently to three significant
figures: 18 of those 239 ran ≥ 250 turns and burned **39 % of all 5.62 B tokens**.

**The synthesis's central recommendation was DECLINED.** It proposed *one derivation per rule* — one agent reads
the rule, locates the code, writes a pre-run predicted expectation, authors the witness and runs it, so the verdict
falls out — with a blind replicate replacing both of today's refuters. Put to the owner as a bare question, the
answer was **no: keep the lanes separate.** The reason is independent derivation of the evidence: an agent that has
already read the implementing code has formed its expectation *from that code*, and both offered guards check the
*record* of that one derivation rather than supplying a second one. Two lanes each read the standard for
themselves and produce two derivations, and that second derivation demonstrably earns its cost — every adjudication
batch so far has overturned some CONFORMS verdicts, always downward. The double derivation is therefore the
**price of the guarantee**, not waste; `DESIGN-spec-conformance-review.md` §5.1 now says so in the lane section,
and §1 is untouched. The declined pipeline is recorded as the analysis, not as process.

**Two decisions were adopted.** Every agent is hard-capped — **160 turns read-only, 220 for an implementer** — and
a job that will not fit is SPLIT, never extended, because a fresh agent from a checkpoint starts on the flat part
of the curve. And one lander may carry **five clusters in one landing**, one commit per cluster inside it, never
more than one landing per transcript: a landing is ~90 % fixed cost, so the measured spread is 10.4 M per cluster
at one against 5.1 M at five, and the corpus proves it directly — the golden lander landed 151 rows for 52.9 M
(0.35 M/row) where the PB383 lander landed 2 rows for 7.2 M (3.6 M/row). Three mechanics needed no decision
because they change no definition: **one mechanism per implementer** (the second mechanism in one transcript costs
44.6 M against 40.3 M for a whole fresh implementer — the quadratic eats the amortization immediately), **a free
implementer slot is filled within the turn it frees**, and the comprehensive **battery runs in a worktree cut at
the batch head** because its phase 2 rebuilds and would otherwise freeze the lander for ~45 min ≈ 7 clusters.

**What changed in the skill.** `.claude/skills/workstream/SKILL.md` §2 is rewritten around the cost law as the
REASON for the budget rather than around the anecdote, and carries the cap table, the one-mechanism and
one-landing-per-transcript rules; the ≈8-agent budget line stays, restated as an error-containment default since it
is tokens, not slots, that the weekly limit rations. §3 gains the five-cluster train with its mechanics — one
build, the union of the clusters' gate filters, one commit per cluster so a red bisects, the dropped-cluster rule —
plus "never spend a lander on a 1–2 cluster landing unless nothing else is ready", the turn-it-frees dispatch rule
and the worktree battery. `templates/implementer-brief.md` now opens with ONE MECHANISM and the 220-turn cap with
its SPLIT instruction, and its opening move becomes *rerun the repro at its path* whenever the contract carries
one, with RE-PROBE EVERY MECHANISM kept for contracts that do not. `templates/lander-brief.md` is marked the
single-cluster form and points at the new `templates/lander-train-brief.md`. The three workflow templates —
`wf_lane3_refute.js`, `wf_lane1_draft.js`, `wf_lane1_validate.js` — carry the 160-turn cap in their COMMON prompt
and an OPTIONAL `deferred` array in every stage schema, so an agent that stops at the cap names the rule-ids it did
not decide instead of returning a silently short population; the registrar reads it. All four templates
`node --check` clean.

**Five notes opened, one extended.** `PB468` (decision, owner) holds the three answers verbatim and the **six
questions the owner has not yet been asked**, verbatim as bare questions — the implementer cap 3 → 6, table tests
closing many rows behind a two-way drift test, effort tiers on checked steps, the ~120 already-OK SR/AR "shall"
rows carrying positive-only evidence, whether Report Writer stays claimed, and whether PB386's derivation arm
extends class by class. `PB469` (analysis, landed) carries the measurement, and names its own softest number: the
note yield outside §14 is measured at 0.36/rule on §14 statements and is **unmeasured on §13 (700 rows) and §8
(412)** — at 0.05 the fix lane is 529 mechanisms and ~22 working days, at 0.36 it is 1155 and ~40. Lane-3 batch 3
is §13.18 data-division clauses, so it measures exactly that, and its registrar owes the number as a first-class
result.

**Two tooling findings, both from today's landers rather than from the review.** `PB470` (defect, open):
`LOCALE-DATE`, `LOCALE-TIME` and `LOCALE-TIME-FROM-SECONDS` return a zero-length substitute on an argument-rule
violation at four sites in `CobolLocale.cs` (lines 49, 65, 71, 83), but §15.52.4 r3 makes their returned length
depend on the LOCALE — so by the words of `docs/CONFORMANCE.md` row `DOC-A.1-90`'s own determination the
zero-length class does not reach them and the general clause does, which says *spaces*. The framing this was
dispatched with is corrected in the note: A.1 item 90 does **not** require the value to be documented — the
standard says it "does not have to be documented in the implementor's user documentation" — the §7 determination
was written voluntarily, which is what makes the code's unwritten determination a defect against the project's own
rule rather than against a conformance obligation. Worse, the one green golden on that path
(`pb64t4_locale_functions`, the `ARG=[]` leg) MOVEs the result and then TRIMs it, and §14.6.8.5 space-fills a
receiver from a zero-length sender — so it passes under either answer and has never been able to contradict the
code. `PB471` (analysis, open) is the same wording problem from the other side: row 90's zero-length clause says
"where the returned LENGTH is itself derived from the rejected argument" while listing the FORMATTED-\* family
wholesale, yet their length derives from argument-1 (§15.39.4 r1), which is valid when argument-2 is the rejected
one. `PB472` (defect, open, process): `build_inventory.py` builds the canonical row skeleton with every field on
every row while `record_verdicts.py` assigns ADJUDICATED fields onto rows loaded from JSON, so a newly added field
— `derivation` was the last one — lands only on the rows a batch touches and lands there after `state`;
`write_inventory` is one careful atomic FILE writer but serializes whatever dict shape it is handed, so there is
one file writer and no row serializer, and no drift test asserts a row's key sequence. Demonstrated mechanically
rather than inherited; the fix is one shared serializer plus a two-language drift test, and it is a fix-lane item
with its own implementer.

`PB175` is extended with the second finding, which had been costing every lander a re-derivation.
`scripts/semgrep/verify.py` compares against `scripts/semgrep/baseline.json`, and that file still held the
**pre-drift** counts (`cobolnet-no-biginteger` 4, `cobolnet-raw-diagnostic-code-literal` 423) while the note itself
had registered 36 / 439 as the real comparison point two days earlier. So the check printed `UP` and exited 1 on
every clean landing, and each lander had to prove from scratch that its own diff contributed none of it — PB383's
lander did exactly that today. Regenerated to the counts measured on this tree (36 / 439, plus the `no-decimal`
DOWN from 3 to 2 locked in); `verify.py` now exits 0 with all four `OK`, so an `UP` means a real increase again.
⛔ Bookkeeping, not absolution: neither half of PB175 — adjudicate the sites, give the check a battery seat — is
closed by it.

Gate: `work.py check` 528 items well-formed; both citation audits at zero; `semgrep/verify.py` PASS;
`node --check` green on all four workflow templates. No `dotnet` build or test was run and none was needed — this
change set touches no file under `src/` or `tests/`.

