## Entry 1010 — 2026-07-23 03:37 PDT — Conformance fix-queue: CA6 (arithmetic) — a binary-char/-short/-long/-double operand is EXCLUDED from the composite of operands (ISO §14.7.7 rule 2b); the arithmetic batch is COMPLETE

**CA6 (CONFORMANCE-FIX-QUEUE, MAJOR/S) landed — completing the arithmetic batch (CA4 · CA5 · CA6).** ISO §14.7.7
rule 2b: when an operand is a data item of usage binary-char/-short/-long/-double (or a floating-point usage / an
intrinsic / a float literal), "the composite of all OTHER operands shall not contain more than 31 digits" — i.e. the
four fixed-width binary usages are EXCLUDED from the composite, exactly like a float operand. Those usages carry
Category Numeric + IsFloat false, so `StatementValidation.CheckComposite`'s two Shape guards (which screened only on
`{ Category: Numeric, IsFloat: false }`) wrongly superimposed them — a BINARY-DOUBLE contributes up to 20 integer
digits and pushed a conforming program past the 31-digit cap (a spurious COBOLNET0805).

**The fix.** A `static bool InComposite(PicInfo p)` predicate — `{ Category: Numeric, IsFloat: false }` AND
`Usage is not (BinaryChar or BinaryShort or BinaryLong or BinaryDouble)` — replacing the inline pattern at both the
operand arm and the receiver loop. COMP-5 is NOT in rule 2b's list and stays counted; float usages are already
excluded by IsFloat; intrinsic-function operands already fall through the OfExpr switch. A pure-leniency change (the
composite only gets smaller → fewer rejections), so no strictness sweep.

**Gate.** Conformance Corpus+Arithmetic+Compute+StandardDecimal 403/403 (incl. the new golden) · characterization
33/33 byte-identical (validation-only). Golden `conformance/2002/ca6_binary_operand_composite` (2002+ — BINARY-DOUBLE):
`ADD BL(binary-double) TO F PIC 9(11)V9(13)` → composite = F (24) ≤ 31 → legal, F = 3+5 = 8 →
`000000000080000000000000` (CLI `--run` verified). **20 landed / 26 fix-ready remain.** The arithmetic batch is done;
run the FULL Conformance + GnuCOBOL/NIST differential as the batch gate before the eventual phase-14 → main merge.
NEXT = CA7 (conditions).

