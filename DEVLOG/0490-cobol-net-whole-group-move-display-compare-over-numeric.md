## Entry 490 — COBOL.NET: whole-group MOVE/DISPLAY/compare over numeric-DISPLAY leaves (the CCVS TEST-RESULTS case), no byte[]

The first real NC101A blocker, resolved spec-first. NC101A's whole-group MOVEs (e.g. `MOVE SPACE TO TEST-RESULTS`,
`MOVE "MPY-TEST-F1-1 " TO PAR-NAME`) act on groups that mix alphanumeric leaves with a numeric-DISPLAY leaf
(`DOTVALUE PIC 99`). G6-core's `AsImage`/`FromImage` only handled all-alphanumeric groups; a numeric-DISPLAY leaf
routed to the loud "Tier-C byte path" guard. But these are NOT byte-puns — they are all USAGE DISPLAY.

**The subtlety (caught before writing code, confirmed against `tests/nist/valid/NC101A.txt`):** a native `long`
DOTVALUE that `ParseDisplay("  ")→0` then `FormatDisplay→"00"` would inject "00" mid-line where the expected output
has spaces (line 16: `… PASS  MPY-TEST-F1-1` then spaces). **ISO/IEC 1989:2023 §14.9 MOVE general rule 4 (spec line
28901) governs:** a non-elementary move "is treated exactly as if it were an alphanumeric to alphanumeric elementary
move, except that there is no conversion … the receiving area will be filled without consideration for the individual
elementary or group items." So a whole-group MOVE is a pure character copy; a numeric-DISPLAY subordinate can hold
spaces, and numeric use of it is then incompatible data (§14.6.13.2, undefined — line 28951). A native `long` cannot
represent that — the spec dictates the leaf must preserve its CHARACTER IMAGE.

**Resolution (spec-grounded, design-faithful §4 Tier-B / §3.3 Place, NO byte[]):** a numeric USAGE-DISPLAY leaf that
lives under a group used as a *whole* operand is stored as its character image (a C# `string`, `DataItem.StoreAsImage`);
its numeric *use* decodes via `CobolNum.ParseDisplay` and writes via `FormatDisplay`. A leaf never referenced as part
of a whole group stays a native `long` (locked invariant #2). A COMP/COMP-3/COMP-5/float leaf still makes its group a
genuine mixed-usage byte-island (Tier-C, deferred/loud). Touch points:
- **Runtime** `CobolNum.ParseDisplay(image, profile)` — the inverse of `FormatDisplay`: decode over-punch / separate
  sign / leading-minus + digits → unscaled `long`; non-digit chars contribute nothing (deterministic 0, conformant
  per §14.6.13.2).
- **Bind-time whole-group pass** — `ReferenceResolver` records every resolved group reference (a group name can only
  be used as a whole) into `DataBinder.WholeGroupReferenced`; `CSharpEmitter.MarkStoreAsImage` then flags numeric-
  DISPLAY descendant leaves (skipping OCCURS — outside the no-OCCURS char-image model).
- **Data model** — `DataItem.StoreAsImage` (→ `string` field); `IsAllAlphanumeric` generalized to `IsCharacterImage`
  (a leaf is char-image if string-category OR a StoreAsImage numeric); `ImageWidth` now counts a separate-sign char.
- **Emit threading** — `FieldEmitter` (string field, image initializer via `FormatDisplay`; `AsImage`/`FromImage`
  treat the leaf as a string), `OperandText` (image read returns the field directly), `NumericRenderer.FieldNum`
  (numeric read wraps in `ParseDisplay`), and the MOVE/arith store paths (`ConvertSource`/`StoreArith` wrap a
  numeric store into a StoreAsImage receiver in `FormatDisplay`).

8 new differential tests (`GroupNumericLeafDifferentialTests`): spaces-in-numeric-position (the CCVS DOTVALUE case,
spec-pinned), value-formats-in-image, `ParseDisplay` round-trip, group→group, signed over-punch, nested group, and a
not-whole-referenced leaf stays native. Conformance **148 → 156 green**, zero regressions; unit 3 green. Deep-dives
updated (DATA_MODEL whole-group edge case + open-question; SSOT §18 #21) with the spec citation, per the keep-docs-
current rule. This was the larger half of NC101A's 66 "mixed-usage group MOVE" loud guards — most were this
DISPLAY-homogeneous case, not a real byte-pun (the resume-prompt's "Tier-C byte island" framing was imprecise).

