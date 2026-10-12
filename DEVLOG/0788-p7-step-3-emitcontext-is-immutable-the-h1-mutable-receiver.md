## Entry 788 — 2026-07-11 14:41 PDT — P7 Step 3: `EmitContext` is IMMUTABLE — the H1 mutable-receiver quadruple is DEAD; `ReceiverContext` travels by parameter

**What (Step 3, DESIGN-codegen-backend §2.5/M2; the premise-audit `wf_8ace7f29-a1d` mapped every site).**
- `EmissionContext` → **`EmitContext`** (ends the legacy-tree name collision), now IMMUTABLE: the four mutable
  receiver fields (`TargetScale`/`TargetReal`/`TargetRounding`/`InSizeErrorContext`) are DELETED with their
  set/reset discipline (`SetTarget`, the EmitDivide/EmitCompute trio-writes, EmitArith's `InSizeErrorContext`
  set/clear, and the SOLE surviving H1 reset — `ConditionRenderer.Render`'s `ctx.TargetReal = false`).
- **`CodeGen/Roslyn/ReceiverContext.cs`** (NEW): `readonly record struct (Scale, Real, Rounding, InSizeError)`
  + `None`. **The visitor-signature decision (recorded in DESIGN §2.5):** the generated single-arg
  `Visit`/`Accept<T>` can't thread a parameter, so every PUBLIC render entry REQUIRES `in ReceiverContext rcv`
  (`Render`/`AsNum`/`Fold`/`Combine`) and stores it in ONE private `NumericRenderer` field the internal
  recursion reads — freshly assigned at every entry ⇒ no render can run under a stale receiver; the
  by-construction guarantee sits at the API boundary. `IntrinsicRenderer` reads the live receiver through
  `NumericRenderer.Receiver` (its args render under the SAME receiver — the pre-P7.3 constant-ctx semantics).
- `EmitArith` becomes `Action<bool>` — the store closure receives the size-error flag (BOTH triggers: the
  phrase AND `>>TURN EC-SIZE` checking), so `MulChecked`/`DivideOrThrow` selection is parameter-driven.
- Receiver-derived contexts at the arithmetic sites (`RcvFor(Receiver, ise)` — the pure factory replacing
  `SetTarget`); receiver-SET contexts for the §14.7.7 one-initial-evaluation shapes (EmitDivide/EmitCompute
  multi-target: widest scale · all-float · Truncation — and NOW ALSO **EmitGiving and EmitDivideRemainder**,
  which pre-P7.3 rendered their RHS/senders under the PREVIOUS statement's leftover `Target*` — the live H1
  instances this step closes); `ReceiverContext.None` at the ~25 receiver-less sites (GO TO DEPENDING,
  ConvertSource numeric MOVE paths, PERFORM VARYING FROM/BY, SET, CALL BY VALUE, INSPECT counters, keyed-IO
  lengths, PTR amounts, STRING/UNSTRING pointers/tallies, LINAGE/RETRY/LINES expressions).
- **The battery adjudicated the staleness question:** all 32 characterization snapshots byte-exact + 3166
  conformance green — no stale-leak had ever materialized in emitted output on the corpus (receiver-less
  renders never reached a Divide/Power/Real-Combine under stale state); the hazard is now structural, not
  observed. Stale H1 prose swept from IntrinsicRenderer/NumericRenderer docs. NOTE for the sweep rule:
  `ConditionRenderer.cs` contains a literal NUL (LOW-VALUE char) — ripgrep treats it as BINARY and silently
  skips it; every verification grep here used `--text` (the audit caught its own first sweep missing that file).

**Verify.** Sln Debug + Release clean; 3166 conformance · 281 unit · 32 characterization byte-exact.

