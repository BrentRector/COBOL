## Entry 1012 — 2026-07-23 03:59 PDT — Conformance fix-queue: CA36 (tables-refmod) — a SEARCH with EC-RANGE checking ON + no AT END DISPATCHES the range EC to its USE declarative (ISO §14.9.37.4 GR1b2)

**CA36 (CONFORMANCE-FIX-QUEUE, MAJOR/M) landed.** ISO §14.9.37.4 GR1b2: when the AT END phrase is not specified and
EC-RANGE-SEARCH-INDEX or EC-RANGE-SEARCH-NO-MATCH was raised during a SEARCH and an applicable exception-processing
statement exists, control transfers to it, and if control returns, to the end of the SEARCH. `EmitSearchScan` had
exactly three unsuccessful-path emissions (the initial-index guard, the serial out-of-range INDEX, and the
advance-past-end NO-MATCH), each only `ExceptionState.Set("EC-RANGE-SEARCH-…", false)` then `goto __searchAtEnd`; the
shared `__searchAtEnd` funnel ran AT END statements only when present and otherwise fell straight to `__searchEnd` —
there was NO `EcDispatchExpr`/`__EcDispatch` call anywhere on the SEARCH path. So with `>>TURN EC-RANGE CHECKING ON`,
a SEARCH with a matching USE declarative but no AT END phrase silently SKIPPED the declarative — a mandated handler
never ran.

**The fix** (mirror `EcEmitter.EmitOverflow`'s no-phrase dispatch): (1) a new `internal EcEmitter Ec` property on
`ControlFlowEmitter`, property-wired by `UnitEmitters` exactly like `Statements` (the same cyclic edge). (2) In
`EmitSearchScan`, gated on `dispatchEc = s.AtEnd is null && (s.CheckSearchIndex || s.CheckSearchNoMatch)`: declare
`string __searchEc{id} = null;` and, via a `RaiseRange` helper, set it alongside each existing `ExceptionState.Set`.
(3) In the `__searchAtEnd` funnel, when `dispatchEc`, emit `if (__searchEc{id} != null) { int __searchR{id} =
EcDispatchExpr(__searchEc{id}, ""); if (__searchR{id} >= 0) { __pc = __searchR{id}; break; } }` before the fall-through
to `__searchEnd` — `>= 0` = RESUME AT a procedure (transfer via the dispatcher break); -1/-2/-3 (declarative ran /
RESUME NEXT / no handler) fall through to the end of the SEARCH, all nonfatal (§14.6.13.1.4 #3/#4).

**Blast radius.** Purely additive to the `AtEnd==null && checking-on` niche — every other SEARCH is byte-identical
(characterization 33/33 confirms: existing SEARCH snapshots have an AT END phrase or no EC-RANGE checking, so
`dispatchEc` is false and no new code is emitted). `RaiseRange` emits the same `ExceptionState.Set(...)` string as
before when `dispatchEc` is false.

**Gate.** Conformance Corpus+Search+Range+Exception 440/440 (incl. the new golden) · characterization 33/33
byte-identical · full Conformance (emit gate — running). Golden `conformance/2002/ca36_search_range_dispatch` (2002+ —
EC-RANGE checking + USE AFTER EXCEPTION CONDITION are the 2002 exception model): a SEARCH with no match and no AT END
→ the declarative runs (`DECL-RAN`) then execution continues past the SEARCH (`AFTER-SEARCH`); CLI `--run` verified
(before the fix: only `AFTER-SEARCH`). **22 landed / 24 fix-ready remain.** NEXT = the EC-infra + OO SUPER-BATCH
(exceptions-ec CA9/CA10/CA11/CA12/V57 · interprogram CA21/CA22/V58 · oo CA29/CA30/V55 — all share
EcBinder/EcEmitter/ExceptionState, SERIAL, one coordinated design pass).

