## Entry 1756 — 2026-09-28 12:21 PDT — Train 70: the standard-float sign partition and a type-6 constant fold, Int128 report counters with a float CONTROL, and fixed form as the default reference format

Three wave-70 clusters landed as one train, in manifest order: A (PB1471 + PB617, PB954 discharged, PB1466
re-probed and blocked), V3 (PB1509 + PB1560 + PB1666 + PB1305 + PB1234) and B (PB1362 + PB1361, PB1494's
indicator-area half). This is the wave the owner approved at ~09:45 PDT ("Okay, approve test impact analysis and
start wave 70"). A and B came back SPLIT, and only what their reports mark complete lands. GAP 896 → 883, 13 rows
closed.

**A — "standard floating-point usage" means the standard set, and a type-6 argument is judged on its value.**
PB1471: the sign condition's Format 1 / Format 2 partition (§8.8.4.7.3 SR1/SR2) asked `PicInfo.IsFloat`, a
STORAGE question, so a bare FLOAT-SHORT / FLOAT-LONG / FLOAT-EXTENDED took Format 2's sign-bit answer and a
FLOAT-LONG +0 was POSITIVE. SR2 limits Format 2 to "a standard floating-point usage" (§3.166 / §3.167), so
`ConditionBinder.IsFormat2FloatSign` now asks `UsageFamilies.IsStandardFloat`, and GR1's algebraic test answers the
implementor floats. The sibling sweep found `ClassConditionModel` (SR7) and `ConditionRenderer.IsStandardFloat`
right but spelling the or-chain twice; both now call the one predicate. The CA8 golden had pinned the wrong answer
on FLOAT-LONG; it is rewritten from GR1 (and CODE-SPEC-AUDIT's CA8 premise corrected), and
`2014/pb1471_sign_condition_float_partition` pins GR2's sign bit on FLOAT-BINARY-64/-32, including −Inf and ±NaN.
PB617: the §15.3 type-6 screen gains its third witness. The additive spine's literal terms fold EXACTLY (a rational
over BigInteger — the algebraic value, never a mode-rounded intermediate, a determination recorded in the code), and
when every other term is provably integral a non-integral constant refuses the argument: `CHAR(1.5 + 1)` and
`CHAR(W-I + 0.5)` are COBOLNET1627, while `CHAR(1.5 + 0.5)` is admitted. Anything the fold cannot value fails open.
PB954 did not reproduce: its mechanism is the one PB960 fixed (8f05718c3), so it is discharged with
`closes_rows: []` and its reason. PB1466's NATIONAL arm reproduces and waits on PB1665 (the group image counts
national positions as byte pairs; re-measured open on this build). Rows SR-8.8.4.7.3-1/-2, GR-8.8.4.7.4-1/-2 and
AR-15.3-6 CONFORMS. Self-review found nothing beyond a numeric-category guard it added to `ProvablyIntegral`.

**V3 — the report engine's counters were a C# `long`.** PB1509 / PB1560 / PB1666: SUM counters now ride Int128 end
to end. `CobolReport.SumEntry`, the addend delegate, `SumValue` and `SetSumValue` are Int128, a term's addends are
summed `checked`, the capacity is 10^digits − 1 with GR1's PICTURE digits up to 38 (`PicInfo.SumCounterItem`'s
clamp 18 → 38), and an overflow while forming the addend is GR3's size error (§13.18.54.4 3), ADD … ON SIZE ERROR
keeps the counter). 20- and 31-digit totals print exactly under NATIVE and STANDARD-DECIMAL (§11.9.5.2 GR1/GR3)
where they printed spaces. The re-probe did NOT confirm PB1560's PAGE-COUNTER arm: PAGE-COUNTER is the pinned
PIC 9(18) (CONFORMANCE.md R26), so that note and PB1666 close with `closes_rows: []` and a reason. PB1305: VARYING
counters are Int128 compose locals, and each FROM/BY value lands through `CobolReport.VaryingInteger`, which raises
EC-REPORT-VARYING (§13.18.64.4 GR5 — a new checking flag, an EcEmitter gate row and a ReportProductionNames entry);
`FROM 2 * 3 - 5` had been a CS0266 backend crash. Determination: with checking off the counter takes the integer
part (GR5 leaves the print line undefined). PB1234: a floating-point CONTROL item died at activation for want of a
restore channel. It now saves its bit pattern, restores through its own encoding (GR4 a), a same-usage transfer)
and breaks on a value change (−0 equals +0; identical NaN bits are equal). Five goldens and three negatives (reject-at
85). Rows GR-11.9.5.2-1/-3, FMT-13.18.64.2, GR-13.18.64.4-5 and GR-13.18.16.4-4 CONFORMS; GR-13.18.64.4-3 restated
DIVERGES, its remaining arms held by PB1306. Its self-review found three new defects (below).

**B — a compilation group starts in fixed form.** PB1362: §7.3.24.3 2) says "The default reference format of a
compilation group is fixed form", and the compiler auto-detected it with a heuristic that reads conforming fixed-form
source (a non-numeric sequence area, text past margin R) as free form. `InitialReferenceFormat` {Fixed, Free, Auto}
has one option reader and is threaded from the CLI's new `--source-format` through `CompilerDriver.Options` to
`Frontend.InitialFormat`. The default is FIXED at every entry point; auto survives only as the documented §4.2.10 3)
selection (DOC-A.1-158, following GnuCOBOL). The implementer measured the population first: 2,820 of 3,982 test sources
were detected free, and flipping the default alone turned 381 Unit tests red. Every non-NIST test compile site
(96 files) therefore selects Auto, and the GnuCOBOL differential passes `--source-format auto`. ⚠ This is a
USER-VISIBLE change: a free-form program with no `>>SOURCE FORMAT FREE` now needs `--source-format free`. PB1361:
`ReferenceFormatProcessor.FormatSegments` walks the lines in order, carrying the format in effect as state (§6.5), and
recognizes a directive only in the program-text area of that format (§7.3.3 SR2/SR3), or on the first line in either
form (§7.3.24.3 4)). This replaces the format-blind pre-scan and the sequence-area allowance, leaving one directive
reading. PB1494 (partial): `KindOf` is the one indicator classifier. The CCVS column-7 letters are honored only
under `--nist`; otherwise a character that is not a fixed indicator is COBOLNET2616 (new). Comment-entries are
scoped to the IDENTIFICATION DIVISION. PB1494 stays open for its edition scope, and PB1492 and PB1359 were not started
(turn cap). Rows GR-7.3.24.3-2, SR-7.3.3-2, GR-7.3.24.3-4 and GR-6.5-1 CONFORMS, and GR-6.5-5 moves from
DIVERGES to PARTIAL. The implementer's last full gate was red on four `StorageFormNistEquivalenceTests` cases (NIST
programs parsed without `--nist`). It fixed them and re-ran that class 13/13, but did not re-run the full gate before
its turn cap. The train's whole-assembly gate is the full re-run, and it is green.

**The train.** The patches from the three branches applied cleanly except for the 85 and negative corpus
manifests, where each hunk was a whole-element add; both sides were kept, and the counts were checked (85: 306 + 1 + 1 = 308;
negative: 1731 + 1 + 3 + 2 = 1737; no duplicates). The inventory was re-derived from the seven verdict batches on
the base inventory and was byte-identical to the merged file. Gate, in this worktree at Normal priority:
`pwsh scripts/build-local.ps1 -Filter "~."` (the WHOLE Conformance assembly). Verdicts: `Passed! - Failed: 0,
Passed: 9289 … Cobol.Net.Tests.Conformance.dll`, `Passed! - Failed: 0, Passed: 29728 … Cobol.Net.Tests.Unit.dll`,
`Passed! - Failed: 0, Passed: 33 … Characterization.dll` and `=== WAVE-LOCAL GATE: GREEN (filter
FullyQualifiedName~.) ===`. Legacy assemblies: Integration 503 passed, 1 skipped; legacy Unit 1200 passed. The
external corpus was fetched, sha256-verified. Semgrep verify PASS, with no count rising (BigInteger 46 → 46). 11
citations were re-run through `cite.py --check`, all OK. Main moved twice, both times with tooling and docs only: 55ef23eb9 (DEVLOG 1754) during the gate, and
a7c484f1b (DEVLOG 1755) while CI verified the train. CI run 36469331688 was green on b9998bc10, but the
fast-forward was refused. The train was rebased onto each move without re-gating (L3); the only conflict was this
DEVLOG entry, which was renumbered. Review: one pass
over the train diff, with no confirmed correctness finding and no cluster dropped. New leads filed: PB1684
(EC-DATA-NOT-FINITE raised for implementor floats — PB1471's root at another site), PB1685 (a numeric-edited SUM
entry loses its scale), PB1686 (SUM truncates each addend before adding), PB1687 (report pictures not digit-gated at
85), PB1688 (fixed-form diagnostic columns off by 7), PB1689 (REMARKS lexed as a keyword outside the IDENTIFICATION
DIVISION) and PB1690 (a §7.3.3 2) violation reported only as COBOL0001). A fourth B lead, that the GnuCOBOL
differential should mirror each case's cobc format flag rather than run everything under auto, is returned to the
orchestrator because the id allocation ran out. Diagnostic codes: COBOLNET2616 was claimed; 2608–2615 and 2617–2619
were returned unused.

