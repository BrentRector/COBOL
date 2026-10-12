## Entry 1742 — 2026-09-27 14:17 PDT — Train 66: a TYPE subject takes its template's implied PICTURE and BASED, GROUP INDICATE per detail group, INSPECT stores a group in its own alphabet, a ref-modded RENAMES alias meets the one tail

Four wave-66 clusters landed as one train: G (PB1475 + PB1300), J (PB1244 + PB1245), K (PB1128 + PB1126 + PB908)
and L (PB1380). GAP 1047 → 1035, with 12 rows closed.

**G — PB1300: SR9's implied PICTURE ran after the TYPE copy, so the subject never inherited it.** The re-probe
reproduced PB1300. `01 W TYPE U VALUE "Q"` over `01 U TYPEDEF VALUE "ABCD"` bound W as X(1) where §13.18.57.4 GR3
("that implicit PICTURE clause becomes part of the description of the subject of the entry") makes it X(4). A
SAME AS of a numeric item under a `USAGE NATIONAL` group inside a `GROUP-USAGE NATIONAL` group took the national
USAGE and was 6 bytes instead of 3. `SynthesizeImpliedPictures` now runs FIRST in `BindPipeline`, over the written
entries, so a template owns its implied PICTURE before the one description copy hands it on; the dead `TypeAnchor`
filter is deleted and `DataItem.PicIsValueImplied` carries the provenance. BASED is now an `EntryOnly` copy fact, so
a TYPE / SAME AS subject of a based description is based, and the new `ScreenComposedBased` (COBOLNET2510) holds the
composed clause to §13.16.3 SR16/SR5/SR13 through the same `BasedLevelAdmitted` predicate the written arm uses. A
TYPE subject's own VALUE is now screened against the composed PICTURE (`01 M TYPE N VALUE 12345` over PIC 9(3) used
to store 345 silently). SAME AS takes USAGE only from the group kinds §13.18.49.4 GR3 names and SIGN only from those
GR5 names, and `ItemCategory.GroupKindsOf` reads the implied GROUP-USAGE pull-style so it answers the same before
the usage walk. ⚠ DETERMINATION: §13.18.63.3 SR4 bounds a VALUE literal by "the size indicated by an explicit
PICTURE clause", so a TYPE subject's longer literal over a VALUE-implied template breaks no syntax rule. PB1475 did
NOT reproduce: PB1166 (`a38622ede`) had already fixed the DPC and locale-identification arms; this cluster adds its
goldens, corrects the §8.5.3.1 doc comment and moves GR-8.5.3.1-2 to CONFORMS. Goldens:
2002/w66g_pb1300_type_same_as_composition and 2002/w66g_pb1475_strong_type_locale_identification. Negatives:
w66g-pb1300-composed-based-level (COBOLNET2510) and w66g-pb1475-strong-type-dpc-differs (COBOLNET1688). Unit:
`TypeSameAsCompositionTests` (5). Four rows closed. The self-review noted that SR13 (CONSTANT RECORD with a TYPE of
a based typedef) has no test.

**J — PB1244 + PB1245: GROUP INDICATE was one report-wide flag plus a post-compose blanking mask.** The re-probe
reproduced both notes. A second detail group's first GENERATE lost its indicated item. A non-indicated item on
another line of the same group was blanked on repeat. A GROUP INDICATE item with a relative COLUMN was refused with
COBOLNET0899. GROUP INDICATE in a CONTROL HEADING, or on an item with no COLUMN, compiled. The clause is now what
§13.18.28.4 GR1 says: a PRESENT WHEN on the item whose condition is per DETAIL group
(`ReportGroup.GroupIndicatePending`), armed at INITIATE, page advance and control break, consumed by that group's
own GENERATE, and ANDed by `ReportWriterEmitter` into the item's one presence test beside PRESENT WHEN and OCCURS
DEPENDING. An absent item places nothing and moves no horizontal counter, so a relative COLUMN needs nothing of its
own and `report-indicate-relative-column` is retired. `_indicateFresh`, `IndicateFields` and the blanking in
`PresentLine` are deleted. §13.18.28.3 SR1 is screened (COBOLNET2519), and every §13.18.29 citation of the rule is
corrected to §13.18.28. One claim did NOT hold: PB1244 expected `BBBBB  AAAAA` for a COLUMN PLUS 2 item, but
§13.18.14.4 GR8 places it at the horizontal counter plus 2, one blank, and the fixed build prints `BBBBB AAAAA`.
⚠ DETERMINATION: a GENERATE whose detail is SUPPRESSed or wholly absent still consumes the condition, because GR1
counts "the first occasion that a GENERATE is issued"; no golden pins it yet (PB1652). Golden
2002/pb1244_group_indicate_per_detail_group and three pb1245 negatives. GR-13.18.28.4-1 and SR-13.18.28.3-1 are
now CONFORMS.

**K — PB1126 landed, PB1128 half, PB908 discharged: INSPECT stored a national group through the byte writer.** The
re-probe reproduced every PB1128 and PB1126 arm. A national group CONVERTING N"A" TO N"Z" came back as mojibake. N" "
beside a PIC ZZ9 USAGE NATIONAL item was refused while " " was accepted. A zero-length identifier-1 re-encoded its
counter. `TO ALL "Q"` was refused. A figurative replacement was sized at bind time against a pattern whose size is
known only at run time. EC-RANGE-INSPECT-SIZE was catalogued but never raised. Now `PlaceRenderer.WriteGroupValue`
is the ONE group VALUE writer, the receiving twin of `SendingGroupValue`, and STRING's group receiver (the same
defect) uses it too. `CobolClass.NumericEditedNational` makes a national numeric-edited item class national
(§8.5.2.1 Table 2), and `AllOrNothingClass` compares Table-2 classes. A zero-length identifier-1 skips the
statement (GR2). §14.9.22.3 SR3 refuses every ALL figurative at literal-1 to literal-4 and admits one at literal-5.
The runtime sizes a figurative literal-3 / literal-5 against its pattern and raises EC-RANGE-INSPECT-SIZE for
GR14/GR15/GR22; `InspectStaticWidth` and the bind-time expansion are deleted. PB908 no longer reproduced
(`66fcfda30`). GR4 d)'s negative zero stays open on PB1128: a native signed DISPLAY item has no -0, which is a
data-model question. Goldens 2002/pb1128_inspect_identifier1_class_store and 2002/pb1126_inspect_size_ec, two
negatives, and the regenerated `char_string_ops` snapshot. Five rows closed; GR-14.9.22.4-4 re-noted DIVERGES for
GR4 d) only.

**L — PB1380: a reference-modified RENAMES alias bound the WHOLE alias.** `ResolveImplCore` composed the alias in an
arm that returned before the §8.4.3.3.3 SR1 screen and the §8.4.3.3.4 GR5 view. So `MOVE RN(2:3)` moved all six
characters, and `RN(2:N)` with N = 0 overwrote the span instead of raising EC-BOUND-REF-MOD (§7.3.23.3 GR1). The
re-probe found it wider than the note: the no-THROUGH alias dropped its modifier the same way, and `RB(1:2)` over a
COMP item compiled clean. The composition moved into `PlaceForItem`, the one item-to-place builder
(`PlaceForRenames`, `SpanLeafPlace`), so every named item meets one tail and the by-item entries build a level-66
place too. SR1 and the view read the place's item. `PlaceGap` keeps COBOLNET2364 naming a refused entry behind an
alias. `RenamesPlaceBuilderDriftTests` pins the one builder. The inherited citation "§8.4.3.3.2 SR1" was wrong
(`cite.py` FAIL); it is §8.4.3.3.3 SR1. Goldens 85/pb1380_renames_ref_mod and 2023/pb1380_renames_ref_mod_zero_length,
negative pb1380-renames-alias-binary-ref-mod (COBOLNET1647). GR-7.3.23.3-1 is now CONFORMS.

**The train.** Every cluster came in by `git apply -3` from its branch diff, with the inventory hunk discarded and
each implementer's `record_verdicts` batch re-applied on the merged tree, in order (G 1047 → 1043, J → 1041, K →
1036, L → 1035). The conflicts were whole elements of the 2002, 85 and negative golden manifests (G, K and L each
against train 65 or an earlier cluster); both sides were kept and the element counts checked (2002: 526 → 531;
negative: 1684 → 1692; 85: 285 → 286; 2023: 666 → 667; no duplicates). `docs/DIAGNOSTICS.md` and
`docs/DRIFT_RULES.md` were regenerated. No conflict markers in the tree or the index after any cluster. The gate is
ONE build, then the WHOLE Conformance assembly unfiltered (filter `~CobolNet.Tests.Conformance`, which selects all
9119): `Passed! - Failed: 0, Passed: 9119` in 10 m 58 s. Unit: `Passed: 29567`. Characterization: `Passed: 33`.
The legacy integration assembly: `Passed: 503, Skipped: 1`. The external GnuCOBOL corpus was fetched into the fresh
worktree without failure. Semgrep verify: PASS, every count equal to the baseline. The CI audits pass locally: code
citations 0, doc citations 0, evidence supersession 0, witness loss GREEN (0 unexcused, 6 retired, 16 re-sited),
drift-rules index current, `work.py check` clean. For the review pass I read every source diff, checked the drift
rules of the changed files, scanned the new tests for wall-clock limits (none) and ran 15 citations through
`cite.py --check` (all OK). The review found 0 correctness findings, so no cluster was dropped. Leads filed:

- PB1650: PROPERTY composed with a BASED TYPE is not refused (§13.16.3 SR21 a)).
- PB1651: `where.py` crashes on a cp1252 console.
- PB1652: no golden pins GROUP INDICATE's consumption by a suppressed GENERATE.
- PB1653: a national group that REDEFINES reads and writes its BYTE window. This is a WRONG ANSWER.
- PB1654: PB908's sweep of the `.Pic is { }` group-kind stand-ins was never run.
- PB1655: `MOVE ALL "*" TO T(7:3)` stores the literal once. This is a WRONG ANSWER.

The legacy engine's report-wide GROUP INDICATE flag now differs from WiseOwl by design; no note unless a
differential flips. Diagnostic codes claimed: COBOLNET2510 and 2519.

