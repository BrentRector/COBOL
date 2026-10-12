## Entry 682 — 2026-07-08 03:12 PDT — Bind-time gating migration Cluster 3 — INVOKE + DELETE FILE (first genuinely-new binder Checks)

Third cluster. Two statement verbs whose keyword is hard-reserved at all editions, so ungating is
tokenization-neutral. INVOKE (§14.9.23, 2002): ungated `CobolParserCore.g4:716`, added
`ConstructRegistry.Check(…, Constructs.Invoke2002, "the INVOKE statement")` as the first line of `OoBindInvoke`
(`StatementBinder.Oo.cs`); the distinct 2023 inline-method form `x(...)` (`inlineMethodInvocationStatement`) stays
gated. DELETE FILE (§14.9.10 Format 2, 2023): ungated `CobolParserCore.g4:679`, added the Check as the first line of
`KeyedBindDeleteFile` (`StatementBinder.KeyedIo.cs`). Retired the INVOKE + DELETE-FILE `EditionGateHints` arms.

**AMBIGUITY verified empirically + by the guard:** ungating `deleteFileStatement` leaves two DELETE-leading
alternatives — `deleteStatement` (`DELETE file RECORD`, unconditional, listed first) and `deleteFileStatement`
(`DELETE FILE file …`). They disjoin on the SECOND token (`FILE` is a reserved token, ∉ `cobolWord`), so ANTLR
ALL(*) routes `DELETE FILE …` only to `deleteFileStatement`; a plain `DELETE F1 RECORD` at 85 still parses clean
(checked). **Beyond-recipe fix (caught by verify-by-building):** `StatementBinder.Oo.cs` lacked `using
CobolNet.Editions;` (unlike Call.cs/KeyedIo.cs) → CS0103 on the first build; added the using. Verified: INVOKE →
COBOLNET0900 at 85, DELETE FILE → COBOLNET0900 at 2014, both compile/gate-past at their edition; DELETE-record
disambiguation intact. Battery: conformance 2055 · unit 224 · FULL legacy guard NIST 353 MATCH.

