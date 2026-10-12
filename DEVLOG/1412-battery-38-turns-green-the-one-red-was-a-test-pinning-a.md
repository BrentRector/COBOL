## Entry 1412 — 2026-08-31 03:04 PDT — Battery #38 turns green: the one red was a TEST pinning a screen the compiler had already deleted, and the filter that let it through was chosen from the wrong end of the change

Battery #38's first run on tree `a3053400` was red on exactly one test out of 5124, and the interesting part is
which party was wrong.

```
CobolNet.Tests.Conformance.UdfInvocationTests.ReturningCategories_CarriedVsStaged1510(
    tag: "BIN", returningDecl: "01 L-R.\n  05 L-R-A PIC X(2).\n  05 L-R-B PIC 9(4) USAGE BINARY.",
    carried: False)  [FAIL]
Assert.False() Failure — Expected: False, Actual: True
```

The row asserted that a group RETURNING item with a `PIC 9(4) USAGE BINARY` leaf draws `COBOLNET1510`. It does
not, and it should not. **[[PB164]]'s F8 finding deliberately deleted that reject** (`06d9fa31`, DEVLOG 1406):
`UdfBinder.UdfReturningResidue` had hand-rolled the DISPLAY-only union `{ Category: Numeric, IsFloat: false,
Usage: Display }` as its group-leaf screen, and F8 replaced it with the derived predicate
`DataItem.ElementImageCapable` — the one place the question *does this leaf have a character image the group
codec can carry?* is answered for every consumer. The hand-rolled copy had gone on rejecting binary / packed /
COMP-5 / float / INDEX leaves for years after V59 gave those forms their pinned bytes.

**The ground, re-derived rather than inherited** (both `cite.py --check`ed here, not trusted from the note that
carried them): **§14.2.2 SR5** is the RETURNING-item rule and the only one — *"Data-name-2 shall be defined as a
level 01 entry or level 77 entry in the linkage section. The data description entry for data-name-2 shall not
contain a BASED clause or a REDEFINES clause."* A level, a section, and two forbidden clauses. No category
restriction, and no usage restriction either. **§8.4.3.2.4 GR1** then clones that description into the caller's
temporary — *"the description, class, and category of the temporary data item is that specified by the
description in the linkage section of the item specified in the RETURNING phrase"* — it does not narrow it. So
every shape `UdfReturningResidue` names is an implementation residue staged loud (§1.4), never a conformance
rule, and a byte-form numeric leaf stopped being one of them in DEVLOG 1406.

The re-derivation caught its own trap on the way through, which is worth recording because PB199 had predicted
it. Checking the fragment *"shall not contain a BASED clause or a REDEFINES clause"* on its own returns **SR1** —
the `data-name-1` rule for the USING formals, a different question with nearly the same sentence. The
discriminating fragment is *"level 01 entry or level 77 entry"* (SR5, `data-name-2`) against *"or **a** level 77
entry"* (SR1). `--check` is only a check when the fragment you feed it can belong to exactly one clause.

**The flip is not a compile-only claim.** The old row only ever asked whether the shape COMPILES, and moving it
to the carried arm on that basis would have re-asserted the widening from the same weak evidence that let it
drift. The row now sits in the carried arm *and* is backed by a new byte-true fact,
`ReturningGroup_BinaryLeaf_CarriesValuesAcrossTheActivation`: the function writes `"AB"` into the alphanumeric
leaf and `COMPUTE L-R-B = L-X * 2` into the binary one, the caller `MOVE`s the result into a like group and
displays both. Expected `A=[AB] B=[0002]`. **The failure branch was fired once before the green was believed** —
pinned to the silently-empty carry `A=[  ] B=[0000]`, the test goes red with `Actual: "A=[AB] B=[0002]"`, which
is byte-for-byte the CLI probe recorded in PB199. A compile-only assertion cannot tell a real carry from an empty
one; this one can, and it was made to prove it.

**Four other records said the deleted thing** — the fix is worthless if only the test moves.

- `UdfBinder.cs`'s `UdfReturningResidue` summary **contradicted itself**: its "Supported:" sentence had been
  correctly widened to `ElementImageCapable`, while the closing enumeration still ended *"and the group residues
  (… non-character leaves)"*, where the body now returns the pointer/object reason. One comment, two answers.
- `docs/COBOLNET_INTERPROGRAM_DESIGN.md` still described the carried shape as a *character-form* group with the
  *non-character-leaf* shapes staging loud.
- `docs/ISO2023_CONFORMANCE_PLAN.md` still listed *binary-leaf-group* RETURNING among the per-shape 1510 residues.
- The sweep for siblings of those sentences found a fifth, in a file nobody had named:
  `docs/rearchitecture/DESIGN-data-model.md` recorded the P11 scout's risk-3 note that the UDF guard's extra
  strictness — *"UDF rejects Binary/Packed too"* — was **deliberate**. It never was. It was a hand-rolled union
  that nobody had re-derived, and a design doc calling it a decision is exactly how such a screen survives a
  decade. Corrected in place, with the reason.

`docs/PHASE4_RECONCILIATION.md` says the same thing in two places and was left alone on purpose: its header
declares it a FROZEN HISTORICAL LEDGER authoritative as of the P10 close, so there it is true as history.

**Why the wave-local gate did not catch it, and what the filter should have been.** This is the PB36 shape §0
names in its own words — *a green test pinning the OLD behaviour sailed through to the comprehensive gate*.
PB164's filter was chosen from the corpus where the wave's new goldens live (Conformance 1429/1429, all
green — §0's own record of that gate). F8 edited `UdfBinder`, and `UdfBinder`'s tests are named `Udf*`. §0's rule already says
**pick the filter from what the change TOUCHES, not from where the new goldens live**; the rule was written down
and the wave still filtered on the goldens. This landing's gate ran `~Udf|~Call|~Function` and is green at
Conformance 479 · Unit 5064 · characterization 33.

There is a second, structural half worth keeping, because it is not fixed by picking a better filter.
`DisplayUsageUnionDriftTests` was added by the same wave precisely so a hand-rolled usage union "cannot appear
silently" — but it scans `src/` only. **Nothing watches a TEST that pins shut a screen a drift test is
unlocking.** The sibling lock `TierCRejectionTests` was re-derived correctly by waves 1/2/R40; the UdfBinder
copy's test was the one nobody swept, because that screen was a separate hand-rolled copy living in a different
subsystem. PB199 lands with the defect it names; that half is split out as **[[PB200]]** (open, analysis) with
three candidate mechanisms and the one that makes the next case automatic identified — a suggestion left inside a
landed note is exactly the paragraph rule 8 says no work list can see.

**The differential's one flip, attributed and intended.** `syn_misc:5699` moved `WE_REJECT_THEY_ACCEPT →
AGREE_ACCEPT`: the same PB164 wave widened `ForceStringCanonical`, the deliberately-stricter EXTERNAL/BASED/
ADDRESS-OF second gate, so the old `COBOLNET0899` "EXTERNAL record cannot be cell-backed" rejection of that
case is gone. A divergence→AGREE flip is a FIX. `tests/external/gnucobol-verdict-baseline.tsv` is regenerated in
this commit from **the gated run's own report** rather than a fresh re-measurement — the baseline must encode the
verdicts the gate READ — and the regeneration changes exactly that one row: totals 572/474/206/71 →
571/475/206/71, no other row moved, no NEW or REMOVED case.

**The re-run, whole, on the fixed tree** (artifacts `scratchpad/battery-38-green`):

```
conformance:     Passed!  - Failed: 0, Passed: 5125, Skipped: 0, Total: 5125, Duration: 11 m 36 s
unit:            Passed!  - Failed: 0, Passed: 5064, Skipped: 0, Total: 5064, Duration: 3 m 6 s
characterization: Passed! - Failed: 0, Passed:   33, Skipped: 0, Total:   33, Duration: 2 s
guard NIST:      === NIST: 353 MATCH, 0 REGRESSION(S) ===
guard audit:     === NIST AUDIT: CLEAN — every declared program produced exactly the verdict the manifest predicts ===
guard verdict:   === ALL GREEN ===
gnucobol:        cases run: 1323   (571 / 475 / 206 / 71)
gnucobol diff:   === DIFFERENTIAL: 1 PER-CASE FLIP(S) ===
                   FLIP  syn_misc:5699  WE_REJECT_THEY_ACCEPT -> AGREE_ACCEPT
```

Two details in there are worth a sentence each. **Characterization ran CONCURRENTLY and passed 33/33** — the
"Test host process crashed" that forced #37's serial re-run has not recurred, so the watch stays open rather
than being closed on one clean run; a second occurrence still earns a register note. And the **Conformance
population is +16 over #37** (5109 → 5125): fifteen from the DEVLOG 1406–1411 commits, and exactly one from
this landing's new byte-true carry fact. Unit is +32 (5032 → 5064).

(Housekeeping note for a future reader: the stamps on entries 1408–1411 run ahead of their own commit times by
30–150 minutes, while this entry's stamp is the real `date` output. The entry NUMBERS are the reliable order.)

