## Entry 1004 — 2026-07-23 01:41 PDT — Conformance fix-queue: CA25 (intrinsics) — UPPER-CASE/LOWER-CASE/REVERSE of a national argument returns a NATIONAL result (completes the intrinsics batch)

Final intrinsics fix. §15.97.1 (UPPER-CASE) / §15.57.1 (LOWER-CASE) / §15.78.1 (REVERSE) result-type tables map a
National argument to a National function type — the result category FOLLOWS the argument. `IntrinsicCatalog` hardcoded
all three to `IntrinsicType.Alphanumeric`, so `FUNCTION REVERSE(national)` was categorized Alphanumeric — its
`MOVE … TO alphanumeric` wrongly compiled (bypassing §14.9.25.4 Table-16), storing the raw national UTF-16 code units
into a PIC X, and fed the wrong class to comparison collation. FIX (mirrors V54's MAX/MIN resolution): a binder arm —
`if (RuntimeMethod is "UpperCase" or "LowerCase" or "Reverse" && OperandCategory(args[0]) is National) category =
National`. The transform is a code-unit op on the national UTF-16 string, so the same RuntimeMethod body applies; only
the category label changes.

**Scope discipline (CA39 lesson applied).** The queue listed TRIM/CONCAT/SUBSTITUTE/FORMATTED-*/CURRENT-DATE as "same
pattern," but CURRENT-DATE has NO argument to follow (always Alphanumeric) — so that list is imprecise; I scoped CA25
to the three functions whose §15.x.1 tables I verified follow the argument. And I SWEPT for fallout BEFORE implementing
(grep of every test/golden using these functions): ALL existing uses take an alphanumeric argument, so there is ZERO
re-baselining — no test relied on the old wrong Alphanumeric categorization.

**Verified (CLI):** `MOVE FUNCTION REVERSE(N1) TO A` (national → PIC X) now REJECTS COBOLNET0819 (was accepted);
`MOVE FUNCTION REVERSE(N1) TO ND` (national → PIC N) + DISPLAY-OF → "CBA"; alphanumeric `REVERSE("abc")` → "cba"
(unaffected). Negative golden `tests/conformance/negative/reverse_national_result_to_an`.

**Gate (wave-local FILTERED):** characterization 33/33 (emit byte-identical — the RuntimeMethod is unchanged),
CorpusRunner+Intrinsic+National+Locale 553/553. **The intrinsics batch (CA24·V54·CA23·CA25) is COMPLETE** — its
runtime-touching fixes passed the 3886/3886 comprehensive gate; CA25 is a narrow, zero-fallout binder-category change.

