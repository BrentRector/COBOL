## Entry 970 — 2026-07-22 01:20 PDT — Track ③ Increment 4: THE BEHAVIOR WAVE — the pc-range F3 PERFORM interceptor works end-to-end (12/12 behavior tests, tier-ordered, RESUME/COMMON/FINALLY/EXIT-PERFORM all correct)

**The exception-checking (Format-3) PERFORM now COMPILES AND RUNS** — the 0899 staging is lifted (program path); a
raised, enabled exception condition in imp-1 is intercepted by the tier-ordered WHEN, runs the handler pc-range, and
resumes/terminates per GR20. This is the byte-critical behavior wave (the shared `__EcDispatch`/`__RunUse`/
`__IoCheckEc`/dispatcher seams). Landed as designed in §9, in two safe sub-steps (Step A additive-infra build-green;
Step B the atomic reshape + behavior).

**pc-range synthesis (§9.1):** `ProcedureTableBuilder.AddF3Handler` registers each already-bound handler body
(imp-2/3/4) as a synthetic UNREFERENCEABLE pc-range paragraph on a per-unit side-list; `StatementBinder` appends the
side-list ABOVE the main pc space after the main bind loop (so `_paras.Count` = the frozen main count throughout
binding) and threads `F3HandlerBasePc`/`F3HandlerOwners` onto `BoundProgram`. imp-1 + imp-5 (FINALLY) stay INLINE.

**Bound-node reshape:** `BoundExceptionPerform(Imp1, Whens: BoundExceptionMatch[], OtherPc?, CommonPc?, FinallyBody?,
WithLocation, PerformId, HandlerHasExit)`; `BoundExceptionMatch(OpenMode?, Operands, Imp2Pc)` — imp-2/3/4 are pc refs,
not inline bodies. **The source-gen `BoundVisitorGenerator` auto-adapted** (`StatementChildren(BoundExceptionPerform)`
= `Imp1` + `FinallyBody` only; the handler bodies are walked at their synthetic paragraphs — no double-count);
`BoundStores.Visit` updated by hand (the one manual consumer). Reshape regression: BoundStatementChildren +
BoundVisitorGenerator + F3 parse 37 + frame stack 9 = **57/57**.

**Binder redirect (`EcBindExceptionPerform`):** the 0899 program-path reject DELETED; imp-2/3/4 bind IN LEXICAL
CONTEXT (InF3When + base TurnState, GR14 overlay already popped) → `AddF3Handler`; `PerformId` per unit;
`HandlerHasExit` (a region-C EXIT-PERFORM scan). OO-method F3 → `F3StagedInMethodStub` (loud 0899, never silent-drop,
§9.7). The open-mode WHEN form (`WHEN EXCEPTION INPUT|OUTPUT|I-O|EXTEND`) → loud 0899 (§5.4-1 staged).

**Emitter (`EmitExceptionPerform`):** installs the `PerformFrame` with a TIER-SORTED matcher closure (per-operand
GR3c-g tier + test, sorted `(tier, whenIdx, opIdx)`, mirroring `__EcDispatch`; WHEN OTHER as the unconditional
fallback; a tier-4 EC-ALL is the catch-all); imp-1 under a try (with a nested try + `catch (ExitPerformSignal when
Id==n)` ONLY when a handler EXIT-PERFORMs); `finally { PopPerformFrame(); }`; `__f3fin{n}`/`__f3end{n}` labels;
inline FINALLY between them (skipped on the fatal throw). `BoundExitPerform` now region-aware (`DispatchState.F3Cur`):
Imp1 → `goto __f3fin`, Handler → `throw ExitPerformSignal`, Finally → `goto __f3end`, else the ordinary loop
`break`/`continue`; a nested inline PERFORM saves/restores `F3Cur=None` (§14.9.14.4 GR5a). The dispatcher marks each
appended handler `case` `F3Region.Handler`; the top-level `__Dispatch` is WALLED at `F3HandlerBasePc − 1` (fall-off
the last real paragraph ENDS the run unit, never runs into the handlers; non-F3 renders the literal `-1`, byte-identical).

**`__IoCheckEc` frame-first (§9.5.1, deferred from Incr 3):** the F1/F3 USE tiers factored into a local function; with
`UnitHasF3Perform` the frame is consulted FIRST (both the warning + unsuccessful paths) and the USE tiers run only on
`!__wh` (GR17 — a matching WHEN ignores the USE); non-F3 emits at the base indent = byte-identical.

**Behavior tests (`PerformFormat3BehaviorTests`, 13/13, all spec-pinned — 12 passed on the FIRST run):** nonfatal
resume-in-place · fatal terminate · fatal+RESUME-NEXT-suppresses · **tier: EC-BOUND-SUBSCRIPT beats EC-ALL AND beats
EC-BOUND regardless of source order** · WHEN OTHER · WHEN COMMON after imp-2 · RESUME-NEXT-skips-COMMON · FINALLY on
normal path · EXIT-PERFORM-in-handler → FINALLY (skips COMMON) · **GR21 re-raise-in-handler NOT re-caught** (an
overflowing ADD in a handler raises EC-SIZE-TRUNCATION which WHEN OTHER does NOT re-catch — the frame is transparent
while handling — → fatal terminate; a RAISE in imp-2 is bind-banned XS-RAISE, so the trigger is a non-RAISE store) ·
version gate 0900 at 2014 · open-mode 0899. Two OBSOLETE `PerformFormat3Tests` (that asserted the 0899 staging) updated
to assert compiles-clean.

**COMPREHENSIVE GATE — ALL GREEN:** characterization **33/33 byte-identical** (all F3 scaffolding gated
`UnitHasF3Perform`, false for every existing program) · full greenfield Conformance **3818/3818** (3816 goldens
byte-identical + the F3 tests; the only 2 red were the now-fixed obsolete staging assertions) · F3 tests **39/39** ·
reshape-regression **57/57** · **legacy guard ALL GREEN (NIST 353 MATCH, 0 REGRESSION; legacy Unit 1203, Integration
678)** · **⚡ GnuCOBOL external differential +2 FIXES / 0 REGRESSIONS** (AGREE_ACCEPT 476→478, WE_REJECT_THEY_ACCEPT
570→568, WE_ACCEPT_THEY_REJECT + AGREE_REJECT unchanged — 2 corpus F3-PERFORM programs we previously 0899-rejected are
now correctly accepted) · build 0/0. ⏭ NEXT = Incr 5–7 residue (the conformance-program + the doc sweep recording the
D12 decisions [RESUME-skips-COMMON, FINALLY-on-fatal defect, bare-file tier] + `CONFORMANCE.md`) → then ④ §24
fix-queue → Wave I merge → P14.

