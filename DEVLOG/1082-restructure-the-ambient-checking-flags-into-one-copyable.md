## Entry 1082 — 2026-07-28 15:07 PDT — Restructure the ambient checking flags into one copyable snapshot, so GR14 cannot rot

Preparation for V57, and an owner decision that became non-negotiable rule 5 (CLAUDE.md) along the way.

**Why this exists at all.** V57's finding says the fix is binder-only — "NO ExceptionState flag/helper, NO
EcEmitter row … this only corrects the compile-time TurnState the EcWrap gate consults". Implementing it
disproved that. §14.9.28.4 GR14's "implicit PUSH ALL followed by TURN OFF ALL" is a RUNTIME state change: the
ambient checking flags are set by the guard around the RAISING statement, and a handler is dispatched from
INSIDE that guard, before its `finally` clears them. Binding a handler body under a disabled TurnState removes
the handler's own guard and leaves the raiser's flags standing.

**The probe that settled it**, and it is the reason this was not a guess: a Format-3 PERFORM whose handler
carries an EXPLICIT `>>TURN EC-BOUND-REF-MOD CHECKING OFF` still terminates abnormally on the handler's
out-of-range ref-mod. No amount of bind-time work can fix that, because the flag is not coming from the
handler's binding.

**The owner's call: restructure first.** ExceptionEngine had 7 ambient `…Checking` flags as separate
auto-properties and no push/pop. A hand-rolled save/restore over 7 named flags would have worked today and
rotted immediately — CA9 adds 3, CA10 2, CA11/V55 and CA37/CA38 more, and every one of them would have had to
REMEMBER to join the list. The failure mode is silent: a condition that forgets simply stops obeying GR14, no
test fails, and it surfaces as a program terminating inside a handler the standard says cannot raise.

**What landed.** A `CheckingFlags` struct holds all seven; each engine property delegates to a field, so
GENERATED CODE AND EVERY RAISE SITE ARE UNTOUCHED — the storage shape changed, the surface did not.
`PushAllCheckingOff()` returns the prior value and assigns `default` (TURN OFF ALL is now a struct default, not
a list of assignments); `PopAllChecking(saved)` restores. Static shim forwarders added. Adding a condition is
now: one struct field, one delegating property. Nothing else has to know.

**And the invariant is ENFORCED, not remembered.** `ExceptionCheckingFlagsDriftTests` reflects over
ExceptionEngine's `bool …Checking` properties and asserts (a) every one has a matching `CheckingFlags` field —
so a flag declared as a loose auto-property is caught as an ORPHAN that would escape PUSH ALL — and (b) PUSH
ALL clears every one and POP ALL restores every one, including restoring a false flag as false. Proved it can
fail: adding a probe `EscapedFutureChecking` auto-property fails BOTH tests; removing it goes green.

**Gate:** characterization 33/33 · CorpusRunnerTests 347/347 · Unit Exception+PerformFormat3 57/57. One real
failure on the way — `Manifest_CoversEveryProgram_NoOverlap(edition: "2023")`, because V57's golden was sitting
on disk unregistered. That is the manifest-integrity check working, and it fired at the WAVE-LOCAL gate rather
than waiting for the comprehensive one. The golden moves with V57, the commit it belongs to.

