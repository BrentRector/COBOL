## Entry 790 — 2026-07-11 14:55 PDT — P7 Step 4a: `FigurativeConstants` — the SIX divergent figurative copies collapse to ONE service

**What (Step 4 first half; DESIGN-codegen-backend §2.5 rationale #6).** `CodeGen/Roslyn/FigurativeConstants.cs`
NEW — `KindOf(word, includeNull)` (the ONE word-recognition table: Z/S/Q/H/L + the NULL/NULLS→L arm where a
site's map historically admitted it) · `FillChar(kind, collate, cat)` (the RAW fill: PCS extremes for H/L unless
the D-N3 national/boolean pin applies; natively ÿ/NUL) · `Fill(kind, collate, cat)` (the C#-literal FRAGMENT —
**byte-identity discipline**: PCS-active H/L render through Roslyn's `FormatLiteral` exactly as the former
`EmitContext.FigFill`, the native pins keep the FIXED historical escape texts `'ÿ'`/`'\0'`, never
re-rendered — my first draft re-rendered them and would have diffed every snapshot; caught before commit).
DELETED: `EmitText.FigurativeFill`, `EmitContext.FigFill` ×2, `FieldEmitter.FillCharFor`,
`ConditionRenderer.FigurativeFillChar`'s table (its raw NUL literal is GONE — the file is ripgrep-visible
again), `CSharpEmitter.FigurativeWordFill`'s table, and the Report-Writer `RwValueOperand` word map — all
routed through the service (~14 call sites).
**Preserved divergences (deliberate, documented in the service):** the three call-site ALL-strip variants
(whitespace-only / whitespace-or-letter / glued-only) REMAIN at their sites — they differ observably; unifying
is a behavior change queued for §8.3.3.6.4 adjudication. Likewise the membership fill's missing category pin
(ConditionRenderer's historical semantics) — flagged, not silently "fixed".

**Verify.** Sln Debug + Release clean; 3166 conformance · 281 unit · 32 characterization byte-exact.
Step 4's second half (the `RuntimeApi` façade + the bare-`Cobol*.` guard test with its shrinking whitelist —
~398 sites across 19 files, per the audit census) lands as the NEXT sub-commit.

