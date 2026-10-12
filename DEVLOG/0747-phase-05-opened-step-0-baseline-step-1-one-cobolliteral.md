## Entry 747 — 2026-07-10 18:01 PDT — PHASE 05 OPENED — Step 0 (baseline) + Step 1: one `CobolLiteral.Decode` (both ISO delimiters) fixes the apostrophe-VALUE silent miscompile

Per the owner's "move other phases forward around Phase-04's D10 tail," PHASE 05 (unified data model — `StorageForm`) is
now the active work. Landed its first two steps.

**Step 0 — baseline captured.** Battery green on a clean tree: greenfield conformance 3157 · unit 227 · characterization
32 byte-exact · FULL legacy guard NIST 353 MATCH. Every later "battery green / snapshots neutral" claim is relative to
this.

**Step 1 — `src/Cobol.Net.Compiler/Common/CobolLiteral.cs` (the ONE COBOL string-literal codec, ISO §8.3.1.2).**
`Decode` / `IsStringLiteral` / `AllLiteralText`, recognizing BOTH the quotation-mark and apostrophe delimiter forms
(equal-standing; a doubled OPENING delimiter is one embedded delimiter; `N`/`B` prefixes are part of the token). This
**fixes a confirmed silent miscompile** (DESIGN-data-model §2.8): the three per-layer decoder twins
(`StatementBinder.DecodeCobolString`, `DataBinder.DecodeString`, `EmitText.DecodeCobolString`) already handled both
delimiters, but the **guard tests that gated whether to decode hard-coded `"`** (`EmitText.AllLiteralText`,
`FieldEmitter.GroupValueText`, `StatementBinder.Initialize` ×4) — so an apostrophe-delimited `VALUE 'x'` fell through to
raw/numeric text. Now the guards route through `CobolLiteral.IsStringLiteral` (delimiter-agnostic).

**What changed:** the 3 twins DELETED; ~30 call sites across 13 files repointed to `CobolLiteral.Decode`;
`EmitText.AllLiteralText` is a thin forward to `CobolLiteral.AllLiteralText`; the hard-coded `'"'`-only guards replaced
by `CobolLiteral.IsStringLiteral` (the N/B category branches keep their prefix-letter test, then the codec). New
`CobolLiteralTests` (×21 theory cases) lock both delimiters + doubled-delimiter + `N'…'`/`B'…'` + `ALL 'x'`.

**Behavior:** greenfield-only (`Cobol.Net.Compiler`; the legacy compiler has its own decoders and is untouched — legacy
guard a belt-and-braces confirm). conformance 3157 (unchanged) · unit 248 (+21) · characterization 32 byte-exact
(neutral — the characterization corpus uses `"`, so the apostrophe fix changes no existing snapshot) · legacy guard 353
MATCH. The apostrophe conformance GOLDEN is deferred to Step 13 (after StorageForm, so it exercises the full VALUE-init
path); Step 1 fixes the decode/guard plumbing + the unit lock.

**RESUME AT: PHASE-05 Step 2** — `StorageForm` as a PARALLEL derived value + `StorageFormPass` + the corpus equivalence
assert (DESIGN Phase D0, no behavior change; the prove half of prove-then-delete).

