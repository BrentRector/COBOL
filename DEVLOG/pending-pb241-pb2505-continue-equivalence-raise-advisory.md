## PB241 + PB2505: the bare-CONTINUE equivalence is pinned, and a RAISE that raises nothing is advised (COBOLNET3803)

Wave 1084 group C, the EC-binder pair: one statement whose meaning was right but unwitnessed, and one whose meaning was
right but invisible.

**PB241 — ISO §14.9.9.3 SR2.** "If the AFTER phrase is not specified, then the statement is processed as if an AFTER
phrase were specified with arithmetic-expression-1 specified as zero" (`cite.py --check` OK). The rule's whole content is an
equivalence, and the two spellings take two paths: a bare CONTINUE binds to `BoundNop` and emits nothing, while
`CONTINUE AFTER 0 SECONDS` emits `CobolTiming.ContinueAfter` inside the ambient `EnterStatement`/`ExitStatement` pair. The
static read in the note said the paths agree; no golden compared them, so the row sat PARTIAL. Re-probed on this build
under `>>TURN EC-CONTINUE-LESS-THAN-ZERO CHECKING ON WITH LOCATION`: identical triples. The fix is the witness.
`conformance:2023/pb241_continue_bare_equals_after_zero` runs three legs with expected values from §14.9.9.4 GR1, §15.33.3
r1, §15.32.3 r2, §15.30.3 r1/r2, §14.6.13.1.1 and §14.9.28.4 GR17: from no exception the two spellings leave the same
EXCEPTION-STATUS / -STATEMENT / -LOCATION; after a real raise (`CONTINUE AFTER -1 SECONDS`) each KEEPS the raise's triple
(`EC-CONTINUE-LESS-THAN-ZERO`, `CONTINUE`, `PB241CEQ; MAIN; 55`); inside `PERFORM … WHEN EC-CONTINUE-LESS-THAN-ZERO`
neither reaches the handler while the AFTER -1 control does. The negative `pb241-continue-after-zero-below-2023` holds the
AFTER-0 spelling to COBOLNET0900 at 85/2002/2014 (Annex E.3.3 item 14). `BindContinue`'s documentation now states SR2 as
the reason for `BoundNop` and names the golden. Row `SR-14.9.9.3-2` PARTIAL → CONFORMS (GAP 101 → 100).

**PB2505 — quality of implementation.** kb/Work PB867 decided that an un-TURNed `RAISE EXCEPTION EC-USER-x` raises nothing
(§14.6.13.1.1, §14.6.13.1.4), and the standard's own Annex D.2.5 sample writes such RAISEs with no TURN, so a user who
copies it gets a program that runs past its fatal errors in silence (re-probed: `ERROR OPENING FILES` then `RAN ON PAST
FATAL ERROR`, no diagnostic). The note asked whether "provably off" could be defined. It can, exactly: checking is lexical
(§7.3.25.4 GR6, and the §14.9.28.4 GR14 implicit TURN of an exception-checking PERFORM), so the fold at the RAISE's own line
is the run-time answer, and `EcEmitter.EmitRaise` already emits nothing from that fold. `EcBinder.BindRaise` now issues
**COBOLNET3803** (`raise-checking-not-enabled`, Warning) from the same `BoundRaise.Enabled` bool, naming the
`>>TURN … CHECKING ON` that would make it raise. Behaviour is unchanged and legal source stays legal. A fatal name is not
advised (it terminates, the §14.6.13.1.3 8) arm); EXIT/GOBACK RAISING are not advisable (enablement is the caller's,
§14.9.18.4 1) b)). Four `ExceptionConditionConformanceTests` facts pin it: each statement advised in the D.2.5 shape; only
the disabled RAISE advised across TURN ON, PERFORM WHEN and TURN OFF; lexical, not dynamic, at 2002 and 2023; a fatal name
not advised. The D.2.5 sample is recorded as a candidate defect report against the standard, the class of kb/Work PB312.
Design doc: the conditions/exceptions diagnostics band.

**Self-review.** Architecture: the advisory reads the one enablement fold the emitter reads, with no second copy of the
rule. Sibling arms: RAISE identifier-1 is never TURN-gated (no advisory owed); EXIT/GOBACK RAISING are dynamic. Comments
and citations re-checked with `cite.py --check`. Performance: one bool test per RAISE at bind time.

Gate: see the report (`E:/COBOL-coord/scratch/reports/w1084c-PB241-report.md`).
