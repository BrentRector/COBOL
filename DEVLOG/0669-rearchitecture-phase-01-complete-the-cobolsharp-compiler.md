## Entry 669 — 2026-07-07 21:05 PDT — Rearchitecture PHASE 01 COMPLETE — the CobolSharp.Compiler.* → CobolNet.Frontend.* rename

Finished Phase 01 (steps 4–8), pulling the front-end namespace rename FORWARD from the former G8 big-bang so every later
phase edits final names and G8 becomes a pure *deletion*. Behavior-neutral by construction (proven by the P0
characterization gate — see below). Continues DEVLOG 668 (steps 1–3: dead-grammar + JSON/XML removal).

**Step 4 — generated ANTLR package → `CobolNet.Frontend.Generated`.** Single-sourced via a csproj `<AntlrNamespace>`
threaded into the generation `Exec` + both `.ps1` `-PackageName` params; repo-wide flip of the fully-qualified
`CobolSharp.Compiler.Generated` (59 files incl. the `superClass` base decl + legacy consumers). `61812b7`.

**Step 5 — the four hand-written sub-namespaces** `CobolSharp.Compiler.{Common,Diagnostics,Parsing,Preprocessor}` →
`CobolNet.Frontend.*` (decls + all consumers; the legacy tree keeps its OWN 76 namespaces and only flips its
shared-front-end imports, §2.1). Per-segment segment-literal seds (74 files). `b98d3b04`.

**Step 6 — `Frontend.cs`:** corrected the false "reuses the legacy `CobolSharp.Compiler` assembly" banner, and narrowed
the SLL-bail `catch (Exception)` → `when (e is ParseCanceledException or RecognitionException)` — the ONE deliberate P1
behavior change (a non-parse internal bug now propagates instead of being silently retried under LL). `9be00fe`.

**Step 7 — banners:** the stale "namespaces stay `CobolSharp.Compiler.*` until G8 / big-bang" comments in both csprojs +
`COBOLNET_DESIGN.md` §1.4 + the §17 paragraph (marked SUPERSEDED, history retained). `db6ae722`.

**⚠ TRANSPARENCY — two corrections beyond the phase recipe, both caught by verify-by-building** (the phase doc assumed
the fully-qualified sed sufficed; it did not, because the legacy tree used namespace-relative shorthands):
(1) BARE `Generated.`/`Common.<Type>` prefixes in 6 legacy semantic files (resolved as `CobolSharp.Compiler.<seg>` from
`CobolSharp.Compiler.*`) → fixed with a `using <seg> = CobolNet.Frontend.<seg>;` alias each (consistent with §2.2's kept
`Core` alias); (2) PARTIAL `Compiler.Common.<Type>` refs in 2 legacy test files (`Compiler` resolving up to
`CobolSharp.Compiler`) → flipped with a preceding-char-preserving sed. Documented in the PHASE-01 STATUS + the step
commit messages.

**Behavior-neutrality — the P0 net earned its keep.** The characterization gate (gates 2+3, 32 snapshots) is
**byte-identical** with NO re-baseline — proof that the whole rename + JSON/XML removal + catch-narrowing changed zero
emitted C# and zero diagnostics on the corpus. This is exactly the neutrality proof P0 stood up for.

**Battery (§5, close-out):** whole-solution build 0-err (warnings-as-errors); greenfield unit 213 · characterization 32
(byte-identical) · conformance 2036; FULL legacy guard **NIST 353 MATCH**, 0 regressions (legacy unit 1204 + integ 607,
ALL GREEN). Guard flakiness noted: one close-out guard run reported 352 (RW103A's `MATCH` line dropped by the 32-way
parallel stdout capture — 0 regressions, and RW103A passes in the greenfield conformance 2036); a re-run confirmed 353.
NEXT: **Phase 02** — the `Cobol.Net.Editions` leaf assembly + the first-class diagnostic registry.

