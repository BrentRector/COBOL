## Entry 1397 — 2026-08-30 12:00 PDT — PB168: one guard per scope — the connector gets its §8.6.4 storage duration, and the fleet catches the two halves the first cut left behind

The defect: `__filesRegistered` was per-INSTANCE while ProgramTable hands a RECURSIVE unit a fresh
instance per activation — every activation re-registered, FileRegistry.Register replaced the live
connector, the depth-2 WRITE answered '42', the displaced writer leaked undisposed. The derivation
sharpened the note's framing: §14.6.2.3.2's action 3 (connectors "to not be in any open mode") fires only
when data enters the INITIAL state — for a non-INITIAL unit's static data that is cases 1–3 — and
§14.6.2.3.3 keeps everything else LAST-USED. So the guard gets the connector's storage duration:
UnitStaticFiles (RECURSIVE AND not INITIAL, functions included, deliberately without UnitStaticWs's
childless conjunct — the flag never crosses an __outer bridge) emits it STATIC, __ResetStatics clears it
on exactly the three initial-state cases, EmitsStaticReset became THE ONE shared emission/registration
predicate (the two emitters had mirrored it by hand under a comment warning that divergence is a CS0103 —
a shared rule begging to exist), and CloseDisplaced flushes what a re-registration displaces.

Then the landing review fleet (58 agents) did exactly what it exists for: 14 confirmed findings collapsing
to five distinct defects, three of them MINE — the first cut had moved ONE flag to run-unit scope while
the block it guarded held THREE scopes of work. (1) CRITICAL, refuters reproduced it with a running
binary: report-engine construction assigns per-INSTANCE __RPT_n fields, so a RECURSIVE unit's second
activation skipped the static-guarded block and NRE'd on INITIATE — a regression the golden could not see
because it had no REPORT SECTION. One guard per scope now: construction under its own per-instance
__reportsConstructed. (2) MAJOR, the spec's own sentence: §8.6.4 makes FILE SECTION data static — ONE
record area per run unit — so the record area had to move WITH the connector, or the resumed outer
activation reads its own stale record where the standard shows the inner READ's (a silent wrong answer
replacing the loud '42' the bug used to give). RouteStaticUnitStorage routes FD records through the same
extracted per-root routine as WS, external files excepted, with NO __ResetStatics re-seed (action 2 names
only WS/LS), and the RecursiveContainedWs staged loud widened to the FILE SECTION for the
recursive-with-containees shape (an FD may be GLOBAL, §13.18.27). (3) The LINAGE evaluator closure
captured the first activation's instance for the life of the shared connector — it now installs
UNGUARDED, per activation, always the current instance. Plus two honesty items: the re-pinned pb154
comment's "only because GR9" overclaim corrected (CloseDisplaced flushes with the same observable effect;
the leg pins the OUTCOME), and four CloseDisplaced concerns refuted 3-0 and left alone.

Three goldens pin the three mechanisms: the shared connector (writes at both depths — '42' before), the
shared record area (OUTER=D2 — the inner activation's READ visible in the resumed outer), the
second-activation report run (an NRE before). The review's mechanical step also exposed a pre-existing
semgrep baseline drift at HEAD (+30 BigInteger, +49 raw diagnostic literals; stash/verify/pop proved
PB168 adds zero) — registered PB175, with a gate-seat half so drift cannot go invisible again.

Wave gate GREEN: Conformance 2471 (all three pb168 goldens by name), Unit 5027, characterization 33 — the
one interim red was the diagnostics-doc drift guard after the descriptor text widened, regenerated
in-change; the interprogram design doc's recorded-residue line for this exact defect closed with it.
Battery #36 accrues (PB168).

