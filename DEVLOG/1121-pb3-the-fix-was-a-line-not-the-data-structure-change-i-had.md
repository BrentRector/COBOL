## Entry 1121 - 2026-07-30 00:05 PDT - PB3: the fix was a line, not the data-structure change I had predicted — and the fabricated citation was in src/ too

**PB3 CLOSED.** `FUNCTION ORD` under a custom PROGRAM COLLATING SEQUENCE now returns the §15.70.4 r1 ordinal
position for EVERY character, including one past the 256-entry alphanumeric weights table. Measured after the
fix, all six spec-derived: `ORD("A")` = `ORD("B")` = 1 · `ORD("C")` = 67 · `ORD(X"FF")` = 255 ·
**`ORD(U+0100)` = 256** · `ORD(U+0101)` = 257. The hole at 256 is gone.

**THE RULE, and it is the one the doc sweep recovered yesterday.** §12.3.7.4 GR7 1.3: "Any characters of the
native collating sequence that are not specified in the literal phrase shall assume a position in the collating
sequence that is greater than that of the highest character specified in this literal phrase. The relative order
within the set of these unspecified characters is unchanged from the native collating sequence." The old
`c < weights.Length ? weights[c] + 1 : c + 1` abandoned the collating sequence entirely past the table, numbering
a character by a different rule than its neighbour.

**⚠ AND THE FIX WAS A LINE, NOT THE DATA-STRUCTURE CHANGE I WROTE INTO THE QUEUE.** That entry said the repair
"is a collating structure that spans the repertoire rather than a 256-entry array, so it is a data-structure
change, not a one-liner." Wrong, in the cheap direction. The dense 256-entry array is fine: the table's own
maximum position is all the arithmetic needs — walk it once for the highest tabulated position, then place each
code unit past the table one position further on. `NationalCollation.Weight` has computed exactly this on the
national side all along (`nextFree + (c − |specified below c|)`), so this was one rule with two implementations
and only one of them incomplete. **I am recording the wrong estimate because a scope guess that reads as
authoritative is how a fix gets deferred for being "big"** — and this one had already been characterised as
CA26's un-finished business rather than a line of arithmetic.

**⛔ THE FABRICATED CITATION WAS IN `src/` TOO, AND YESTERDAY'S SWEEP MISSED IT.** `audit_doc_citations.py`
scans `docs/`. Going into `CobolIntrinsics.Text.cs` to fix ORD, the neighbouring `NationalCollation` doc comment
read "ISO §12.3.7 GR7 k3 — never a shared bucket". Thirteen more instances across five files —
`CollatingModel.cs`, `DataBinder.Switches.cs`, `ProgramEmitter.cs`, `CobolString.cs`, `NationalCollation.cs` —
including the comments on the one implementation that gets the rule RIGHT. All corrected to §12.3.7.4 GR7 1.3.

The lesson is about the instrument, not the citation: **a doc audit that scans only `docs/` measures half the
prose.** Source doc-comments carry citations at the same density and with the same authority — the DESIGN doc and
the XML comment above the implementing method are read by the same person for the same reason. The deliberate
QUOTATIONS of the fabrication (the queue entries that exist to explain it) are left intact, which is why the
grep still finds five.

**GOLDEN** `pb3_ord_collating_tail` — the full derivation for `ALPHABET AL IS "A" ALSO "B"`: {A,B} at 1 ·
0x00–0x40 at 2–66 · 0x43–0xFF at 67–255 · U+0100 at 256 · U+0101 at 257. Every value from GR7 1.3 and 1.6, none
observed first.

**GATES.** Conformance **4144/4144**, zero skipped, nothing red. Unit **963/963**. Characterization **33/33**.
GAP **3774 → 3773**.

