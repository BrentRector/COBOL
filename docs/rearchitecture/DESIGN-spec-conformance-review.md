# DESIGN — Full Implementation ↔ Specification Conformance Review (P14 Step-0, spec-first)

> **STATUS: DESIGN — decision-complete methodology + plan.** This is the exhaustive, spec-anchored conformance review
> that is the PHASE-14 definition of DONE (owner decision D13: commercial-quality, 100% CONFORMING to ISO/IEC
> 1989:2023 per §4.2.1 with correct support for 1985/2002/2014). It re-grounds the P14 Step-0 "traceability
> inventory" as **spec-first and exhaustive** — the spec text is the ORACLE, not a differential. Spec-first is the
> only going priority (memory `feedback_spec_is_the_oracle`).

## 0. Why this exists — the lesson that forced it

The historical test strategy is entirely **differential + happy-path**: NIST-85 goldens, the GnuCOBOL compile
accept/reject differential, and our own conformance corpus whose "expected" outputs were largely derived from the
legacy byte-engine. **None of these is a spec oracle**, and a differential is structurally BLIND to a spec-violation
that both implementations share — and many defects were *ported from* the legacy, so the differential and the bug came
from the same place. Two spec-first audits (design-doc `wf_480d50f5`, code `wf_4ce42db6`) found **~44 candidate
conformance bugs** those checks never could. But those audits were **bounded SAMPLING** — 14 behavioral areas, one
agent each, a handful of rules apiece — not exhaustive coverage. 100% conformance (D13) requires an EXHAUSTIVE,
traceable review: every normative rule → its implementation → a verified verdict → a spec-derived test.

## 1. Definition of DONE

Every normative ISO/IEC 1989:2023 rule — and its 1985/2002/2014 applicability — has:
- **(a)** a located implementation (a traceability link into `src/Cobol.Net.*`), OR a recorded **owner
  documented-non-support decision** (optional modules only, per §4.2.16);
- **(b)** a conformance **verdict VERIFIED against the spec TEXT** (not against the legacy or GnuCOBOL);
- **(c)** a **spec-derived test** that covers it — the expected value COMPUTED FROM THE SPEC, never copied from an
  implementation oracle; **OR, only for a rule that carries no observable obligation, the owner-signed
  DERIVATION of §1.1 below.**

The traceability inventory at **zero unresolved GAP** = P14 DONE (D13). This supersedes differential-based confidence.

### 1.1 The DERIVATION — the (c) alternative for a rule with no observable obligation (owner, `kb/Work/PB386`, 2026-09-03)

⛔ **This is the ONE widening of §1 an agent did not make, and the bounds are the owner's, not a paraphrase.**
Some normative rules impose no obligation a conforming program can observe: the standard declares the result
*undefined* (§4.4 2): *"Situations in which the results of executing a statement are explicitly undefined or
unpredictable are identified in A.2, Undefined language element list. A COBOL run unit that allows these
situations to happen is a conforming run unit"* — `cite.py --check 4.4` OK); or the rule's antecedent cannot be
populated in this implementation; or its consequent cannot be told apart from another rule's. For such a rule
§1(c) is not merely unmet, it is **unmeetable**: any golden written for it would pin an IMPLEMENTATION CHOICE as
though it were the standard's answer, which is exactly what the schema's `spec-derived` clause exists to prevent.
The measured population before the door opened was **eight rows** — every CONFORMS-but-untested row on the tree
except `DOC-A.1-93`, which is a real defect (`kb/Work/PB383`).

A **derivation** is a determination recorded in `docs/CONFORMANCE.md` §8, keyed by the inventory `rule-id`,
naming ONE of four arms and signed by the owner. It stands in place of §1(c) **and of nothing else**: (a) and
(b) are paid in full, and a `kind: DOC` row still pays its register anchor and its observability cost.

| arm | what it claims | how it is CHECKED |
|---|---|---|
| `undefined-A.2` | the standard itself lists this rule as explicitly undefined | **MECHANICAL.** The `Names` cell reads `A.2 item <n>`; the checker resolves Annex A.2 item *n*'s own trailing citation (`(14.9.5, CANCEL statement, General rule 11)`) against the rule catalog and requires **this row's `rule-id` to be among the ids it resolves to**. Naming an item that does not cover the row is refused. |
| `unpopulatable-antecedent` | the antecedent is false for every state this implementation can reach | **REVIEWED ARGUMENT, shape-checked.** The `Names` cell must STATE the closed set that makes it unreachable (≥20 characters — a dash or an `n/a` is refused). The argument itself has a reader, and the owner's signature is that reader. |
| `indistinguishable-consequent` | the consequent is byte-identical to another rule's, so no conforming program can distinguish them | **REVIEWED ARGUMENT, shape-checked.** The `Names` cell must be a `rule-id` that EXISTS in the catalog and is not this row's own. |
| `definitional` | the rule DEFINES a property of the user's program (§4.4's conforming run unit) and obliges the implementation to nothing — owner decision `kb/Work/R43` item 4, extending `PB386` | **REVIEWED ARGUMENT, shape-checked.** The `Names` cell must open `defines` and state the property and whose it is (≥20 characters after the word). A rule whose subject is the implementation or the implementor is never this arm. |

**What is REFUSED, by the writer and by the gate.** Each of these is a bound the owner set, encoded rather than
described:

1. a derivation on a row that already carries a **spec-derived `test-ref`** — the row demonstrably HAS an
   observable obligation, so the premise of the whole mechanism is false for it;
2. a derivation on a row whose **verdict does not resolve** — a derivation explains why no test can exist, not
   why a DIVERGES is acceptable;
3. a derivation on a row of a kind that **obliges a register determination which `docs/CONFORMANCE.md` does not
   carry** — the documentation obligation is not yet stated, so there is nothing for the derivation to be about;
4. an `undefined-A.2` arm naming an item that **does not resolve to this row**;
5. a **missing or unknown owner signature**: the `Signed` cell must be a key of the schema's `signatures` map, one
   key per dated owner decision that signed rows (`owner: 2026-09-03`, PB386 and the rows signed under it through
   2026-09-25; `owner: 2026-10-08`, the PB1636 batch). A new batch signing adds its key in the change that writes
   its rows, so the cell records WHEN its argument was read;
6. a `derivation` field that is not the anchor **COMPUTED** from the row's own rule-id, or for which
   `docs/CONFORMANCE.md` §8 carries **no row**.

⚖ **How this coexists with `PB280` Q2 ("no" for anchor-only DOC rows), which it does NOT reverse.** A DOC row
asks the implementor to STATE a choice, so "there is nothing to observe" is unfalsifiable there and cannot close
a row — that answer stands. `DOC-A.1-19` is admitted for the opposite reason: its determination **is** stated,
in §7, and it names a greenfield site; only its WITNESS is impossible, because §14.9.5.4 GR10's branch is
reachable only through the GR2 locate step this runtime never performs, so the effect is byte-identical to
GR7's mandated no-op. **The distinction is encoded by ORDER, not by a special case**: `state_for` charges the
kind's anchor and observability costs FIRST and consults the derivation only where a spec-derived `test-ref`
would have been consulted. A DOC row whose only location is its §7 anchor therefore still computes `GAP`, with
a derivation or without one — refusal 3 above adds the second half, that a DOC row §7 does not document cannot
carry a derivation at all.

## 2. The unit of review — the normative rule

The spec expresses requirements as: **Syntax Rules (SR) + General Rules (GR)** per statement (§14.9.x) and clause
(§11/§12/§13.x); **general formats** (the grammar); **§8 concepts** (classes/categories §8.5, conditions §8.8.4,
reference/subscript/ref-mod §8.4, standard conversions §8.5.1, arithmetic/boolean expressions §8.8); **§15 intrinsic**
definitions; **Annex A** required-documented-behavior; **Annex E** edition deltas; **Annex F** obsolete/archaic. Each
rule = `(rule-id, §-anchor, subject, kind, edition-applicability, text)`. The complete rule set is the coverage
**DENOMINATOR** — the thing the audits never enumerated.

## 3. The three phases

### Phase A — build the spec-rule catalog (the denominator)
Extract EVERY normative rule from `specs/ISO_COBOL.md` into a structured, machine-checkable catalog. The spec's SR/GR
are numbered under "Syntax rules" / "General rules" headings per section, so extraction is tractable (parse by
§-section). **Output:** `docs/rearchitecture/spec-rule-catalog.json` — one entry per rule:
`{ id, section, subject, kind: SR|GR|format|concept|intrinsic|doc-behavior, editions, text }`. A **completeness
critic** cross-checks the catalog against the spec's TOC + section numbering, because an OMITTED rule is silent false
confidence. Orchestrated: fan out over the §-clauses; loop-until-dry.

**A general-format (FMT) row is a format the clause PRINTS, not a heading that says "General format(s)"** (kb/Work
PB1536): a candidate heading yields a row only when its body carries a diagram or the "Format N" labels §5.2.1 gives a
multi-format construct. A candidate with neither is a PARENT clause about formats (§5.2 itself — skipped and printed)
or a leaf whose diagram the transcription lost (a loud `--check` failure: restore the `<pre>`/`<u>` block from the
rendered page). `CatalogCoverageDriftTests.EveryFmtRow_CarriesADiagramOrAFormatLabel` holds the bar over the written
catalog.

### Phase B — map + verify (the numerator)
For each rule (batched by statement/clause), an agent: **(1)** reads the rule + general format; **(2)** LOCATES the
implementing code (binder/emitter/runtime) — the traceability link; **(3)** VERIFIES conformance across the applicable
editions and adversarial edge cases; **(4)** assigns a **verdict** — `CONFORMS | DIVERGES | PARTIAL | NOT-IMPLEMENTED
| DOCUMENTED-NON-SUPPORT | NEEDS-OWNER-DECISION`; **(5)** records the covering spec-derived test, or flags
`test-needed`. Results accumulate into the **traceability inventory** (§4), incremental and RESUMABLE (a rule's
verdict persists across sessions). Orchestration: fan out by clause; completeness critics surface un-verdicted rules;
candidate verdicts are agent-surfaced ⇒ **re-verified** before they are trusted.

### Phase C — close the gaps
- **DIVERGES** → the **§24 fix-queue** (`PHASE-13-plan-vs-spec-review.md`): spec-first fix + spec-derived golden.
  (V54–V59 + `CODE-SPEC-AUDIT.md` CA1–CA38 are the FIRST INSTALLMENT of this queue.)
- **NOT-IMPLEMENTED** → implement to spec, OR an **owner** documented-non-support decision (optional modules only).
- **CONFORMS-but-untested** → write the spec-derived golden (so coverage is DURABLE, not merely asserted).
- Inventory at **zero GAP** = DONE.

## 4. The traceability inventory (SSOT) — schema

One row per rule, at **`tests/version-matrix/traceability-inventory.json`**, generated by
`scripts/spec/build_inventory.py` from the Phase-A catalog and adjudicated only through
`scripts/spec/record_verdicts.py` (§8). It sits beside `constructs.json` and the rest of the machine-read test data
rather than under `docs/`, because its reader is the battery, not a person.

| field | meaning |
|---|---|
| `rule-id` / `section` / `subject` / `kind` / `ordinal` / `page` | from the Phase-A catalog — REGENERATED on every rebuild, never hand-edited |
| `editions` | 85 / 2002 / 2014 / 2023 applicability (introduced / removed / changed) |
| `code-location` | the traceability link: `path` or `path#Symbol` (see the correction below), or `""` if not located |
| `verdict` | one of the six defined in `inventory-schema.json` — the vocabulary lives THERE, not here, so it cannot drift between this doc and the tools |
| `test-ref` | the spec-derived test covering it, in a form that RESOLVES on disk (§8), or `""` |
| `derivation` | §1.1's owner-signed alternative to `test-ref`, for a rule with no observable obligation: the register anchor `docs/CONFORMANCE.md#DRV-<rule-id>`, **COMPUTED from the row's own rule-id**, or `""` |
| `notes` | the fix-queue item id for a DIVERGES; the owner decision for a non-support |
| `state` | **derived, never written** — `OK` only when the verdict resolves AND every field that verdict requires is filled; otherwise `GAP` |

`scripts/session-probe.ps1` reports the live GAP count — that is the **burn-down metric** for P14.

> **⛔ DESIGN CORRECTION, superseding the `file:line` this table used to specify.** `code-location` is a **path,
> optionally plus a `#Symbol`** — never a line number. A line number rots on the next edit to the file, so 3,790
> rows of them would hold the battery gate permanently red for reasons having nothing to do with conformance, and
> a gate that cries wolf is a gate nobody reads. A symbol survives refactoring, moves with the code it names, and
> fails only when the thing it points at actually stops existing — which is the one event worth failing on.

> **⚙ THE `code-location` OF A `kind: DOC` ROW (Annex A.1), and why it has a second form.** A DOC row is not a
> behavioural rule: it is an obligation to SPECIFY and to DOCUMENT an implementor-defined element (§4.2.5), and a
> binder symbol alone cannot discharge that — a DETERMINATION in `docs/CONFORMANCE.md` §7 can. So a DOC row's
> `code-location` is **its §7 register anchor, plus every greenfield implementing site**:
> `docs/CONFORMANCE.md#DOC-A.1-<n>; src/Cobol.Net.…#Symbol[; …]`.
>
> **The anchor is COMPUTED from the row's own rule-id, never chosen** —
> `inventory-schema.json` → `kinds.DOC.anchor-template` expands `{rule-id}`, which is why §7's item key is the
> rule-id `DOC-A.1-<n>` and not the bare item number. Two things follow. A determination filed under a
> neighbouring item cannot be *spelled*, which is the structural answer to `kb/Work/A11` (the §15.3.3.2
> determination sat under item 87, whose obligation is FORMATTED-CURRENT-DATE's accuracy). And a bare number
> could never have been an anchor at all: the gate resolves a `#Symbol` by a word search over the file, so `#7` —
> which five live rows carried, all `state: OK` — matched the digit 7 anywhere in a 790-line document.
> `code-location.anchored-files` now holds each such file to its real anchor spaces (`DOC-A.1-<n>` for §7,
> `4.2.16` for §1, `A.4.<n>` for §5) and forbids citing one bare.
>
> **A DOC row closes exactly as every other row does**: its own anchor, at least one site matching
> `kinds.DOC.implementation-pattern` (`^src/Cobol\.Net\.` — the same greenfield predicate `audit_annex_a1.py`'s
> source sweep applies), and a spec-derived `test-ref`. ⛔ There is **no policy arm**: a row whose only location
> is its §7 anchor stays a GAP. **The owner answered that question `no` on 2026-09-02 (`kb/Work/PB280` Q2)** —
> §1(a) is not widened — on the ground that the claim such an arm would rest on is unfalsifiable: greenfield
> source cites just **13 of the 222** items in `implementor item N` comments, so for the other 209 "there is
> nothing in the compiler to observe" would be contradicted by nothing *and confirmed by nothing*
> (`feedback_verdict_evidence_invariant`). An item with genuinely nothing to observe is discharged by an
> explicit owner decline per item, or stays open. The §7 row's machine-readable **`Pinned by`** cell names the
> same spec-derived test(s), and `audit_annex_a1.py` holds the two in agreement — the determination and its
> evidence are one artifact, which is the half the computed anchor does not cover.
>
> ⚠ **The anchor is owed by the rows with a determination to point at — and the REGISTER decides which those
> are, not the verdict.** CONFORMS, PARTIAL and DIVERGES each assert something about what §7 says, so each owes
> its own row unconditionally. `DOCUMENTED-NON-SUPPORT` has **two grounds on a DOC row**, and they differ exactly
> here:
>
> | ground | §7 row | anchor | how it closes |
> |---|---|---|---|
> | the conditioning facility is not implemented, so A.1's preamble **withdraws** the item ("the item is not required if the optional or processor-dependent feature is not implemented") | must **not** exist | not owed | its module's **witness** alone (items 84, 85, 173, 86) |
> | the element is **optional and not provided** — `kb/Work/PB280` Q1, owner, 2026-09-02 | **exists**: stating the non-provision *is* the determination | owed, like CONFORMS | anchor + a site + a witness (items 127, 206) |
>
> `kinds.DOC.anchor-exempt-verdicts` therefore names the verdicts that **may** be exempt, and one predicate per
> language (`Schema.anchor_obliged` / `AnchorObliged`) asks the register, over one parser per language
> (`inventory_schema.section7_rows` / `tests/_shared/ConformanceRegister.cs`). `audit_annex_a1.py` applies the
> same test — the DNS selectors' rows *minus the ones §7 documents* (`unreachable_items`, emitted as `--json`'s
> `unreachable`) — so the writer, the drift gate, the register gate and the REMAINING counter answer alike.
> ⛔ The first half was found only by landing the A.1 lane and the derived-verdict lane onto one tree
> (2026-09-02): the A.1 lane wrote the anchor rule where every DOC row was a determination, the derived lane then
> stamped four DOC rows `DOCUMENTED-NON-SUPPORT`, and the writer refused the whole batch. ⛔ The second half was
> wrong from the moment PB280 Q1 was answered, and silently: keyed on the verdict, items 127 and 206 would have
> been excused their own determination, `check_pins` would have stopped holding their witness in agreement with
> §7's `Pinned by` at the moment the witness landed, and the audit would have printed "⊘ CANNOT ARISE" about an
> element this register documents in full — dropping REMAINING by two for no work done.

## 5. Execution model — two concurrent lanes, five row-shape lanes (owner decision PB278, 2026-09-01)

Workflow-orchestrated (owner: max parallelism for disjoint work). **Fan out by §-clause** (disjoint units).
**Incremental accumulation** into the inventory — resumable across sessions; the GAP count is the burn-down. **Loop-
until-dry + completeness critics** so nothing is silently uncovered. Every Phase-C fix lands with its **spec-derived
golden** and a comprehensive gate (tiered testing, `feedback_tiered_gates`). This is many sessions of work; it is
paced by the inventory GAP, not big-banged.

**⚖ Adjudication and fixing are TWO LANES THAT RUN CONCURRENTLY** (`kb/Work/PB278`, replacing the 2026-08-09
"backlog to zero before adjudication" order). The measurement behind it: of 3,636 GAP rows on 2026-09-01, 3,361 had
never been adjudicated while thirteen per-wave review fleets (~1,000 agents, ~4B cache-read tokens in three days)
had moved the GAP by 27 rows. The two lanes never write the same thing: the adjudication lane reads a `git
worktree` pinned at a commit and writes verdict batch files; the fix lane edits the main tree.

### 5.1 The lanes, ordered by rows closed per token

| lane | rows (2026-09-01) | unit of work | what closes a row |
|---|---|---|---|
| **Golden** | CONFORMS rows with an empty `test-ref` (139) + DOCUMENTED-NON-SUPPORT rows without a witness (18) | one writer agent per subject family (~10–14 rows), one refuter per file checking ONLY that each expected value is derivable from the cited rule text | a spec-derived golden (or witness) registered in the manifest; no compiler change |
| **Derived verdict** | rules made unreachable by an A.4 module the owner has declared *Not claimed* (`CONFORMANCE.md` §5) | one `derived-verdicts` selector per module in `inventory-schema.json` (DATA — the PB198 mechanism) + one witness negative test per module | the selector + the witness; the drift test holds the population |
| **Adjudication** | never-adjudicated rules (3,361) | `phase_b_batch.py` batches of ≤20 rules per agent, **dossier-fed** (§5.2), refuters read only the CONFORMS / DNS candidates | a verified verdict; CONFORMS rows then go to the golden lane, defective rows to `kb/Work/` |
| **Fix** | defective rows (PARTIAL / NOT-IMPLEMENTED / DIVERGES) via their `kb/Work` notes | clusters by MECHANISM, not by note; the proven loop (implementer → self-review → fleet → director → lander) with the fleet sized in §5.3 | the fix + golden + re-verdict in one change set; one comprehensive battery per cluster batch |
| **A.1 documentation** | Annex A.1 DOC rows (222; 49 now carry a verdict, 45 §7 determinations exist) | the schema's `kinds.DOC` defines what a DOC row costs (§4); then batches by statement family | a §7 determination as `code-location` + a witness golden as `test-ref` — **except** a DOC row an A.4 module the owner declined has **withdrawn**, which belongs to the **Derived verdict** lane instead and closes on that module's witness alone (A.1's preamble makes the item not required, so there is no determination to file: §4, §8.1; four rows — items 84, 85, 173, 86 — as of 2026-09-02). ⚠ A DOC row the derived lane stamps because the element is **optional and not provided** (PB280 Q1) is NOT that case: it has a determination, so it stays on this lane's evidence rules in full (items 127, 206) |

⛔ **THE ADJUDICATION LANE IS VERDICT-ONLY, AND THE GOLDEN LANE STAYS A SEPARATE LANE** (owner decision,
2026-09-04, `kb/Work/PB468` Q1). It was put to the owner as a bare question — *may the agent that assigns a row's
verdict also author that row's witness, given a mechanically checked pre-run predicted expectation and an
independent blind replicate?* — and the answer was **no: keep the lanes separate.** The proposal it declines is
recorded whole, with the measurement that motivated it, in
`docs/rearchitecture/evidence/PROCESS-REVIEW-2026-09-04.md`; that evidence is frozen and is **the analysis, not
this document's process.**

The reason is **independent derivation of the evidence.** An agent that has located and read the implementing code
has already formed its expectation *from that code*; a witness the same agent then authors is checked against that
one derivation, and a pre-run predicted expectation or a blind replicate checks the *record* of it rather than
supplying a second one. Two lanes each read the standard for themselves and produce **two** derivations, and the
second one demonstrably earns its cost: every adjudication batch so far has overturned some CONFORMS verdicts, and
always downward. §1's definition of DONE is therefore unchanged, and so is the price it implies — the same rule is
read twice, once to verdict it and once to witness it. That cost is accepted as the price of the guarantee; it is
not waste, and it is not to be removed by merging the lanes. The measured consequence, recorded so nobody
rediscovers it as a surprise: an adjudication batch on its own moves the GAP only ~8–11 %, because its CONFORMS
rows wait for the golden lane.

The same decision left **`kb/Work/PB468` open with six further questions** the owner has not yet been asked — the
implementer cap, table tests closing many rows, effort tiers, the already-OK "shall" rows with positive-only
evidence, Report Writer, and extending PB386's derivation arm. Three of those would change §1 or §5 if answered
yes; none of them is in force.

**Batch order in the adjudication lane** alternates a *defect-rich* batch — §14 statement groups, grouped by
mechanism (the I/O statements together, the string statements together) so the notes it produces already cluster —
with a *closure-rich* batch (§13, then §12, §11, §8, §7, the small clauses; §13/§12 adjudicate ~96% CONFORMS where
§14 adjudicates 64%). The lane stays one batch ahead of the fix lane: the GAP moves every battery and the fix lane
never starves.

### 5.2 The dossier — agents verify, they do not search

Phase-B batch 8 agents spent ~120 turns each, most of them discovering what they were to verify. The batch
generator therefore hands each agent, beside the rule texts: the `.g4` rules for the construct, the binder /
emitter / runtime files that cite the clause, every test and golden that cites it, and the `CONFORMANCE.md`
determinations for it. Discovery is a grep; verification is the agent's job.

### 5.3 The fleet is sized to the cluster, and it runs once

A fleet per wave at ~100 agents was the largest token sink relative to burn-down, and five of the last eleven
findings were defects the wave itself introduced. So: the implementer runs the four review lenses as a **self-review
checklist before** any fleet; the fleet is **~40 agents** (four lenses, two refuters per finding) and runs **once
per cluster**; implementer agents receive the apply-ready contract, not a discovery brief, with a turn budget.
Refute stages on anything that CLOSES a row are never dropped (§9).

Two mechanical enablers, each its own change set: the A13 collection split (landed 2026-09-02 as partitions — the
Conformance leg fell 721 → 600 s, not the ~10× predicted, because the box saturates at 24 physical cores; the next
lever is a persistent compile host), and the pinned-worktree convention that lets the adjudication lane read while the
fix lane builds.

**⛔ How every lane is RUN is the owner's standing instruction of 2026-09-02, and its operational SSOT is the
`workstream` skill (`.claude/skills/workstream/SKILL.md`, with the brief and workflow templates):** checkpoint to disk
(WIP commits + `STATUS.md` per worktree; one JSON line per rule per workflow stage), replace a killed agent with a
fresh one reading the checkpoint, keep to the concurrency budget (one lander + ≤3 implementers + one four-wide
read-only chunk), land finished work first with one lander on main at a time, allocate ids centrally. The fleet
sizing above is a consequence of that budget, not a separate rule.

## 6. Governance / relationship to existing artifacts

- **This IS P14 Step-0** (the traceability inventory), re-grounded spec-first + exhaustive — supersedes any
  feature/legacy-checklist framing of Step-0.
- **Divergences → §24** (`PHASE-13-plan-vs-spec-review.md`) — the verified defect/fix-queue SSOT.
- **Editions → the VERSION TEST MATRIX** (`VERSION_CHANGE_REFERENCE.md` + `VERSION_TEST_MATRIX_DESIGN.md`) — the
  (construct × edition) dimension; introduction/continuity/behavior invariants.
- **Seed / first installment:** `DESIGN-SPEC-RECONCILIATION.md` (design-doc audit, 54 conflicts) + `CODE-SPEC-AUDIT.md`
  (code audit, CA1–CA38). The full review is their exhaustive completion.
- **Spec-first discipline:** `feedback_spec_is_the_oracle`, `feedback_spec_is_the_oracle`, `feedback_spec_scopes_not_tests`
  (implement/verify COMPLETELY to the spec — tests verify, never scope).

## 7. Risks + discipline

- **Catalog exhaustiveness** — a missing rule is silent false confidence; the completeness critic + TOC cross-check
  guard it. The denominator's completeness is itself reviewed.
- **Verdicts must be spec-derived**, never differential — that is the entire point; a "matches the legacy" verdict is
  invalid.
- **Candidate verdicts are agent-surfaced** ⇒ VERIFIED before any code change (the verify-then-fix loop already in
  flight for the ~44).
- **Scale** — incremental/resumable inventory; the GAP burn-down is the metric; do NOT attempt it in one pass.
- **Optional-module non-support is an OWNER decision** (D13), recorded in `CONFORMANCE.md`, never an agent's call.

## 8. HOW A VERDICT IS RECORDED — the Phase-B mechanism

Phase A left a 3,790-row inventory and no way to write a verdict into one: `build_inventory.py` only ever
*preserves* the adjudicated fields across a rebuild, and nothing checked what a row claimed. That gap is not a
missing convenience. **The GAP count is the definition of v1.0** (D13), so a row is a claim about completion, and
an unaudited claim about completion is a self-report. The mechanism below exists to make each of the three claims
in §1 — located, verified, tested — cost what it is supposed to cost.

**Nobody hand-edits the inventory.** A reviewer or a Phase-B agent emits a **batch file** of verdict records;
`scripts/spec/record_verdicts.py` merges it.

```
python scripts/spec/record_verdicts.py --dry-run batch.json     # validate + report the GAP delta, write nothing
python scripts/spec/record_verdicts.py batch.json [more.json]   # merge
dotnet test tests/Cobol.Net.Tests.Unit --filter "FullyQualifiedName~SpecTraceabilityInventory"
```

A record sets only the adjudicated fields (plus the two retirement control fields below) and is keyed by `rule-id`:

```json
{"rule-id": "AR-15.7-1", "verdict": "CONFORMS",
 "code-location": "src/Cobol.Net.Compiler/Binder/Intrinsics/IntrinsicSignatures.cs#Abs",
 "test-ref": "conformance:2023/intrinsic_abs; unit:IntrinsicArgumentRuleTests.Abs_RejectsAlphanumeric",
 "editions": "85,2002,2014,2023", "notes": ""}
```

**Witnesses MERGE; statements are restated (kb/Work PB959).** `code-location` and `test-ref` are sets of
witnesses (`inventory_schema.WITNESS_FIELDS`): the writer UNIONS a record's values with the row's, so a record that
names one new test adds it and a record that omits the field keeps what the row had. `verdict`, `editions`,
`notes` and `derivation` are single statements the record restates. Three refinements, each measured:

- **A witness leaves a row only by a named, reasoned retirement** — `"retire-witnesses": "<ref>; <ref>"` plus
  `"retire-reason"`. Each ref must be on the row; the writer drops it and appends `retired-witness: <ref>
  (<reason>)` to the row's `notes`, which is the durable mark the gate below reads. Any other loss is refused.
- **A re-adjudication re-sites `code-location`.** A record that CHANGES the verdict and names a code-location
  replaces that field (`inventory_schema.resites`): a PARTIAL row's code-location names the defect site, and a
  union would present it as implementation evidence for the CONFORMS record that closes it. The inventory's own
  history showed this is the dominant legitimate removal (1,105 of 1,714). `test-ref` is never re-sited.
- **A witness-only record** — `{"rule-id": …, "test-ref": …}`, no verdict and no statement field — adds
  witnesses to an adjudicated row and restates nothing, so it is safe to re-apply on a merged tree after another
  batch re-adjudicated the same row.

**The count is gated, not only the resolution.** `SpecTraceabilityInventoryDriftTests` checks that each surviving
reference RESOLVES and deliberately not how many there are, so until PB959 a batch that subtracted valid evidence
left every gate green. `scripts/spec/audit_witness_loss.py --check` (run by `build-local.{sh,ps1}` and
`battery.sh`) compares the inventory with the merge-base with main and is RED on any witness lost without a
retirement mark or a re-site; `--history` replays the file's git history under the merge rule and
`--restore-batch` emits the witness-only batch owing back what the old overwrite silently removed.

**The five parts, and why each is where it is.**

| part | file | owns |
|---|---|---|
| the rules, as DATA | `tests/version-matrix/inventory-schema.json` | the verdict vocabulary, each verdict's `resolves` flag and required evidence, the legal editions, the `code-location` pattern, the `test-ref` forms, and **`kinds`** — the PER-KIND evidence rules (§4) |
| the Python loader | `scripts/spec/inventory_schema.py` | parsing that schema, deriving `state`, and the ATOMIC inventory write |
| the writer | `scripts/spec/record_verdicts.py` | validating a batch's SHAPE and merging it all-or-nothing — witnesses by union (PB959) |
| the witness-count gate | `scripts/spec/audit_witness_loss.py` | that no row LOSES a witness without a retirement or a re-site (PB959) |
| the battery gate | `tests/Cobol.Net.Tests.Unit/SpecTraceabilityInventoryDriftTests.cs` | REFERENTIAL integrity, continuously |
| the register audit | `scripts/spec/audit_annex_a1.py`, run by `AnnexA1RegisterDriftTests` | that `docs/CONFORMANCE.md` §7 is internally sound and agrees with the inventory |

- **`kinds` is orthogonal to `verdicts`, and that is the point.** `verdicts` says what a VERDICT costs; `kinds`
  says what a ROW OF THIS KIND costs. Before it existed the schema described only one kind of claim — a
  behavioural rule located in a binder and pinned by a golden — so all 222 Annex A.1 rows sat verdict-less while
  §7 already carried 39 determinations, a third of that work invisible to the burn-down. Adding a kind is an
  edit to the JSON; the two evaluators read it, and neither carries a copy.

- **The vocabulary is data, in one file.** Three consumers read it — the row builder, the writer and the C# gate —
  and none carries a copy. Adding a verdict is an edit to the JSON and nothing else. This is CLAUDE.md rule 5
  applied where drift is most likely, and the DA wave's lesson (`feedback_one_rule_one_place`) applied before the
  fact rather than after it.
- **Shape at record time, reference at battery time — with NO overlap.** The writer checks that the rule-id is
  real, the verdict is in the vocabulary, the required fields are filled, the editions are legal and each
  reference parses. It does **not** check that a `code-location` symbol exists or that a `test-ref` names a real
  test. That belongs to the gate, because it has to keep holding as the tree changes underneath a row recorded
  sessions ago, and implementing it in both places would be the duplication this whole design is trying to avoid.
- **All-or-nothing, then an atomic replace.** Every record across every named batch validates before any of them
  merges, and the write goes through a temp file. A half-applied batch is the worst outcome available: the rows
  that DID land look exactly like reviewed work, and nothing afterwards can tell them apart.
- **`state` is recomputed, never believed.** Both the Python side and the gate derive it from the verdict and its
  evidence. Closing a GAP by hand-editing a string fails the battery.
- **⛔ ONLY A SPEC-DERIVED TEST CLOSES A ROW — §1(c), enforced rather than trusted.** Each `test-ref` form carries
  `spec-derived`: `conformance` / `unit` / `conformance-test` yes; **`nist` and `characterization` no**, because a
  NIST CCVS golden and a characterization snapshot are regression NETS (CLAUDE.md rule 1) and a differential is
  structurally blind to a violation both implementations share. An xUnit form *can* be spec-derived, so the form
  alone cannot decide it — `disqualifying-method-patterns` additionally strikes any test whose method name says
  otherwise (`*_MatchesLegacy`, `*_MatchesGnuCobol`, `*_MatchesOracle`). A non-qualifying ref is still worth
  recording as corroboration; it simply cannot be what closes the row.
- **`requires` is NOT "what closes the row".** `requires` is the evidence a RECORD needs to be well-formed —
  evidence for the VERDICT. Closing additionally needs the spec-derived test. Keeping them apart is what makes
  **CONFORMS-but-untested** expressible, the Phase-C category §3 names: the rule verified against the code, no
  test yet pinning it, row still a GAP and one golden from done. Folding the test into `requires` would force
  every such row to be misrecorded as PARTIAL — or closed on whatever test happened to touch the function.

> **⚠ BOTH OF THE ABOVE WERE LEARNED FROM THE FIRST BATCH, NOT DESIGNED IN.** It returned 19 CONFORMS rows
> carrying a covering test, and **7 rested on nothing but a NIST golden or a `*_MatchesLegacy` differential** —
> they would have moved the v1.0 burn-down on exactly the evidence this document's §1(c) rules out. The cause was
> upstream of the agents: the fan-out prompt said which forms RESOLVE and never that a cover must be spec-derived,
> and the schema listed `nist:` with nothing marking it differential. Any future fan-out prompt must state the
> spec-derived requirement explicitly. `session-probe` had the same blind spot in the same direction — it counted
> "test-needed" as an EMPTY `test-ref` rather than as a row that did not close, under-reporting 11 as 4.

### 8.1 The DERIVED verdict — one determination, applied by a predicate rather than by a list

Some verdicts are not adjudicated per row at all. When the owner declines a whole optional module, every rule the
module conditions becomes unreachable *at once*, and there is nothing left to decide about any single one of them.
Recording that by hand produced the failure kb/Work PB198 measured: sixteen rules on one question carrying **four
different verdicts** — NOT-IMPLEMENTED (9), blank (2), NEEDS-OWNER-DECISION (4) and CONFORMS (1).

**Two shapes qualify, and the second one is newer than the mechanism's own description of itself.** Until
2026-09-02 "the decision makes the rules unreachable" was written down as if it were the definition; PB280 Q1 is
the counter-example. Its rows are perfectly **reachable** — an A.1 documentation obligation for an element a user
can write today — and what is common to all of them is the **adjudication**: the owner answered, once, a question
each of up to 30 rows asks in the same words ("an A.1-OPTIONAL element §7 records as *Not provided.* closes as
DOCUMENTED-NON-SUPPORT"). A selector of the second shape wants reading twice as carefully, because its rows are
live surface; the protection is that its arms turn on **what the owner wrote** — the requirement class the
standard states and the determination this repository filed — never on an agent's reading of a rule's text.

So the module's scope is written **once, as DATA**, in `inventory-schema.json` under `derived-verdicts`, and
**both engines read it**: `DerivedSelector` in `scripts/spec/inventory_schema.py` (driven by
`scripts/spec/derive_verdict_batch.py`, which emits the batch) and `DerivedVerdictDriftTests.Select` (which holds
the population every build). Neither carries a selector. ⚠ That claim was *aspirational* from PB198 until
2026-09-02 — no Python consumer of `derived-verdicts` existed, so the PB198 batch was hand-made and the schema's
own `$comment` asserted a second reader that was not there.

**A selector is `arms`: a disjunction of conjunctions.** Within an arm every field present must hold; a rule is
selected when any arm holds and no entry-level `excludes-patterns` matches its text. The fields exist because the
standard scopes a module in more than one way and each way was, at some point, the one a text-only predicate
missed:

| field | the scoping mechanism it reads | the module that needed it |
|---|---|---|
| `sections` | the rule's own CLAUSE — §8.8.1.4 is *titled* for the declined mode and its rules never repeat the phrase | A.4.3, A.4.8, A.4.14, PB198 |
| `pattern` | the rule's TEXT — the facility's functionality stated from inside a mandatory clause | PB198, A.4.14 |
| both, as an AND-gate | a term that is STATEMENT-LOCAL (`file-name-1` means the FD's own subject in §13.4.5.4) or an OPERAND NAME (§12.3.7's `data-name-1` IS the CURSOR operand) | A.4.13, A.4.2 |
| `xref-sections` + `kinds` | an Annex A.1 documentation obligation citing a declined clause, keyed on the same clause numbers as the clause arm | A.4.8, A.4.14 |
| `excludes-kinds`, per arm | a general format (kind FMT) is evidence about a CLAUSE, never one of its formats | A.4.2, A.4.3 |
| `requirement` | the A.1 **requirement class the standard itself states** ("This item is optional."), carried on the catalog row — A.1's preamble writes its licences per class | PB280 Q1 |
| `determination-prefix` | what **`docs/CONFORMANCE.md` §7 says** about the element, read off the register row keyed by the rule's own id — which is exactly the anchor `kinds.DOC.anchor-template` computes | PB280 Q1 |

Two properties belong to the **matcher**, not to any selector, because a convention each author must remember is
a convention one author will forget. Clause prefixes match **component-wise** — §13.18.30 is not inside §13.18.3,
and a raw `StartsWith` had the screen selector taking 543 rules instead of 156, flipping 32 CONFORMS rows to
non-support. And the text is **hyphen-normalised** before matching, because 29 catalog rules spell an operand name
with U+2011 where the rest use ASCII `-`, which silently *under*-selects — the direction that leaves the drift
test green.

⛔ **`determination-prefix` is the first arm field that reads an artifact outside the catalog, and it has to be:
a DOC row's implementation IS its determination.** Three consequences, each load-bearing. The §7 table therefore
has **one parser per language** — `inventory_schema.section7_rows` and `tests/_shared/ConformanceRegister.cs` —
because three readers now need it (the register audit, `anchor_obliged`, this arm). An item with **no** §7 row is
never selected: a missing determination is not a negative one. And the match is on the cell's **opening** words
with markdown emphasis stripped, never `contains` — item 127's determination discusses what is *not* provided
three sentences into its body, and the opening words are the register's answer to A.1's question.

⛔ **THE `requirement` AXIS IS STILL NOT FALSIFIABLE AGAINST TODAY'S DOCUMENT; the `determination` axis
BECAME so on 2026-09-09.** When this landed, all 30 A.1-optional items and all 47 §7 rows agreed — the only
"Not provided." determinations were on optional items, and the only optional items with a determination said
"Not provided." — so a predicate that had dropped `requirement` altogether, or matched *any* determination,
selected the same two rows and left `DerivedVerdictDriftTests` green. ⭐ Re-measured 2026-09-09 the second half
is no longer true: §7 now carries **58** rows, and **5** of A.1's 30 optional items have a §7 row (10, 127,
150, 184, 206) of which only **2** open "Not provided." — item 206 left the set when owner decision Q30
(`kb/Work/PB592`) found that §13.18.60.4 GR12's SIGNED minimum bands are OPEN at the bottom and the element is
PROVIDED. A predicate that matched *any* determination now selects five rows and this file's own live
assertion (every selected row's cell opens "Not provided.") turns RED. The `requirement` axis is still blind
live, because every "Not provided." row remains on an optional item. That is precisely the
shape `feedback_green_gates_arent_evidence` names, so each axis is driven against a **fabricated** catalog and a
**fabricated** register by `python scripts/spec/derive_verdict_batch.py --self-test`, which
`TheSelectorEngine_ProvesEveryAxisCanFail` shells every build and which asserts the **case names**, because a
shrinking self-test still exits 0. The axis that most needs it is `requirement`: a **required** item whose §7 row
said "Not provided." is a conformance *defect*, and stamping it DOCUMENTED-NON-SUPPORT would document
non-conformance as if the owner had licensed it.

**Three properties keep a derived verdict honest, and they are what distinguish it from a shortcut:**

1. **It may never overwrite an adjudication.** A selected row may be blank, or may already carry the selector's
   own verdict (agreement — the reassuring direction). A row carrying CONFORMS, PARTIAL or DIVERGES means the
   determination and a human reading of the code disagree, so `derive_verdict_batch.py` **refuses the whole
   batch** rather than reporting a rewrite the way an adjudication batch legitimately does.
2. **One row, one determination.** Two selectors claiming the same rule would leave it carrying whichever batch
   ran last; the generator refuses it and `NoTwoSelectors_ClaimTheSameRow` asserts it against the data.
3. **It does not close the row.** DOCUMENTED-NON-SUPPORT resolves, but §1(c) still demands a spec-derived test
   proving the documented posture is what *actually happens* — and for a declined module that is the module's
   **witness**: a negative case showing the construct is refused, by name. The 2026-09-02 A.4 landing stamped
   **308 rows** and moved the GAP by **zero**, which is the correct and expected outcome; the PB280 Q1 landing
   stamped **2** and moved it by zero for the same reason (their witness debt was `kb/Work/PB373`, DISCHARGED
   2026-09-09 — item 127 on `conformance:2023/pb373_object_computer_name_inert`, and item 206 by leaving the
   selector altogether when owner decision Q30 / `kb/Work/PB592` found the element IS provided, closing CONFORMS
   on `conformance:2002/pb592_binary_usage_range_bounds`).

**What a selector cannot reach is recorded, not silently dropped** (`feedback_measure_the_selectors_complement`).
The standard also scopes rules by the printed **FORMAT band**, and the catalog does not carry it —
`extract_rule_catalog.py` glues the heading onto the preceding rule's text — so twelve rules that *are* solely
conditioned stay outside the predicate and are named in the schema's `$format-band-residue` and in kb/Work PB284.
Reaching them by regex would be a hand list in regex clothing; the repair is the extractor, and a denominator
change rebuilds the inventory and is its own landing.

**What the gate actually enforces** (eleven facts, and it is proven able to fail — see below): the inventory covers
the catalog exactly, no row contradicts its catalog entry, every verdict is in the vocabulary, every stored `state`
equals the derived one, every verdict carries its required evidence, every edition name is real, every
`code-location` names a file that exists and a symbol still in it, and every `test-ref` resolves —
`conformance:<edition>/<case>` and `nist:`/`characterization:` to a `.cob` on disk, `unit:<Class>.<Method>` and
`conformance-test:<Class>.<Method>` to a method really declared in that class. Plus the two `kinds` facts (§4):
every kind-anchored row whose VERDICT claims a determination carries the anchor its OWN rule-id computes (§4's
`anchor-exempt-verdicts` carve-out), and every fragment on an `anchored-files` path is a legal anchor of that
file's space — an anchored file being cited bare included.

**And the register audit runs every build.** `audit_annex_a1.py --check --json` was wired into nothing until
2026-09-02 — not `battery.sh`, not the CI workflow, not `build-local.*` — so the A.1 register's own correctness
was enforced when a human remembered. `AnnexA1RegisterDriftTests` now shells it from the Unit project, following
`ExternalCorpusPopulationDriftTests`'s precedent for a Python gate, which puts it in the per-commit wave-local
gate, battery phase 1 and both CI unit jobs at once. It asserts its POPULATION (a run that filed nothing is not a
pass), that every §7 row is filed under the item A.1 names, that no `DOC-A.1-<n>` token occurs outside a row key,
that each row's `Pinned by` agrees with its inventory row's spec-derived `test-ref`, that every verdicted DOC row
has a determination, and that the script's own `--self-test` still names every case it drives.

> **⚠ A gate observed only passing is indistinguishable from a gate that inspects nothing**
> (`feedback_green_gates_arent_evidence`), and this one will spend most of its life green over rows nobody has
> touched. Two things answer that. `TheseChecks_ActuallyFail_OnAFabricatedInventory` drives every check above
> against rows built to break it — one defect class at a time, plus positive controls so the failures cannot come
> from a checker that rejects everything — and it runs on every build. And the gate was confirmed against the
> REAL artifact once, by corrupting `traceability-inventory.json` in place: a row hand-promoted to `OK` and a
> CONFORMS row citing a file and a golden that do not exist turned exactly three tests red, then green again on
> restore.

### 8.2 The DERIVATION — how §1.1's evidence kind is recorded and checked

§1.1 says WHAT a derivation is and what it may not do; this says where the parts live. **The reference shape is
the DOC row's, reused rather than paralleled**: an anchor COMPUTED from the row's own rule-id, carried on the
row, cross-checked against a register PARSED by one reader per language.

| part | file | owns |
|---|---|---|
| the rules, as DATA | `tests/version-matrix/inventory-schema.json` → `derivation` | the record field, the anchor template, the §8 heading, the owner signatures (one per dated owner decision), and the four arms with their `names-pattern` + `check` |
| the register | `docs/CONFORMANCE.md` **§8** | the determinations themselves — one row per closed rule, keyed `DRV-<rule-id>` |
| the register parser | `inventory_schema.register_section` / `tests/_shared/ConformanceRegister.cs` | ONE markdown-table reader per language, now taking the HEADING and stopping at the next `## ` — §7 and §8 are two calls, not two parsers |
| the undefined-element list | `tests/version-matrix/annex-a2-undefined.json`, generated by `scripts/spec/extract_annex_a2.py` | Annex A.2's 66 items, each item's own trailing citation, and the catalog `rule-id`s that citation RESOLVES to |
| the state rule | `Schema.state_for` / `DerivedState` | the same clause, in both engines, in the same change set (`kb/Work/PB315`'s lesson) |
| the writer | `record_verdicts.validate` | the six refusals of §1.1, at record time |
| the gates | `SpecTraceabilityInventoryDriftTests` · `AnnexA2UndefinedListDriftTests` | that every derivation row still holds, and that the A.2 artifact still equals the spec |

- **The A.2 list is a GENERATED ARTIFACT, not a parse at check time, and that is the shape that makes the next
  A.2 row automatic.** The extraction lives in exactly one place — a Python script that reads
  `specs/ISO_COBOL.md` and resolves each item's citation against `spec-rule-catalog.json` — and both engines then
  read the resolved `rule-ids` as data. Parsing the spec markdown in C# as well would be the second parser this
  design spends its whole length avoiding, and re-parsing 66 items per row would put a 1.3 MB read on the path
  `EveryRowState_IsDerived_NotAsserted` walks 4,311 times. `extract_annex_a2.py --check` regenerates and diffs, so
  the artifact cannot go stale silently; `AnnexA2UndefinedListDriftTests` runs it every build, the
  `AnnexA1RegisterDriftTests` precedent.
- **Adding an A.2-arm row costs one register row.** The item→rule-id resolution is mechanical, so the next rule
  the standard declares undefined needs no edit to any list — only the determination and its `A.2 item <n>`.
- **⛔ §7's `DOC-A.1-<n>` token invariant had to learn the second register.** `audit_annex_a1.py` asserts the
  COUNT of each `DOC-A.1-<n>` token in `CONFORMANCE.md` equals the number of §7 rows keyed by it — a check that
  exists because an inventory row resolves its anchor by a word search over the WHOLE file, so a prose mention
  would otherwise read as a filed determination. A §8 row keyed `DRV-DOC-A.1-19` is a second, legitimate
  occurrence. The invariant is therefore stated over BOTH registers (`§7 rows keyed T` + `§8 rows keyed DRV-T`),
  and `check_pins` — which is what actually holds a DOC row's evidence in agreement with §7 — was widened at the
  same time from "closes on a spec-derived test-ref" to "closes", so the guard that matters did not weaken.

## 9. RUNNING A PHASE-B BATCH — the fan-out, and what its prompt MUST carry

A batch is: pick the next contiguous clause block → generate one input file per subject → fan out one agent per
subject → hand each result to an INDEPENDENT agent told to overturn it → merge → gate.

```
python scripts/spec/phase_b_batch.py 15.32-15.44     # writes scratchpad/phase-b/in-<slug>.json, prints the slugs
#   … each file carries the subject's DOSSIER; --no-dossier omits it, --include-adjudicated is NOT for a batch …
#   … run the workflow (one adjudicate agent + one refute agent per slug) …
python scripts/spec/record_verdicts.py --dry-run scratchpad/phase-b/out-*.json
python scripts/spec/record_verdicts.py scratchpad/phase-b/out-*.json
dotnet test tests/Cobol.Net.Tests.Unit --filter "FullyQualifiedName~SpecTraceabilityInventory"
```

`phase_b_batch.py` excludes rules that already carry a verdict, so a re-run after a partial batch is safe.

**Every input file also carries a DOSSIER — the discovery, pre-computed, so the agent VERIFIES instead of
SEARCHING.** Batch 8 (2026-09-01) cost 574M cache-read tokens over 39 agents for 224 rows, and most of each
agent's ~120 turns went on the same three greps — where is the grammar rule, where is the binder and emitter,
where are the tests — a search with one answer per subject that was being bought once per agent. The dossier
computes it once per subject and pastes it into each of that subject's part files: the ANTLR rules (matched on
the rule name, on the clause its comment block cites, and on the construct keyword), every `src/Cobol.Net.*` file
citing the subject's clause with the citing lines, the same for `tests/` including the `.cob` goldens, the
`docs/CONFORMANCE.md` determinations, the `kb/Work/` notes whose `spec_refs` or `inventory_rows` fall inside the
subject **with their `status`** — which is how §8 item 8's "do not re-report a landed fix" gets answered without
anybody hand-writing the list it forbids — and the citing `docs/DIAGNOSTICS.md` rows. Nothing is hand-mapped:
the clause set comes from the catalog rows plus their construct root, and the keyword from the catalog's own
`subject` field, because a hand-kept construct→file table would be a sixth work register (CLAUDE.md rule 8) whose
staleness would be invisible — a dossier missing an entry looks exactly like a construct that has none.
⛔ **That is also the dossier's limit, and every file repeats it to the agent in `how-to-read`: it is a selector,
so it is evidence about what it RETURNED and none about what it dropped.** An empty list means "this derivation
found none", never "the compiler has none" — 8 of the catalog's 599 subjects are prose with no keyword to search
with at all, and code implementing a rule without citing its clause is invisible to it. A NOT-IMPLEMENTED verdict
still needs the agent's own search. The generator prints a per-subject count table, and a row of zeros is the
signal that that one agent starts cold.

### ⛔ The refute stage is not optional, and the evidence is now three batches deep

Every batch's refute stage overturned verdicts, and **every overturn was a DOWNGRADE** — the pass only ever
removes confidence. It has also found defects the adjudicator looked straight at:

- **PB5** (the quantizer saturating at 9.2 × 10⁹) — the adjudicator had sampled only small receivers, where the
  clamp cannot bite. The refuter read the implementation instead of sampling outputs.
- **PB7** (every zero-argument intrinsic unreachable in the keyword-omitted form) — invisible to output-sampling,
  because the program COMPILES and fails at run time.

So: **an all-CONFORMS report is a red flag, not a good result**, and that sentence belongs in the refuter's prompt.

### What every batch prompt must carry — each line paid for by a defect

1. **The spec is the only oracle**, and the legacy / GnuCOBOL / existing goldens are regression nets.
2. **Validate every citation** with `cite.py --check`. Give the concrete precedents: the fabricated
   "§12.3.7 GR7 k3 … distinct ascending" that came out of *our own design doc*, and the one-level-short shape
   (§13.18.60 where the rule is in §13.18.60.4).
3. **Only a SPEC-DERIVED test closes a row.** Name `nist:`, `characterization:` and any `*MatchesLegacy` method
   as non-qualifying, and say that CONFORMS-but-untested is a legitimate, expected outcome. The first batch
   offered seven rows that would otherwise have closed on a differential.
4. **Editions are never derived from `IntrinsicCatalog`'s own row** — that is deriving the answer from the code
   under review, and it produced a wrong window in batch 1.
5. **CONFORMS costs something**: code not located ⇒ NOT-IMPLEMENTED; an edge unverified ⇒ PARTIAL; a rule with
   two halves where one was verified ⇒ PARTIAL, always.
6. **`DOCUMENTED-NON-SUPPORT` is never an agent's to choose** (owner decision, D13).
7. **`code-location` is `path#Symbol`, never a line number.**
8. **The fixes that have already landed**, so agents do not re-report them — and so they know that a function
   ABSENT from `IntrinsicArgumentRules.Verified` is genuinely unscreened, because that table is deliberately
   partial and grows as this review adjudicates each clause.
   ⛔ **THE LIST IS NOT WRITTEN HERE AND MUST NOT BE.** It read "PB1–PB7" while PB8–PB11 and PB13 had all landed,
   then "PB1–PB11 and PB13" while a dozen more had — a hand-maintained roster inside the very doc that warns
   against one, which is what CLAUDE.md rule 8 forbids. **Generate it when writing the batch prompt:**
   `python scripts/spec/work.py stats`, or `status: landed` over `kb/Work/`. A stale roster is worse than none:
   it tells an agent a fixed defect is open and the batch is spent re-reporting it.
   ⚠ Also tell agents that `Verified` now screens by CLASS SETS, not one class per operand — a figurative
   constant is admissible wherever ANY class it can present is admissible (§8.3.3.6.4 GR4; fix-queue PB48) — so
   "the screen accepts ZERO here AND there" is correct behaviour, not a missing rule.
9. **Read-only, and no `dotnet build`/`dotnet test`** — other work is usually in flight.

### ⚠ Reading the results

- **Wait for the workflow's completion notification.** Output files exist from stage one and are REWRITTEN by
  stage two; reading early published a wrong GAP number once, and because every overturn is a downgrade, an early
  read biases the result *upward*.
- **Read the notification's `<failures>` block before its results.** One run reported "completed" with 5 of 12
  refuters dead on API 529, leaving unrefuted stage-one output in their files. `resumeFromRunId` re-runs only the
  failures.
- **Cluster before triaging.** The finding count is not the defect count: batch 1's 42 open rows were three root
  causes for 31 of them; batch 2's 41 were 34 of a single one.
