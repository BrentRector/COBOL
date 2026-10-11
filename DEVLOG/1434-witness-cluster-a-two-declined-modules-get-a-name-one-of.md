## Entry 1434 — 2026-09-02 18:29 PDT — Witness cluster A: two declined modules get a NAME, one of them because the compiler was quietly SHIPPING it — and landing the batch exposed a rule written in three places and read in one

Module-witness cluster A (Annex A.4.8 FORMAT / SELECT WHEN, Annex A.4.13 WRITE FILE / REWRITE FILE), landed from
worktree `agent-a4e96fdfc1f71b6ca` (base `9f11a8d6`, five WIP commits), rebased onto cluster E.

**Why a declined module still costs work.** §4.2.7 lets an implementor decline any Annex A.4 element, and
`CONFORMANCE.md` §5 declines both of these. Annex A.4.1 then says the quiet part: *"Any associated syntax rules,
general rules, other rules, exception conditions, and I-O status values are also optional, even if not explicitly
listed"* — so the decline reaches 49 inventory rows. But a declined row does not close on silence. It closes on a
**witness**: a program that uses the element and is told, by name, that this implementation does not provide it.
Lane 2a's derived selector had stamped all 49 DOCUMENTED-NON-SUPPORT and [[PB281]] recorded that **not one could
close**, because the refusal a user actually got was an unnamed ANTLR syntax error.

**⭐ And for A.4.13 there was no refusal at all.** `CONFORMANCE.md` §5 said *"A.4.13 | REWRITE FILE and WRITE FILE
| Not claimed | No surface — a parse error today"*. Both halves of that note were false, in two different
directions ([[PB282]], a `kind: defect` with `under_rejects: true`): `WRITE FILE F FROM X` parsed, **bound and
ran** through a live `SequentialIoBinder` arm at every edition with no diagnostic, and `REWRITE FILE F FROM X`
compiled clean and threw at RUN TIME with an empty name in its message. That live arm is deleted — it was
undeclared support AND wrong against §14.9.51.4 GR8 — and PB282 flips to `landed` in this commit with its twelve
rows re-verdicted, because a defect note may not outlive its fix.

**One seam, not two bands.** `EditionContext.Declined(descriptor, seen)` is now THE declined-element policy seam,
and its severity is what picks the posture: **accept-inert (Warning)** for a facility whose absence is merely
missing — screen handling `COBOLNET1560`, VALIDATE `1580`, commit/rollback `1579`, MCS `1578`, all four re-routed
through it with zero direct emits left — versus **refuse (Error)** for one whose inert acceptance would be a
WRONG ANSWER. A.4.8 and A.4.13 are the second kind, and the descriptors say why: an inert FORMAT changes which
bytes reach the medium (§13.18.24.4 GR1), an inert SELECT WHEN picks the wrong record description entry
(§13.18.51.4 GR1) with an I-O status 45 path, and an inert WRITE FILE writes nothing where the program believes a
file was written. Measured on the CLI at 85/2002/2014/2023 **and under `--permissive`**: identical refusal, which
is the point — a declined module is not a dialect leniency.

**FORMAT becomes a reserved word without breaking COBOL-85.** The clause needed a lexer token, and §8.9 reserves
FORMAT only from 2002. The `cobol-words.json` nameSlot row is what keeps `01 FORMAT PIC X(4)` legal at `--std 85`
while the §8.9 funnel draws `COBOLNET0901` by name at 2002+. Proven, not assumed: the NIST leg is **349 / 349**
on this tree, and the eight new witnesses carry `*> reject-at: 85 2002 2014 2023`. `REWRITE rec RECORD FROM x`
also gained its optional `RECORD` (rendered p710 — legal on the mandatory arm and rejected before), and two stale
clause headers in `CobolIO.g4` were repaired (§14.9.46 → §14.9.51 WRITE, §14.9.36 → §14.9.35 REWRITE).

## ⛔ The landing found a defect the batch could not have found: [[PB315]]

Applying the 49-record batch and running the gate the writer itself prints turned `EveryRowState_IsDerived_
NotAsserted` **red**:

```
DOC-A.1-84: state 'GAP' but the schema derives 'OK' from verdict 'DOCUMENTED-NON-SUPPORT'
```

The Python writer and the C# gate disagreed about when a DOC row closes. `inventory-schema.json` →
`kinds.DOC.anchor-exempt-verdicts` encodes Annex A.1's own preamble — an item *is not required if the optional or
processor-dependent feature is not implemented* — so a DECLINED A.1 row owes no §7 determination to anchor and
nothing implemented to observe; it owes only its witness. The C# `DerivedState` asks `AnchorObliged` and gets
that right, and its comment even asserts *"this side and the Python writer read ONE rule"*. **They did not.**
`Schema.state_for` asked `anchor_for`, which is non-`None` for every DOC row, so a declined row was required to
carry a location the same schema forbids it to claim and could never reach `OK`.

The disagreement was invisible while no declined DOC row had a witness — both sides derived GAP, for different
reasons — and this batch is the first thing in the repo's history to supply one. ⭐ The drift test earned its
keep exactly here: the writer was wrong, the reader was right, and only a gate that RECOMPUTES could tell them
apart.

**The sweep found a third arm, and the grep did not.** `grep anchor_for` cleared the other Python callers
(`record_verdicts.validate` already asked `anchor_obliged`; `is_observable`'s use is correct). But
`audit_annex_a1.py --check` then produced three findings of the same shape from a *different file*:
`check_pins` demanded a §7 row for every DOC row closing on a spec-derived test — exactly backwards for a
withdrawn item, whose §7 row must not exist. Same fix, third site. The lesson is that the grep found the sites
that NAME the predicate, and the defect was in the site that should have named it and never did.

**And the self-test was quietly vacuous.** `audit_annex_a1.py --self-test` then failed on two cases, correctly:
its fabricated inventory rows carried no `kind` and no `verdict`, so under the corrected predicate every one of
them was exempt *by accident* and two checks had stopped testing anything. The fixtures are now real DOC rows,
plus a new case pinning the exemption itself against the claiming-row control that proves the check still fires.
`--self-test` is ALL GREEN with every check proven able to fail — which is the only thing that makes the
`--check` green worth reading.

## Registered, not fixed (each measured on this tree, not inherited from the report)

- **[[PB292]] — `RECORD DELIMITER IS STANDARD-1` compiles and RUNS with no diagnostic**, at every edition, while
  `CONFORMANCE.md` §2 row 26 declares the Annex A.3 element *Not claimed*. Probed here: the program prints `RAN`,
  exit 0. `recordDelimiterClause` parses both arms and every `src/` reference outside `Generated/` is zero —
  the grammar's own comment says *"parsed and ignored"*. §4.2.6 ¶3 is imperative: *"An implementation **shall**
  provide a warning mechanism at compile time to indicate use of syntactically-detectable processor-dependent
  language elements not supported by that implementation."* This is A.4.13's silent-accept shape again, one annex
  over, and the `feature-name-1` arm is its twin. The fix is now one `Declined` call on a **Warning** descriptor,
  because the seam this cluster built is exactly the right shape for it.
- **[[PB294]] — `DiagnosticRegistryDriftTests.EveryEmittedCode_IsACatalogDescriptor` inspects 2.3% of its
  subject.** Its `(?<![\w.])` lookbehind excludes every QUALIFIED emit call — which is how sites are actually
  written (`edition.Error("COBOLNET0710", …)`). Measured here: **487** literal emit sites in `src/`, the gate's
  regex matches **11**, and **151** distinct codes are emitted with **no catalog descriptor** — no
  `DIAGNOSTICS.md` row, no `--suppress` identity, invisible to the next-free allocation scan, which is the
  failure the test's own docstring names as how `COBOLNET1573` shipped with two meanings. It is how
  `COBOLNET1560` sat as a bare literal until this cluster catalogued it. ⚠ The one code the regex CAN reach and
  that is absent from the catalog as a string literal is nevertheless registered (via
  `EditionCodes.RemovedConstruct`) — so the test is green for a real reason on the only site it looks at, which
  is what let the vacuity survive: the sample is not merely small, it is unrepresentative.
- **[[PB293]] gains the sweep's measurement rather than a second note** (the RECORD KEY / ALTERNATE RECORD KEY
  `SOURCE` phrase, reached independently from the Annex A.3 side): its anonymous parse error is the *better* of
  the two declined shapes, PB292's silent accept is worse, and the fix order should follow that. Also recorded
  there and in PB292: `CONFORMANCE.md` §2 carries the placeholder citation **`13.x` in five rows** (5, 12, 13,
  26, 40) — unresolved citations inside the document that IS the §4.2.6 user documentation.

## Gate and numbers

Full suites on the rebased tree: Conformance `Passed! - Failed: 0, Passed: 5407, Total: 5407` (8 m; +9 over
cluster E — eight witnesses and one positive control), of which NIST `Passed! - Failed: 0, Passed: 349,
Total: 349`; Unit `Failed! - Failed: 2, Passed: 5211, Total: 5213` — the two `ExternalCorpusPopulationDriftTests`
refusing to report the absent GPL corpus as an empty one; Characterization `Passed! 33 / 33`. `semgrep/verify.py`
`cobolnet-raw-diagnostic-code-literal` **475 → 474** (catalogueing COBOLNET1560 removed a bare literal),
`no-biginteger` unchanged at 36 — the PB175 baseline did not increase. `work.py check` 364 items well-formed;
`audit_annex_a1.py --check` zero findings and `--self-test` all green.

**GAP 3456 → 3407** (49 rows: 46 on the corrected batch, then 3 more once PB315's fix let the withdrawn A.1 rows
close). Corpus **557 positive · 590 negative**. Next free diagnostic **COBOLNET1721** (1705/1706 sit below cluster
E's 1720; the band has holes and the probe reports the maximum, which is what matters).

