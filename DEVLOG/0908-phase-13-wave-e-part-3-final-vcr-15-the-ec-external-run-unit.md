## Entry 908 — 2026-07-19 12:30 PDT — PHASE-13 Wave E (part 3, FINAL) — VCR 15: the EC-EXTERNAL-* run-unit conformance raises (§14.8.4)

**Wave E is COMPLETE** (VCR 63/16 → Entry 904, VCR 18/31 → Entry 905, VCR 15 here). The four EC-EXTERNAL-*
conditions were catalogued-but-never-raised (`ExternalTable.cs:10` conceded "not enforced here yet"); now the three
checkable ones raise at the §14.9.4.4 GR3e CALL raise point. Spec-first derivation (all §s re-read this session):

- **The check (§14.8.4.2/.3/.4):** at each activated element's activation entry, its compile-time
  `ExternalDescriptor` per external RECORD (byte count · record-name VALUE spec · strong TYPE name · CONSTANT
  RECORD presence — §13.18.22 GR6/§14.8.4.3; the complete-record REDEFINES exemption honored by construction)
  and per external FILE connector (FILE STATUS/RELATIVE KEY/LINAGE corresponding-external-item identities —
  §14.8.4.2; the §12.4.5.3 GR1 a–m entry fingerprint — §14.8.4.4) registers into the run-unit `ExternalTable`
  (`Describe`, kind-bucketed so a same-named FD record + connector never collide) and compares against every
  OTHER describer. Violation ⇒ `CobolCallException` carrying the Table 13 name (all three Fatal) — the
  activation unwinds to the CALL site, "the program call is not successful" (GR3e), ON EXCEPTION takes it
  (GR3h #1) via the CALL emitter's catch, now widened from EC-PROGRAM-* to + EC-EXTERNAL-*. EC-EXTERNAL-IMP has
  NO raise site by definition (no implementor-defined external checks — a conforming choice, Table 13 "Imp").
- **The §14.8.4.1 both-elements gate, derived precisely:** checking must be enabled in BOTH the ACTIVATING
  element — at the CALL statement (per-statement TURN state; the site emits
  `ExceptionState.ExternalCheckMask = <bits>` only when non-zero) — and the ACTIVATED element — "before the
  Environment division" (a per-unit constant folded by `BinderDriver.BindUnitData` at the first
  post-Identification division header line). `ProgramTable.CallProgram` latches site-mask → `ActivatorExternalMask`
  around each activation (save/zero/restore — no leak across statements or nested calls); the gate is the bitwise
  AND, so each condition pairs independently. **⚠ SCOUT DRIFT CAUGHT (2):** the scout's plan put the
  before-Env-division rule on both sides — §14.8.4.1 scopes it to the ACTIVATED side only; and the scout's golden
  expected a post-CALL DISPLAY to run after an unhandled fatal raise — GR3h #2 → §14.6.13.1.3 terminates; the
  landed golden uses ON EXCEPTION.
- **Zero-scaffolding:** the `__DescribeExternals()` emission is gated on (group has ANY enabling EC-EXTERNAL TURN)
  && (unit describes externals) — `TurnState.AnyEnabledFor` (new) — so an EC-free group's generated source is
  byte-identical (characterization 33 ✓). A mask-zero unit in a TURN-carrying group still REGISTERS (a later
  element must be able to check against it); cross-compilation-group descriptors from a TURN-free group are
  absent, which is exactly the no-enablement-no-check posture.
- **Wiring:** EcBinder `ExternalNames` on `BoundCallProgram` (CANCEL excluded — §14.9.5 GR8) · CallEmitter site
  mask + widened catch · OoEmitter `EmitExternalDescribes` (beside its sibling `EmitExternalBackings`; reuses
  `BinderDriver.ExternalItemIdentity` — the VCR 18/31 identity, now internal) · ProgramEmitter Call()/Activate()
  entry hooks. RECORD DELIMITER/RESERVE/COLLATING are not modeled by FileModel ⇒ identical by construction,
  absent from the fingerprint (noted in code). UDF/INVOKE activations: site mask 0 ⇒ no raise at those
  boundaries — a named residue (function refs carry no ON EXCEPTION; the INVOKE GR7d leg needs the OO activation
  seam), recorded in the remaining-waves scout's Wave E section owner.
- **Goldens (3, CLI-probed, GreenfieldOnly + manifest + legacy exclusions same commit):**
  `2023/ec_external_format_conflict` (GR6 byte-count 4v8; the failed CALL provably never ran the sub; a
  conforming third describer then succeeds and shares the cell §8.6.7) · `2023/ec_external_file_mismatch`
  (GR1(e) organization SEQUENTIAL vs RELATIVE) · `2023/ec_external_data_mismatch` (LINAGE data-name vs literal —
  the one in-group-reachable §14.8.4.2 face, since VCR 18/31's compile checks block the FILE STATUS/RELATIVE KEY
  faces) · negative `ec-external-turn-below-2023` (COBOLNET0878 at 2002/2014). No new diag codes (runtime EC
  names only).

Battery: characterization 33 byte-exact · unit 313 · full Conformance run pending at commit gate.

