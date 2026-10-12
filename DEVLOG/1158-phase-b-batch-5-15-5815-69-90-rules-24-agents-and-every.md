## Entry 1158 — 2026-08-03 20:30 PDT — Phase-B batch 5 (§15.58–15.69): 90 rules, 24 agents, and every refute-stage overturn was a downgrade for the fifth batch running

Owner instruction: adjudicate. Batch 5 over §15.58–15.69 — LOWEST-ALGEBRAIC · MAX · MEAN · MEDIAN · MIDRANGE ·
MIN · MOD · MODULE-NAME · NATIONAL-OF · NUMVAL · NUMVAL-C · NUMVAL-F. **12 subjects, 90 rules, 24 agents
(adjudicate → independently refute), 0 errors, 0 skipped, 0 empty results**, 5,842 s wall.

**RESULT: DIVERGES 42 · PARTIAL 21 · NOT-IMPLEMENTED 15 · CONFORMS 11 (3 closed the GAP, 8 test-needed) ·
NEEDS-OWNER-DECISION 1.** Inventory **330 → 420 adjudicated**, GAP **3799 → 3796**.

**⛔ THE REFUTE STAGE OVERTURNED ~16 VERDICTS AND EVERY OVERTURN WAS A DOWNGRADE — the fifth batch in a row.**
That is no longer a tendency, it is the measured behaviour of the loop, and the two mechanisms are the ones
`DESIGN-spec-conformance-review.md` §9 names by name:
- **"the adjudication SAMPLED OUTPUTS"** — RV-15.59.4-1 (CONFORMS→DIVERGES: the adjudicator sampled
  middle-of-range values 12.34 / −5.00 / 99.99 where the defect cannot appear); RV-15.68.4-1 (CONFORMS→DIVERGES:
  "sampled outputs through a single channel with well-behaved currency strings").
- **"read ONE of the TWO bodies"** — AR-15.64.3-2 and RV-15.64.4-1, both CONFORMS→DIVERGES, both because MOD has
  two implementations and the adjudicator read one and stopped.
An adjudicator's CONFORMS is worth exactly what its refuter leaves of it.

**EIGHT ROOT CAUSES, NOT 79 FINDINGS** (the playbook's "cluster before triaging"; the observed ratio holds at
~10:1). Filed as **PB30–PB37**. The one that matters most:

**⛔ PB32 IS THE PB2/PB13/PB14/PB28 FAMILY FOR THE FIFTH TIME.** `RenderNum` routes on `AnyRealArgument(ic)`, so
every statistical and MOD function has an EXACT `Int128` body and a FLOAT `double` body — and the refuters found
fix after fix applied to one arm only: MOD's zero-divisor rule, MEDIAN's even-count mean, MIDRANGE's
×5-at-scale-s+1. PB33 is the same shape once more (`TestNumvalC` has the 31-digit cap, `NumvalC` does not — the
validating twin corrected, the value-producing one not). **A dispatch with two arms where only one is ever
fixed is now this compiler's most reproducible defect shape**, and it deserves a structural answer rather than a
sixth individual fix.

PB37 is a genuine owner decision and is recorded as the bare question: §A.4.9 lists thirteen optional locale
elements and NUMVAL-C is not among them (item 12 is *TEST-NUMVAL-C function, LOCALE key*). Seven
NOT-IMPLEMENTED rows hang on whether that is deliberate or an editorial gap — documented non-support if
optional, a conformance gap if not. `DOCUMENTED-NON-SUPPORT` is never an agent's to choose (D13).

**⚠ THE MERGE WAS REJECTED THREE TIMES BEFORE IT LANDED, AND THAT IS THE MACHINERY WORKING.** `record_verdicts.py`
validates ALL-OR-NOTHING, so nothing was half-applied: (1) an `evidence` field I had added to the workflow's
schema is not in the accepted set; (2) test-refs and code-locations carried appended commentary
(`…#Algebraic_Fold (CORROBORATION ONLY …)`); (3) the battery gate then caught **18 code-locations** written as
`Foo.cs#Foo.Bar` where the file already names the class, so the symbol was never found.
`scripts/spec/normalize_batch.py` now fixes all three **and reports every change** — a silent normalizer would be
a second place verdicts can change without a reader. ⛔ It **preserves** rather than deletes: `evidence` folds
into `notes`, and a ref that cannot be parsed leaves its reason behind. Its first draft would have DROPPED a
legitimate spec-derived golden (`tests/conformance/2002/arith_standard.cob`) because commentary made it
unparseable — caught in the dry run, which is why the dry run exists.

⚠ Batch outputs were written to `scratchpad/phase-b/b5/`, not the shared directory, and merged **by name**: §0
records that the playbook's own documented `out-*.json` glob swept every prior batch into the batch-4 merge and
would have re-adjudicated PB11's freshly-closed rows backwards. The raw agent output is preserved at `b5-raw/`
so the normalizer's effect stays auditable. The tell that it went right: **the GAP went DOWN**, 3799 → 3796.

Gate: `SpecTraceabilityInventoryDriftTests` **10/10** after the code-location repair — the run that matters,
since it is what proves the recorded references actually resolve in the tree.

