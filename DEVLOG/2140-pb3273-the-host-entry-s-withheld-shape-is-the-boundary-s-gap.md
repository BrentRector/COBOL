## Entry 2140 — 2026-10-16 21:21 PDT — PB3273: the host entry's withheld shape is the boundary's gap (PB3139), now pinned and documented

Wave 1105 group H took PB3273: "a strong group holding both a variable-length component and a reference has no host
entry (the GR13 restriction in `PublishedRecordEmitter.Renders`)". The note asked whether the restriction is
spec-required or a gap before a fix was chosen.

**Re-probe.** On a fresh Debug build of main 2b5a81145 a unit whose formal is a strong group holding `PIC X DYNAMIC LENGTH`
and `USAGE POINTER` publishes no entry class while its siblings do (`BOTH.cob`: `H3MAIN` and `H3OK` only). The note's claim
reproduces.

**Verdict.** ISO section 14.2.3 13) obliges the implementor to state restrictions; it imposes none (`cite.py --check`
OK), so the restriction is an implementation gap. Its root is not the host emitter. The shape has no crossing form in any
lane of the boundary: a COBOL CALL of it is refused COBOLNET1688 (legal source: section 14.8.2.2 2) makes two groups of
one type correspond, section 13.18.60.3 14) admits a pointer in a strong type, nothing bars a strong group from being
variable-length), the activated program's own formal of that description stages the Tier-C loud at its copy-in, and a group
MOVE of it aborts `NotImplemented` (A.1 item 57). The storage itself works: one cell holds the managed slot and the dynamic
slot together. An entry offered over that boundary could only reach a loud, so withholding it is the honest behavior, and
lifting it means giving the boundary a form (the cell area plus the group's atoms, `CallEmitter.AreaOf`'s `VarGroupWindow`
arm), which touches the binder's capability gate, the CALL and callee emission, `CobolArgAdapt` and the whole-group
consumers: a redesign beyond a host-surface slice.

**What landed.** `kb/Work/PB3139` (an open analysis whose to-do was exactly this probe) is re-verdicted a defect with the
measurements and the expected behavior, and PB3273 is blocked on it. `HostEntryShapeDriftTests.AGroupHoldingAComponentAndAReference_PublishesNoEntry_WhileTheBoundaryHasNoFormForIt`
pins the withheld entry (the sibling units still publish) and the boundary's absence of a form, so it is red the day PB3139
lands and the restriction is then lifted together with the record's composition of the new form's storage. The
`HoldsSlots` conjunct in `Renders` is implied today by `CurrentExtentImageCapable` and stays as the guard against a lift that
leaves the record dropping its references; the comments on `Renders` and `HostEntryEmitter.Published` (one was stale) and the
design 4.3 / DOC-A.1-141 text now say why.

**Self-review.** Silent failure: the comment fix removes no check, and the new test asserts the absence of an entry
rather than a catch. Tests: the fixture has both a publishing and a withheld unit, so a regression that withholds
everything is also red. No type was added. Gate (`TestResults/build-local/20261017T004840Z-aa28c4`): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` with the new Fact passed in `leg-1-Unit.trx`; semgrep verify PASS.

