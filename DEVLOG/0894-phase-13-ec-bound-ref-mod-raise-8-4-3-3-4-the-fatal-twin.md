## Entry 894 — 2026-07-18 15:07 PDT — PHASE-13 EC-BOUND-REF-MOD raise (§8.4.3.3.4) — the fatal twin

Raise the fatal EC-BOUND-REF-MOD when a reference modification's leftmost-position or length is out of range (or an
unallowed zero-length), under >>TURN EC-BOUND-REF-MOD CHECKING ON. Closes the second staged EC-BOUND condition
(the REF-MOD-ZERO-LENGTH directive that ALLOWS a zero-length result is a split-out follow-on — it needs the RefModPlace
shape change; the raise itself does not).

**⚠ SPEC-CITATION DRIFT CAUGHT (both prior sources wrong).** The audit cited §8.4.2.4, the remaining-waves scout cited
§8.4.2.3 — but §8.4.2.3 is "Subscripts" and §8.4.2.4 is numeric comparison. Grep of the spec put reference
modification at **§8.4.3.3** (§8.4.3.3.4 General rules holds the GR at spec :7089: "If the evaluation of
leftmost-position or length results in a non-integer value, a zero value, or a value that references a position
outside the area of identifier-1, the EC-BOUND-REF-MOD exception condition is set to exist. However, when the
REF-MOD-ZERO-LENGTH directive is in effect, a zero-length result is allowed."). Cited §8.4.3.3.4 throughout — a good
reminder to verify even the scout spec-first ([[feedback_use_the_spec]]).

- Runtime: `ExceptionState`/Engine gains the fatal ambient gate `BoundRefModChecking` + `RefModError` (mirrors
  ArgumentFunctionChecking/ArgumentError — throws CobolFatalException only when checking is on) + the static shim.
  `CobolString.RefMod` (read) and `SpliceInto` (write) call `RefModError` BEFORE the clamp when leftmost<1 /
  leftmost>size / length==0 / leftmost+length-1>size; checking OFF (the default) falls through to the lenient
  clamp/space-pad — byte-identical for every existing program (no program enables the gate). Fixed the stale §8.4.2.4
  doc citation on both methods.
- Emit: GENERALIZED `EcEmitter.EmitArgOrPlain` (the fatal leg) to wrap for the SET of fatal ambient gates
  {EC-ARGUMENT-FUNCTION, EC-BOUND-REF-MOD} — ONE gate emits the literal name (byte-identical to the pre-generalization
  output; characterization 33 confirms), two+ use the actual __af.EcName for the status/dispatch. `EcBinder.EcWrap`
  adds EC-BOUND-REF-MOD conservatively (scout-sanctioned; the raise fires only at an actual out-of-range ref-mod).
- Golden: `tests/conformance/2002/ec_bound_ref_mod` (+ manifest). WS-X(7:2) on X(5) → out of range → USE AFTER
  EXCEPTION CONDITION declarative → "CAUGHT=EC-BOUND-REF-MOD" → RESUME AT NEXT STATEMENT → "Y=[??]" (the aborted MOVE
  left WS-Y unchanged). CLI-probed: checking-on+declarative (RESUME), checking-off (clamp "Y=[  ]"), fatal-unhandled
  (abnormal termination, exit 1, the cited message). Scout golden had `RESUME NEXT STATEMENT`; the grammar requires
  `RESUME AT NEXT STATEMENT` (§14.9.33.2 — AT required) — corrected.

Wave-local gate (tiered per the new execution model): characterization 33 (emit byte-identity) + CorpusRunner 262
(+1 golden, EC/ref-mod regression clean). Greenfield-only; the comprehensive battery runs at the batch boundary.
The EC-BOUND surface is now closed (OVERFLOW + REF-MOD); Wave B's staging is retired.

