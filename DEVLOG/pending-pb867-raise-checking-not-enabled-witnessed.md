## PB867: an un-TURNed RAISE raises nothing, now witnessed and closed CONFORMS

**Mechanism.** kb/Work PB867 asked whether `RAISE EXCEPTION EC-USER-x` raises its condition when checking for it is not enabled. The
w1033 adjudication (refuter UPHELD) found that the standard settles it: `§14.6.13.1.1` says a condition that a statement's general rules
say exists "is raised only if checking for that exception condition is enabled". That covers RAISE's own `§14.9.29.4 1)`. For EC-USER,
"Checking is not enabled unless the TURN directive or WHEN phrase of the PERFORM statement is used". `§14.6.13.1.4` adds "If checking is
not enabled, execution continues as if the exception did not occur". A raise is one event: it sets the last exception status and it
nulls EXCEPTION-OBJECT. An un-raised RAISE therefore does neither. The note stayed open only because row `GR-14.9.29.4-1` was PARTIAL and
no golden witnessed the not-enabled arm.

**Re-probe.** The w1033 repro produced the same output at 2002, 2014 and 2023 on this worktree's build. The un-TURNed RAISE printed `AFTER [`
followed by 31 spaces and `]`. With TURN ON, the declarative printed `DECL [EC-USER-DEMO]`. With TURN OFF again, nothing was raised. Each
un-TURNed RAISE also carries PB2505's COBOLNET3803 advisory. The behaviour is the standard's, and nothing in the compiler changed.

**Witness.** The golden `2002/pb867_raise_checking_not_enabled` (PB867M) runs at 2002, 2014 and 2023:
- C1: an un-TURNed RAISE runs no declarative and leaves the status at spaces.
- `RAISE E` (an exception object) runs its declarative.
- C2: an un-TURNed RAISE after that leaves the status at `EXCEPTION-OBJECT` (`§15.33.3 1)`) and leaves EXCEPTION-OBJECT referencing E. This is the
  EXCEPTION-OBJECT half that registrar 14c asked to be witnessed.
- C3: after TURN ON, the declarative prints `DECL [EC-USER-PB867]` and EXCEPTION-OBJECT is null.
- C4: after TURN OFF, the status keeps the previous name.

The negative `negative/pb867-raise-checking-below-2002` (`reject-at: 85`) is refused with COBOLNET0900 on `>>TURN`.

**Rows.** `GR-14.9.29.4-1` moves from PARTIAL to CONFORMS, and GAP goes from 71 to 70. PB867 is flipped to landed with
`closes_rows: ["GR-14.9.29.4-1"]`.

**Fixed on the way (I9).** `EcEmitter.EmitRaise`'s checking-off comment cited a stale markdown line number (`(24485)`) and leaned on the
GR1 NOTE, which speaks only to control flow. It now cites the §14.6.13.1.1 sentence. `ExceptionState.ExceptionObject`'s summary had two
stale line numbers, and it said EXCEPTION-OBJECT is "nulled by every NAMED raise" without the enablement condition. The EC RUNTIME paragraph
of `COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` now states the checking-off arm.

**Self-review.** Architecture and duplication: no code moved, and the rule lives once, in `EmitRaise`, as the `BoundRaise.Enabled` split.
Full code: the comments now quote the rule they implement. Performance: nothing changed.
