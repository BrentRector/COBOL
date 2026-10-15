## PB359: every unsuccessful READ and START invalidates the file position through one virtual

**Wave 1084 E, kb/Work PB359.** The note described the `'47'` START in both keyed connectors returning
`Status = ReadNotOpenForInput` without applying §14.9.41.4 GR7 ("Following the unsuccessful execution of a START
statement, the file position indicator is set to indicate that no valid record position has been established",
`cite.py --check` OK), and recorded it as latent: no COBOL program could observe it.

**Re-probe: the START half is DISCHARGED.** kb/Work PB352 (9bb9535fb) had already replaced the five hand-written
copies with `FileConnector.StartOpenModeGuard`, which calls the virtual `InvalidateFilePosition` on both failing
arms (703bb3c16 added the permanent-error arm). The probe on this build
(`E:/COBOL-coord/scratch/w1084/w1084e/probe/PB359P1.cob`) prints `47` for START in OUTPUT mode and on a closed
connector, `00` for the first READ NEXT after a reopening OPEN, and `23` then `46` for a random READ miss followed
by READ NEXT, on relative and indexed files. Row `GR-14.9.41.4-7` was already CONFORMS.

**The sibling arm, fixed.** READ has the same rule in §14.9.30.4 GR18 ("Unless otherwise specified, at the
completion of any unsuccessful execution of a READ statement, … the file position indicator is set to indicate
that no valid record position has been established", `cite.py --check` OK), and its exits still had the shape the
note named. The `'47'` READ, sequential and random, and its permanent-error replay returned with nothing. The
random READ's `'23'`, both absent-OPTIONAL arms, the keyed at-end and `'14'` exits and the sequential at-end exits
set `LastReadUnsuccessful` by hand, which skipped the keyed connectors' `_fpiValid` bit. The sequential connector's
non-seekable PREVIOUS arm set `'30'` without invalidating. None of these was observable, for the same reason as
the START: every route had already invalidated, or forced a reopening OPEN. That reason depends on the current
shape of OPEN, not on the rule. The fix routes every exit through the virtual:

- `SequentialReadGuard` now applies GR18 once, on every failing arm. Its arms moved, in the same order, into a
  private side-effect-free `SequentialReadFailure`.
- The new `RandomReadOpenModeGuard` is the random READ's side-effecting twin of `StartOpenModeGuard`.
  `ReadOpenModeGuard` stays pure because the pre-read record identity (`PeekSequentialRecordId` /
  `PeekRandomReadRecordId`) asks it without executing a READ.
- `RandomReadAbsentOptionalGuard` and every other unsuccessful READ exit call `InvalidateFilePosition`. The
  non-seekable arm calls `ReadFailed`.

**Gates and witness.** `FilePositionInvalidationDriftTests` (Unit) checks three things: the virtual is the only
writer of `LastReadUnsuccessful = true`, the keyed overrides are the only writers of `_fpiValid = false`, and no
READ entry returns the pure open-mode answer as its own status. A failure-proof test drives each predicate with a
source that must trip it. The golden `tests/conformance/85/pb359_unsuccessful_read_start_position` gets its expected
values from the spec. It covers relative, indexed and sequential files: a `47` START or READ, a random `23` then
`46`, an at-end `10` then `46`, and a failed START `23` then `46`. In each case a repositioning statement clears
the `46`. Witnesses were added to `GR-14.9.41.4-7` and `GR-14.9.30.4-18`; both were already CONFORMS, so the GAP
count is unchanged (101).

**Self-review.** Architecture: one invalidation mechanism, the virtual, now used by every READ and START failure
exit; the pure/side-effecting guard split is the same split START already had. Full code: GR18's "unless otherwise
specified" was checked against the READ rules. GR10 a) (record operation conflict) is the only exception, and the
registry handles it before any connector step. GR29/GR32 set the indicator during execution, and GR18 applies at
completion. Performance: no allocation added. Duplication: the hand-written `LastReadUnsuccessful = true; _fpiValid
= false;` pairs are gone.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261015T205400Z-4fad7d: Conformance 6,068/11,709, Unit 33,158/33,671, Characterization 6/36 in leg 1; no failures). The golden and all four drift tests ran in leg 1 and passed. The Linux gate runs at the landing.
