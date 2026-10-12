## Entry 1415 — 2026-08-31 08:35 PDT — Battery #39 stops one step short of green: two new rejections are RIGHT, and the sweep that cleared them read two files out of a corpus of 1,323

**Four legs green, and the fifth is the interesting one.** The comprehensive battery ran on tree `3bc4e4db` —
the accumulated batch of burn-down cluster A (PB173 + PB177 + PB178, DEVLOG 1413) and the PB184 + PB188 seed wave
(DEVLOG 1414). FULL greenfield Conformance **5152 / 5152**, Unit **5091 / 5091**, characterization **33 / 33**
concurrent with no host crash, NIST **353 MATCH / 0 REGRESSION** with a CLEAN audit and the guard's own
`=== ALL GREEN ===`. Then:

```
=== DIFFERENTIAL: 2 PER-CASE FLIP(S) ===
  FLIP  run_subscripts:351   AGREE_ACCEPT -> WE_REJECT_THEY_ACCEPT   error COBOLNET1702
  FLIP  syn_redefines:172    AGREE_ACCEPT -> WE_REJECT_THEY_ACCEPT   error COBOLNET1701
```

1323 cases, no NEW or REMOVED, totals 571/475/206/71 → 573/473/206/71 — consistent with exactly those two and
with no offsetting pair hiding behind them. This is the flip shape the battery's standing orders name as a
STOP rather than a baseline update, and the reason turned out to be better than the rule deserved.

### The compiler is right on both, and that was checked before anything else

`syn_redefines:172` is a two-line shape: an `02` elementary with `OCCURS 2`, redefined by its `02` sibling.
**§13.18.44.3 SR5** sentence 1 — *"The data description entry for data-name-2 shall not contain an OCCURS
clause."* — names it outright. `run_subscripts:351` is an `01` group with a VALUE literal and a subordinate
elementary carrying its own VALUE; **§13.18.63.3 SR13** sentence 2 — *"The VALUE clause shall not be specified at
subordinate levels within this group."* Both clause numbers were re-derived and run through
`scripts/spec/cite.py --check` rather than inherited from the diagnostic text they were about to justify
(CLAUDE.md rule 1). GnuCOBOL merely WARNS on the REDEFINES one, and its own testsuite carries the comment that
the check *"should be a dialect option, currently it is _always_ a warning"* — so `WE_REJECT_THEY_ACCEPT` is the
CONFORMING verdict here and their acceptance is documented latitude on their side.

So both flips are conformance improvements, and the mechanical thing to do would have been to regenerate the
baseline and call the battery green.

### The reason not to: the flips refute the evidence the waves shipped on

Plan §0 carried, as the standing reachability proof for COBOLNET1701's new rejection, *"a corpus-wide mechanical
scan found ZERO programs with a REDEFINES whose target entry carries an OCCURS clause (conformance + NIST +
characterization + **the GnuCOBOL testsuite sources**, 1,448 files)"*.

Measured on the same tree: `tests/conformance` holds 959 `.cob`, NIST 459 sources — and the entire
`tests/external/gnucobol` tree holds **75 files, of which exactly 2 are `.cob`/`.cbl`**. 959 + 459 + 2 = 1,420.
That is where the 1,448 came from. The GnuCOBOL contribution to a sweep named after the GnuCOBOL testsuite was
**two files**. The 1,323 differential programs are not files at all: they live as `AT_DATA` heredocs inside 38
`.at` autotest wrappers that `gnucobol_differential.py` parses and materialises into a temp dir at run time, and
a `find … -name '*.cob'` walks straight past every one of them.

⛔ **The case that fired COBOLNET1701 is titled "REDEFINES: with OCCURS".** A sweep that had reached those sources
at all would have hit it on the keyword alone. The zero was real, it was just a zero about a different
population — [[feedback_measure_the_selectors_complement]] and [[feedback_reachability_is_measured_not_deduced]]
in one instrument. Rewriting the baseline in the same breath as discovering this would have destroyed the only
record that it happened and left the false claim standing in §0 looking confirmed.

So: baseline TSV **unchanged** at 571/475/206/71, battery #38 stays the CURRENT reference, and the false sentence
is withdrawn from §0 with its measurement attached. [[PB209]] is registered with the structural fix — not a wider
`find` glob, but ONE `AT_DATA` extractor shared by the sweep and the differential so a wave cannot prove a shape
absent from a corpus the gate then finds it in, plus a drift test asserting the sweep's external population size
tracks the differential's `cases run:`. Do that, re-run the differential, attribute both flips, regenerate the
baseline in that commit, and #39 records then.

### What the run does prove

Everything except the two rows. The seams a wave-local filter structurally could not reach — `ExpandType` /
`ExpandSameAs` writing a flag every group-VALUE screen reads, `OdoModel.WrapGroup`'s extent unit, the
`CallEmitter` boundary string pair, `IntrinsicArgumentRules.StaticUsageOf` — were all exercised by the NIST and
differential legs and produced no flip. COBOLNET1698, 1699, 1700 and 1703 fired on no differential case, so their
zero-candidate claims survive this measurement; only 1701 and 1702 were contradicted.

**Population arithmetic, both suites reconciled against the #38 TRX by test name, not by count:** Conformance
5125 → **5152**, +28 added / −1 removed = net +27 — 20 new negative fixtures (13 PB177, 6 PB184, 1 PB207), 5 new
2023 goldens (`pb173_bit_group_boundary`, `pb177_method_group_boundary`, `pb178_convert_any_redefines_group`,
`pb184_group_value_area`, `pb188_table_value_image_width`), and 2 net authored methods (one new
`AcceptDifferentialTests` bit-group slice fact; `OccursDynamicGuardTests` split its one dynamic-REDEFINES test
into the object side on COBOLNET1701 and the subject side on COBOLNET1525, which is the −1 and two of the +28).
Unit 5064 → **5091**, +27 / −0 — 26 authored (3 `BoundaryImageChannelTests`, 1 `ControlClauseCitationDriftTests`,
4 `RedefinesClassificationTests`, 7 `ImageWidthIsStorageWidthTests`, 11 `UsageWordDriftTests`) and **one that
nobody wrote**: `ParenTokenTwinDriftTests` enumerates compiler source files as its MemberData, so the new
`Binding/DataBinder.GroupValue.cs` added its own row. Worth knowing that the suite grows when `src/` does.

