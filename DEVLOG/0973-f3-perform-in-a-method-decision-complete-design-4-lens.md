## Entry 973 — 2026-07-22 09:24 PDT — F3-PERFORM-in-a-method: decision-complete DESIGN + 4-lens adversarial verify (SSOT §9.10) — the top phase-14 item, do-not-rush

**Context.** Resumed on `phase-14` at the top of the plan §0 REMAINING list: **F3 PERFORM inside an OO method** — the one
genuine remaining P13 feature (loud-0899'd via `F3StagedInMethodStub`; owner: "do NOT rush"). Baseline re-confirmed green
at HEAD (`1f56f572`): build 0/0, characterization 33/33, F3 unit 37/37.

**Diagnosis.** Traced the whole F3 runtime + the OO emit-into-a-type spine. The gap is TWO coupled problems, not one: (1)
**pc-reachability** — a class has no class-level dispatcher; each METHOD emits its own `__MDispatch(startPc,exitPc)` local
function whose `switch` covers only its slice `[EntryPc..EndPc]`, so a handler pc appended above the whole class pc space
(`AddF3Handler` → `classCount+ordinal`) hits `default:` and silently never runs; (2) the deeper **data-scope** — a WHEN
handler references the method's per-activation locals (LINKAGE/LOCAL-STORAGE §14.5.3, method-local INDEXED-BY), captured
only by the method-local `__MDispatch`, so the parts of the F3 machinery that reach the dispatcher (`__RunUse`/`__RunF3`/
`__useActive`) MUST themselves be method-local — a class member can neither call a method-local function nor see its
per-activation locals.

**Design (SSOT `PHASE-13-c5-perform-format3-DESIGN.md` §9.10, decision-complete).** The SAME pc-range mechanism re-scoped
per context (NOT a second mechanism, NOT local-function-per-handler which would fork the EXIT-PERFORM/RESUME handling):
handler pc-ranges appended to the class `bound.Paragraphs` (binder IDENTICAL to the program path); `__RunUse`+`__RunF3`+
`__useActive` become method-LOCAL (their sole caller, the frame `Matcher` closure emitted inline in the method's
`__MDispatch` case, captures them + the method's locals); `__EcPerform`/`__IoCheckEc` stay CLASS members (they reach the
handler only through `ExceptionState.RunTopFrame`→the Matcher, never `__RunF3` directly). Per-method handler slices are
contiguous (proven by `BindMethodRoster`'s pc-order second bind loop). A per-artifact scope table, binder/emitter changes,
5 edge cases, 5 probes, and a 4-increment plan (M1 dead binder/data → M2 scope-parameterized machinery → M3 `EmitMethod`
wiring → M4 un-reject + behavior matrix + comprehensive gate) are recorded.

**4-lens adversarial verify (`wf_570480e6-06d`, 643k tok).** C# emission model = SOUND (field-stored lambda calling a
method-local function compiles; per-activation capture, mutual-local-function recursion, two-range switch all hold). Three
confirmed defects, all FOLDED into §9.10.1: **C1 (BLOCKER, caught by all 3 non-C# lenses)** — `__IoCheckEc`/`__EcPerform`
are emitted at class scope where the per-method `UnitHasF3Perform` is false, so their frame-first branches would be
silently absent ⇒ an I/O WHEN in a method fatal-terminates instead of running imp-2; fix = gate those class-member
emissions on the class-level `bound.Ec.HasF3Perform`. **C2 (MAJOR)** — the run-unit-wide frame stack lets an invoked F3
method's unmatched raise leak up into the activator's WHEN (CALL only does a defensive finally-side `TrimPerformTo`, no
during-call isolation); fix = a per-activation FLOOR on `RunTopFrame` raised at F3-method entry (O(1), defaults to 0 ⇒
zero change for programs/CALL — the cross-CALL "in range" reading stays the documented staged item). **C3 (MINOR)** — the
scope-parameterized `__RunUse` must render `dispatch.DispatchName`, not the hardcoded literal `__Dispatch`
(`DispatchEmitter.cs:203/215`).

**Next.** Implement M1→M4 with wave-local gates; M4 gates the comprehensive suite + GnuCOBOL differential (shared EC/OO
emit seams). No code emitted yet — the construct stays 0899-rejected in a method until M4.

