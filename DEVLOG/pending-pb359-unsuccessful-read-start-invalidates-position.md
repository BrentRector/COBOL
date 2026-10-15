## PB359: every unsuccessful READ and START invalidates the file position through one terminal

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
shape of OPEN, not on the rule. The fix gives every READ and START one way to fail:

- `FileConnector.Unsuccessful(status)` is the one failure terminal, the twin of `ReadSucceeded`: it calls the
  virtual `InvalidateFilePosition` and then assigns the status, as one step. `StartPositioned()` is the START
  success epilogue (`'00'`, the `'46'` poison cleared) that the three organizations' own epilogues now end in.
- The guards (`ReadOpenModeGuard`, `SequentialReadGuard`, `RandomReadAbsentOptionalGuard`, `StartOpenModeGuard`)
  are pure: they answer a status and change nothing. `SequentialReadReachesRetrieval`, which the pre-read record
  identity asks, is now just `SequentialReadGuard() is null` instead of a second copy of its three tests.
- Every READ and START entry returns its failures through `Unsuccessful` (the sequential connector's `bool`
  entries through `ReadUnsuccessful`, which wraps it), so no entry assigns `Status` itself.

**Gates and witness.** `FilePositionInvalidationDriftTests` (Unit, Roslyn) checks four things: no READ or START
entry of a connector (a method whose name begins with `Read` or `Start`, other than the terminals) assigns
`Status`; `Unsuccessful` calls `InvalidateFilePosition` before it assigns the status; the guards assign nothing
and invalidate nothing; and the virtual and its keyed overrides are the only writers of `LastReadUnsuccessful =
true` and `_fpiValid = false`. It also checks that it still sees each organization's known entries. A
failure-proof test drives each predicate with a source that must trip it. The golden `tests/conformance/85/pb359_unsuccessful_read_start_position` gets its expected
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

**Branch review (w1084e R1-1).** The first version of the drift test matched three literal texts: the flag set by
hand, the validity bit cleared by hand, and `ReadOpenModeGuard() is { } … return Status =`. It could not see a
bare `Status = X; return false;` or `return Status = X;` exit with no invalidation, which was one of the three
shapes this note named, nor a deleted `InvalidateFilePosition()` inside a side-effecting guard. The finisher fixed
the shape rather than the matcher. The guards became pure, and the one failure terminal, `Unsuccessful`, carries
the invalidation. The gate is now a Roslyn rule over every READ/START entry: no entry assigns `Status`. Reverting
`SequentialConnector.ReadRecord`'s `_reader is null` arm to its old bare form turns the gate red and names the arm.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261015T211153Z-8627d3: Conformance 6,085/11,709, Unit 33,216/33,672, Characterization 6/36 in leg 1; no failures). The golden, the five drift tests and the PB352 START goldens ran in leg 1 and passed. Semgrep verify: PASS. The Linux gate runs at the landing.
