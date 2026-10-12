## Entry 968 — 2026-07-21 23:33 PDT — Track ③ PERFORM Format-3 runtime: decision-complete pc-range design (adversarial panel) + Increment 1 (runtime-additive frame stack) landed

**Resuming Track ③ fresh (owner: "do NOT rush — this rewires the byte-critical EC runtime").** Two things landed
this checkpoint: the decision-complete interceptor DESIGN, and the first (runtime-additive, zero-blast-radius)
increment.

**The design (design SSOT `PHASE-13-c5-perform-format3-DESIGN.md` §9 — the new implementation contract).** I ran an
adversarially-verified design panel (3 independent designers under diverse lenses → 3 spec/buildability verifiers →
1 synthesizer; 7 agents, ~1.27M tokens) to resolve the owner-directed **pc-RANGE architecture**'s open mechanisms
BEFORE touching the funnel. I grounded it with a code-fact brief from direct reads (the single `EcDispatchExpr`
funnel + 9 raise sites, `__RunUse`/`ResumeSignal`, the `ExceptionEngine` home, the pre-binding pc synthesis, the
complete `EcBindExceptionPerform` binder) and cross-checked every semantic claim against the spec myself. Resolved
decisions (each a panel disagreement or a verdict blocker/major, fixed in §9):
- **imp-1 inline** (guarded body in the host paragraph's `case`); **imp-2/3/4 = synthetic pc-range paragraphs
  APPENDED above the main pc space** (run via the existing `__RunUse(id,pc,pc)`); **imp-5 (FINALLY) inline** trailing.
- **Matcher is TIERED, not written-order** — GR17 binds WHEN matching to §14.9.49.4 GR3a–g, which I confirmed
  DIRECTLY in the spec is a tiered priority scan (file+L3 → file+L2 → L3 → L2 → L1, source order only intra-tier);
  the binder's "first written-order match wins" comment is wrong for cross-tier cases (`WHEN EC-ALL … WHEN EC-SIZE`
  → EC-SIZE's tier wins). Mirrors the existing `__EcDispatch` tier structure.
- **`fatal` DROPPED** from the matcher — the fatal/nonfatal split (GR20) is realized by each raise site's own static
  throw idiom + the `-1/-2` protocol, so threading `fatal` was dead plumbing (my own derivation flagged this before
  the panel; the panel confirmed). `EcDispatchExpr` keeps its 2-arg signature ⇒ zero caller edits.
- **No-declarative F3 BLOCKER (all 3 verdicts caught):** `__RunUse`/`__EcDispatch`/`__useActive`/`__IoCheckEc` are
  gated on `decls.Count > 0`, but the canonical F3 (WHEN handlers, no USE declaratives) has zero declaratives → widen
  BOTH the outer (`DispatchEmitter.cs:79`) AND inner (`:166`) gates to include `HasF3Perform`; `__EcPerform` fallback
  gated `UnitHasF3 ? __EcDispatch : -3`.
- **`__IoCheckEc` frame consulted at the TOP** (before the F1 file/mode USE switch) — GR6+GR17: a matching WHEN
  ignores the USE.
- **`RunTopFrame` = top-down WALK with deferred `Handling`-clear** (not "peek top") — nested F3: an EC in an inner
  imp-1 that only the OUTER WHEN names must reach the outer handler (GR17 for the outer PERFORM); the deferred clear
  keeps a skipped inner frame transparent while a selected outer handler runs (its imp-1 is suspended, GR21).
- **EXIT PERFORM:** plain `goto` for imp-1/imp-5; a new `ExitPerformSignal` (sanctioned sibling of `ResumeSignal`/
  `StopRun`/`ProgramReturn`, NOT a second dispatch mechanism) for handler pc-ranges (they cross the nested-`__Dispatch`
  C# call boundary a goto can't); `F3Region` save/restore around nested inline PERFORMs so a plain EXIT PERFORM there
  breaks the inner loop (§14.9.14.4 GR5a).
- **RESUME NEXT skips COMMON** (chosen interpretation — RESUME is a transfer, not a "completion" per GR17; record in
  D12). **FINALLY inline, normal/EXIT path only, NOT on fatal abnormal-termination** (standard defect: NOTE 8 vs GR20,
  corrected from the panel's mis-cite of NOTE 9). **OO-method F3 = loud reject** (keep 0899), never silent-drop.
- 5 unresolved risks flagged for verify-by-RUNNING probes (deep nested re-raise; bare-file operand tier;
  UsageCollectionPass double-count; EcFeatures positional fan-out; SORT/MERGE-USE interplay in `__IoCheckEc`).

**Increment 1 — runtime-additive (this commit).** Purely additive, nothing generated references it yet ⇒ byte-identity
trivial. NEW `Exceptions/PerformFrame.cs` (matcher `Func<ec,file,int>` + `Handling` + `NoMatch=int.MinValue`, no
`fatal`); NEW `Control/Signals/ExitPerformSignal.cs`; `ExceptionEngine` gains a `List<PerformFrame>` stack +
`PushPerformFrame`/`PopPerformFrame`/`RunTopFrame(ec,file,out handled)` (the top-down walk with deferred clear) +
internal `PerformDepth`/`TrimPerformTo`; `ExceptionState` facade delegators; `ProgramTable.CallProgram` snapshots
`PerformDepth` + `TrimPerformTo` in `finally` (per-activation scope, mirroring the `ExternalCheckMask` save/restore).
**Gate: `PerformFrameStackTests` 9/9** (innermost-wins, inner-no-match→outer, GR21 re-raise-not-re-caught, the
deferred-clear nesting proof that distinguishes correct-vs-eager-clear, push/pop balance) + **characterization 33/33
byte-identical** + solution builds 0/0. Wave-local gate only (runtime-additive, no shared `.g4`/emitter — the
comprehensive gate is per-batch pre-merge).

**Process note:** re-ran the design as a workflow (ultracode) rather than solo, because A-vs-B was owner-settled but
the pc-synthesis + EXIT-PERFORM + RESUME/COMMON mechanisms had spec-contradiction hazards; the panel caught the
no-declarative blocker (3/3), the tier-ordering spec violation, and the `__IoCheckEc` frame-placement violation that
a solo pass could easily have shipped. NEXT = Increment 2 (the gating flag flow: `EcFeatures.HasF3Perform` +
`EcState.UnitHasF3Perform` + `BoundProgram.F3HandlerBasePc`, all still inert) → Incr 3 (funnel/`__EcPerform`, dead) →
Incr 4 (pc-range synthesis + tier matcher + un-reject).

