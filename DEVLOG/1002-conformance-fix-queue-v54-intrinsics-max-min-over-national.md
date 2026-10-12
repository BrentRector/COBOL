## Entry 1002 — 2026-07-23 01:15 PDT — Conformance fix-queue: V54 (intrinsics) — MAX/MIN over national arguments returns a NATIONAL result, not Alphanumeric

Second intrinsics fix. `IntrinsicBinder`'s MAX/MIN string block did `if (sig.Name is "MAX" or "MIN") category =
PicCategory.Alphanumeric;` UNCONDITIONALLY, even for all-national arguments — but §15.59.1 (MAX) / §15.63.1 (MIN)
result-type tables map a national argument to a National function type. The mis-label bypassed the §14.9.25.4 Table-16
National→Alphanumeric MOVE guard (a `MOVE FUNCTION MAX(n1 n2) TO alphanumeric` wrongly compiled) and fed the wrong
class to comparison collation. FIX: the category now follows the arguments — `args.All(OperandCategory is National) ?
National : Alphanumeric`. The selected national UTF-16 string flows unchanged through the string channel (MaxString
selects, doesn't transform), so only the category label changes.

**Verified (CLI):** `MOVE FUNCTION MAX(N1 N2) TO A` (national → PIC X) now REJECTS COBOLNET0819 (was accepted);
`MOVE FUNCTION MAX(N1 N2) TO ND` (national → PIC N) + DISPLAY-OF → "XYZ". Negative golden
`tests/conformance/negative/max_national_result_to_an` (reject-at 2002/2014/2023).

**Gate (wave-local FILTERED):** characterization 33/33, CorpusRunner + Intrinsic + National conformance 540/540
(includes the new negative fixture). Full Conformance runs once for the whole intrinsics batch.

