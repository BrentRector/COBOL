## Entry 1380 — 2026-08-29 07:33 PDT — PB138 LANDED: CONTINUE AFTER whole — the nonfatal dispatch exists, non-finite screens, the truncation runs in the value's own domain, and a suspension is not an exit point

Four defects, one statement. (a) EC-CONTINUE-LESS-THAN-ZERO was RECORDED and never DISPATCHED — the
golden's own generated code carried a dead handler pc. The runtime now REPORTS the GR1b raise and the
emitted site runs the §14.6.13.1.4 nonfatal USE-declarative selection (RESUME honored, no fatal arm —
nonfatal's default is to continue); the golden's handler prints both its lines and execution resumes.
(b) `(long)double.NaN` saturates to 0, so a NaN interval silently skipped the suspension — non-finite now
screens FIRST as the CA10-checked EC-DATA-NOT-FINITE (§14.6.13.2 item 3), with no-suspension the lenient
outcome (sleeping forever on +Inf is the one thing no reading licenses). (c) The interval was delivered as
binary64 on every path, so a fixed-point or decimal 0.999… with enough nines — whose binary64 image is
exactly 1.0 — slept a full second where GR1's implicit COMPUTE without ROUNDED truncates to ZERO. The
emitter now hands the exactly-truncated seconds beside the full-precision sign value, per lane
(CobolNum.Rescale truncation for fixed-point, ToUnscaled(0, Truncation) for decimal); the probe ran the
17-nines interval in 0.043 s wall. (d) DeclIsTrivialExit read ANY lone CONTINUE — including CONTINUE
AFTER, a SUSPENSION — as a pure exit point, truncating a USE handler's bounded dispatch mid-handler; the
AFTER phrase now disqualifies. The §14.9.8.4 miscite (COMPUTE's clause, cited for CONTINUE) corrected —
the rest of the citation sweep stays PB159's.

THE GATE'S TWO REDS, both the batch's own edges: the user-word-commit-2023 MATRIX row still probed the
reference position PB137 made structurally unparseable (reshaped to the declaration screen, same as the
negative — the PB137 gate's filter had missed ~VersionMatrix, the edition-sweep rule's exact warning); and
the RuntimeApi guard caught the new emission spelling CobolTiming.* raw — routed through
RuntimeApi.ContinueAfter/ContinueAfterExact, the ONE emission channel.

Verdicts: GR-14.9.9.4-1 DIVERGES → CONFORMS, SR-14.9.9.3-1 DIVERGES → CONFORMS.

