## Entry 1832 — 2026-10-01 12:31 PDT — Train 1001: wave 1001 groups D, C, A and B land (17 notes landed or retired, GAP 710 → 691); PB1527 dropped for contradicting PB244

**Group D (PB1233, PB1665, PB244 legs, PB1466; PB601 retired).** One root, four notes. A CONSTANT RECORD's content is
now seeded by the INITIALIZE recipe: `ValueInitializer.RecipeFor` returns `SeedRecipe.Initialize` for a CONSTANT RECORD
root and both seed roots ask it (`FieldInit` and `GroupImageCodec.ImageInitOf`, the record-struct arm and the image arm),
so a VALUE-less numeric-edited item holds its edited zero and an OPTIONS INITIALIZE fill no longer leaks (§13.18.15.4 GR1;
re-probe `[      ]` → `[  0.00]`). A group's `ImageWidth` was written twice (`DataItem.ImageWidth` and a mirror in
`RecordLayout`); it is now one copy, `RecordLayout.ImageWidth`, in its class's unit (a national group counts positions,
an alphanumeric group storage characters), so `MOVE G5 TO G6` no longer drops the members after a national leaf
(PB1665, `[AB][CD][  ]` → `[AB][CD][EF]`), and RENAMES THROUGH tiling moved to storage bytes. That unblocked PB1466's
national `IS NUMERIC` arm (row GR-8.8.4.4.4-L2.1). STRING and UNSTRING over a strongly-typed group holding a non-display
leaf are now bind errors (`StringUnstringBinder.Sr1Offence`, `Sr2OffendingCategory`; §8.5.2.1 gives the class and
category of a strong group as its type-name). PB601 did not reproduce (fixed by PB392) and is retired. PB244's shape (b),
a variable-length group DISPLAY, stays open, sized on its note. Goldens `pb1233_constant_record_initialize_content`,
`pb1233_constant_record_options_initialize`, `pb1665_group_image_national_leaf`, `pb1466_national_numeric_class_image`,
`pb244_strong_group_equality_and_string`, negatives `pb244-string-strong-group-pointer-leaf` and
`pb244-unstring-strong-group-sender`; drift test `ConstantRecordSeedRecipeDriftTests`. Rows GR-13.18.15.4-1 and
GR-8.8.4.4.4-L2.1 → CONFORMS. Codes: none used (COBOLNET1626 and COBOLNET1651 reused).

**Group C (PB1494, PB1690, PB1492, PB1359, PB1640, PB1758).** The §6.5 logical conversion's missing half: one
`LogicalLineBuilder` for both reference formats replaces the line-for-line free-form conversion. It carries the floating
literal continuation indicator (PB1359, edge 2002 by derivation), continuation-line validation (PB1492), library-text
diagnostics through one per-compilation `ReferenceFormatDiagnostics` shared by main source and copybooks (PB1640), a
named directive-after-program-text error (PB1690), the comment-entry header kept so the removal gate sees fixed form
(PB1494, PB1758), and the `D`-line window of owner decision R61. Re-probe: all five notes reproduced; PB1492's
apostrophe half did not (fixed by PB1491, now pinned by a golden). Self-review found and fixed a sibling: library text
copied in the IDENTIFICATION DIVISION was converted in the wrong division (`DivisionCursor`,
`CopyProcessor.CopyStandsInIdentificationDivision`). Codes COBOLNET2684-2691; 29 goldens and negatives; 12 rows →
CONFORMS plus re-sited witnesses.

**Group A (PB1160, PB1121, PB1122, PB1761; PB1197 and PB1144 leg 2 stay open).** RESUME inside a GLOBAL declarative's
scope of execution is now a CONTINUE (§14.9.33.4 GR1), through the same in-range test GOBACK GR6 uses, and SR2 is asked
before the WHEN-phrase question. The exception-object boundary tests `RAISING LAST` against ONE header census
(`EcState.PdRaisingObjectCsTypes`) and converts an untaken object to the named EC-OO-EXCEPTION through the activator's
checking gate (`ExceptionState.ConvertObjectPropagationToNamed`; §14.6.13.1.5 items 1 and 4). A declarative that sees a
fatal exception in its scope, or a RESUME made a CONTINUE, no longer completes normally (§14.6.13.1.2 1): a run-unit
declarative activation record in `ExceptionEngine`, closed in the emitted `finally`, so a MERGE GIVING bypass becomes a
termination (closes PB1761). PB1122's SORT shape is unreachable today (SORT GIVING indexed is COBOLNET1757, kb/Work
PB994), so its golden uses MERGE. Goldens `pb1160_resume_in_global_scope`, `pb1121_object_propagation_boundary`,
`pb1122_declarative_fatal_in_scope_merge`, negative `pb1160-resume-in-when-in-global-declarative`. Five rows →
CONFORMS. Codes: none used.

**Group B (PB1572, PB1653, PB1781 landed; PB1527 dropped).** `BitLayout.Walk` is the one §8.5.1.6.3 cursor, relative to
the group, and the REDEFINES class walk asks `BitLayout.ChildStarts` instead of running an absolute-seeded third copy
that put a mid-byte bit group's first member at bit 8 (PB1572, `R3V=10000000` → `11100000`). A national group's
REDEFINES view has a value channel (its `NationalWindow` coding) and a storage channel (`ByteWindowRead`/`Write`), and
ACCEPT and CALL follow it (PB1653, `䅂䍄` → `AB`). A fixed group crosses a universal INVOKE as its character image on
both sides through `CallEmitter.CallStringRead`/`Write` (PB1781, a Roslyn CS0029 on legal source). Goldens
`pb1572_bit_group_redefines_offsets`, `pb1653_national_group_view`, `pb1781_universal_group_crossing` with their
below-2002 negatives; nets `BitRunImageDriftTests`, `GroupViewAliasDriftTests`, `UniversalCrossingShapeDriftTests`.
No rows (all three notes carried none). Codes: none used.

**The train.** Order D, C, A, B, each brought in as a patch from its base `b62408c25` (origin/main had not moved). Only
`docs/DRIFT_RULES.md` conflicted (D and B both regenerated it); it was regenerated from the tests (251, then 250 after
the drop). Manifest element counts were checked against the four branches (2002: 579 + 15, negative: 1884 + 21 before
the drop) and every verdict batch dry-runs at zero changed rows on the merged tree. ⛔ **PB1527 was dropped from group
B.** Its fix made STRING and UNSTRING READ a strongly-typed group holding an object reference or pointer, and its
positive golden ran exactly those statements; group D's PB244 legs refuse those statements at bind time, so the two
cannot both hold. The lander kept D's refusal because the spec supports it directly: §8.5.2.1 makes a strong group's
class and category its type-name and gives the usage of display only to an alphanumeric group, so UNSTRING's
§14.9.48.3 SR2 refuses a strong-group sender by construction. B's determination rested on a code fact D changed. The
PB1527 commits (`86e8ebf24`, `4deac4cfd`) were reverted out of B, DOC-A.1-214 stays PARTIAL, and kb/Work PB1527
records what remains (a non-strong group with a pointer leaf through STRING, where B's `AsTransferString` is the shape
to reuse). Gate: `=== BUILD-LOCAL GATE: GREEN — Conformance 9,711/9,711 · Unit 30,184/30,184 · Characterization 35/35
cases ran (skipped 0) in 1 of 1 leg(s) · lander mode` (run 20261001T192524Z-5ef6e6); legacy Integration 503 passed,
1 skipped; semgrep unchanged (BigInteger 46, decimal 2, raw-code 294, rendered-text 3). GAP 710 → 691. New leads
filed: PB1901 (class-fixed intrinsic argument over a strong group, from D), PB1902 (RENAMES odd-byte national
boundary, from D), PB1903 (CopyProcessor raw COBOLNET0902 literals, from C), PB1904 (bit-group view storage image,
suspected, from B). The other leads already have owners: PB244 shape (b), PB1802/PB1705, PB1360, PB1197, PB1144,
PB994 (SORT/MERGE keyed files), PB480 (universal INVOKE group prefix).

