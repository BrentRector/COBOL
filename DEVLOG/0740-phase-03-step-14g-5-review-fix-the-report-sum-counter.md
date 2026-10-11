## Entry 740 — 2026-07-10 12:28 PDT — PHASE-03 Step 14g.5 review-fix: the report SUM-counter PICTURE-skeleton 0900 was dropped (a third Analyze site off GateData)

An adversarial find→verify review of the 14g.5 commit (`6dd27247`, one subagent, H1–H5) found **1 confirmed
byte-neutrality DROP** — the same "an Analyze site whose PicInfo isn't on a gated item" class the SkeletonGate design
had to satisfy. Fixed at the root cause + a regression witness. (H2 over-fire, H3 FUNCTION-PROTOTYPE count, H4 REPOSITORY
scope, H5 dead-code/split — all refuted; the method-scope sub-hypothesis was refuted too — method roots land in `Roots`
via `BindEntries`.)

**The finding.** There are exactly THREE production `PicInfo.Analyze` callers: the data-description entry (→ a `DataItem`
in the forest), the report PRINTABLE item (→ `field.PrintItem`), and — the gap — the **report SUM counter**
(`DataBinder.Reports.cs:BindSumClause`), which analyzes the counter PICTURE only to derive its scale (GR1) and DISCARDS
the `PicInfo`. `GateData` walks the forest + printable items, never SUM counters — so an external-float / national-edited
SUM-counter picture's 0900, which the former inline `NotImplementedSkeleton` fired, is silently DROPPED below 2002. A
non-printable SUM counter (SUM without COLUMN) drops from 1→0; a printable one (also a print item) from 2→1. Nonsensical
input (a SUM counter's picture is fixed-point numeric per §13.18.54.4 GR1), but a genuine break of the "identical for
every program" invariant on an error path.

**The fix.** `ReportSumModel` gains `SkeletonGate` (+ the exact `SkeletonWhere`); `BindSumClause` stamps them from the
scale-derivation `Analyze` instead of discarding; `GateData` walks `report.Sums` and fires `Check(sum.SkeletonGate,
sum.SkeletonWhere)`. This reproduces the former inline behavior EXACTLY — once per SUM counter, so a non-printable SUM
gets its 0900 and a printable SUM (gated once above as a print item) gets both, byte-identical to the two former Analyze
sites. New `RepositoryPrototypeEditionTests.ReportSumExternalFloat_At85_ExactlyOne0900`.

Byte-neutral for real reports: their SUM counters are numeric (SkeletonGate null → the walk fires nothing) —
characterization 32 byte-exact, INV-1-strong 349/349 (the RW golden reports unchanged). Battery: greenfield conformance
**3157** (+1) · unit **223** · characterization **32** · INV-1-strong 349/349. Greenfield-only change (no `.g4`) — CI runs
the full legacy guard as backstop. Step 14g.5 is now review-clean; RESUME AT Step 15 (PHASE-03 close).

