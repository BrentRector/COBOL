## Entry 981 — 2026-07-22 12:37 PDT — §24 fix-queue: EC-seam batch #2 — V3 EC-DATA-NOT-FINITE + EC-DATA-OVERFLOW (float paths) LANDED

Second EC-seam wave — the two catalogued-but-never-raised FATAL float exceptions. **EC-DATA-NOT-FINITE** (§14.6.13.2
item 3): a standard-float SENDING operand whose content is NaN/±Infinity, referenced by any statement, raises unless
exempt (class condition, sign condition, same-usage MOVE, VALIDATE). **EC-DATA-OVERFLOW** (§14.9.25.4 GR4 step 4a): a
MOVE whose finite value overflows a single-precision float receiver to ±Infinity. Both fatal (Table 13), both
default-OFF.

**Design adversarially validated BEFORE any code (4-agent Workflow, 3 probes → synthesis) — it caught the first cut being
structurally wrong in four ways, the reason to validate byte-critical seams first.** (1) `NumericRenderer:140` is NOT the
singular chokepoint — `OperandText.FieldAsString:94` (`CobolFloat.Display`, the string-image read) is co-equal and carries
non-exempt reads a 140-only wrap would silently MISS: DISPLAY, STRING, MOVE float→alnum/national/edited/boolean/group,
float-vs-alnum relation, CALL USING a float. BOTH chokepoints are now wrapped. (2) my "emit-conditional wrap" idea was a
singular-pattern violation AND impossible (`FieldNumCore` is `internal static`, no `EmitContext`); the correct pattern is
**always-emit + runtime-flag** (exactly `CobolString.RefMod`/`RefModError`), which — since the characterization corpus is
float-snapshot-free — changes NO committed `.g.cs`. (3) the class-condition "naturally exempt" claim only held because :94
was unwrapped; once wrapped it needs `floatCheck:false` like the sign condition. (4) the same-usage MOVE exemption must be
gated on `Usage`-equality, not "receiver is float" (COMP-1↔COMP-2 is a conversion, non-exempt).

Implementation (the always-emit singular pattern): `ExceptionEngine.FloatNotFiniteChecking`/`FloatOverflowChecking` +
`FloatNotFiniteError`/`FloatOverflowError` (mirror `RefModError`) → both ECs join `FatalAmbientGates` (the try/catch F3
dispatch + flag set/reset) + `EcWrap` (NOT-FINITE conservative any-statement, OVERFLOW on `BoundMove`) → `CobolFloat.Sending`
(float+double overloads) at both chokepoints via `RuntimeApi.FloatSending`/`FloatDisplay`, `CobolFloat.StoreSingleChecked`
via `RuntimeApi.FloatStoreSingleChecked` (cast-based `IsFinite(src) && float.IsInfinity((float)src)`, never a MaxValue
compare). Exemptions = raw reads: `NumericRenderer._floatSendingExempt` (re-entrant save/restore, propagates through the
`.Accept` recursion) threaded via a `floatCheck` param on `FieldNumCore`; `OperandText` gains four cached `AsStringVisitor`
(deSign × floatCheck); sign condition → `floatSendingExempt:true`, class condition (built-in + user) → `floatCheck:false`,
same-usage MOVE → `Usage`-equality. Arithmetic float STORE stays a bare cast (a ±Inf arithmetic result is the valid
§14.6.8.3 GR1 value, never EC-DATA-OVERFLOW — MOVE-only).

**Spec-fidelity call (mine, not the validation's):** §14.6.13.2 item 3 scopes the EC to a "STANDARD floating-point usage" =
the ISO 60559 FLOAT-BINARY/-DECIMAL forms; FLOAT-SHORT/-LONG/-EXTENDED + COMP-1/2 are implementor-defined (§13.18.60 item
21) with implementor-specified exception conditions (§14.6.13.4). Since the typed-native model makes EVERY float usage an
IEEE binary `float`/`double`, COBOL.NET raises both ECs uniformly — mandatory for the standard usages, an implementor
determination for the rest (pinned in `CONFORMANCE.md §3`). Two documented gaps recorded there: float NUMERIC-EDITED MOVE
receiver (EC-DATA-OVERFLOW), and the multi-float-receiver ADD/SUBTRACT half-commit under USE-F3 RESUME (an undefined-results
case; a `ContainsRefMod`-style precise follow-on rather than restructure the byte-critical arithmetic store loop).

Gate: goldens `2023/ec_data_not_finite` (3 exemptions no-raise + 4 raise legs across BOTH chokepoints, via a USE
declarative + RESUME NEXT) + `2023/ec_data_overflow` (single-precision overflow raises; double-receiver + underflow do
not) · characterization **33/33 byte-identical** (incl. the `RuntimeApiGuard` ratchet — every new float call routes through
`RuntimeApi`, no bare `CobolFloat.*` increase) · greenfield Unit **571/571** · CLI-probed all legs + the OFF byte-identical
control (`Infinity` displays normally with no directive). The full Conformance + guard/GnuCOBOL comprehensive gate runs
before the EC-seam batch merges (V4 EC-RANGE next). Design SSOT `COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` updated (the
two-chokepoint always-emit gate + the four exemptions).

