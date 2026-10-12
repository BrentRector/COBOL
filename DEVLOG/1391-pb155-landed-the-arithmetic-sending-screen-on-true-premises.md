## Entry 1391 — 2026-08-29 13:07 PDT — PB155 LANDED: the arithmetic sending screen on true premises — edited operands reject under strict in EVERY arithmetic context, string/hex/figurative literals fail at bind not in Roslyn, and the composite of operands counts what §14.7.7 counts with real mask geometry

Three mechanisms (kb/Work PB155). (1) NonNumericOperandKind's `EditMask: null` guard: an
alphanumeric-edited picture is Category Alphanumeric WITH a mask (no *Edited enum member), so
`ADD <PIC XXBXX> TO N` slipped the §8.8.1.1 screen and FieldNumCore digit-decoded it under STRICT — the
--permissive leniency applied unconditionally. Gone; edited or plain, class alphanumeric/national is not
class numeric (§8.5.2.1 Table 2; de-editing is MOVE's grant alone, §14.9.25.4 GR6d1). (2) NumLiteral's
fall-throughs: STRINGLIT/HEXLIT reached BoundNumLiteral carrying their QUOTED text and died as `"ABC"L` in
generated C# (the PB94 wrong-stage family) — now the same COBOLNET0844 as the NATLIT/BOOLLIT arm
(§8.3.3.2.1); a non-ZERO FIGURATIVE was a bare BoundExprError rendering as a runtime NotImplemented — now
0844 at bind (§8.8.1.1 admits only ZERO). (3) CheckComposite, wrong in BOTH directions: the literal arm
counted an E-form's characters (§14.7.7 r2 EXCLUDES floating-point literals — `ADD 1.5E+3 TO <9(28)>`
rejected LEGAL source with 0805), and InComposite's Category==Numeric gate DROPPED the numeric-edited
GIVING resultants MULTIPLY (§14.9.26.3 SR4: ALL operands) and DIVIDE (§14.9.12.3 SR4: all but REMAINDER)
compose. The edited contribution is the MASK's geometry via the new DataBinder.StoredShapeOf — the ONE
fixed-point (digits, scale) shape lifted from the §13.18.63.3 VALUE-fit site — with IsFloatEdited excluded
as a documented reading (no fixed decimal point to superimpose).

THE REVIEW FLEET (73 agents, 4 lenses, 23 raw findings, 15 adversarially confirmed) then reshaped the
landing: (a) the §8.8.1.1 screen was BYPASSED for OperandContext.ArithmeticIndexWindow — R29 flipped eleven
call sites (SET, PERFORM/SEARCH VARYING, compound subscripts, compound relation/EVALUATE operands) to the
window context claiming "identical screening" and the guards were never widened, so `SET IX TO XE`
digit-decoded under STRICT while ADD drew 0844. The DATA-ITEM guard is widened (pb155-set-to-edited pins
SET; a sole data reference short-circuits through FieldOperand, so comparand positions stay legal) — but
the gate MEASURED the INTRINSIC guard's widening rejecting legal source (six NIST IF programs' sole
`IF FUNCTION LOWER-CASE(X) = …`, pb101, 2002/char_national: a sole function comparand has no
short-circuit), so that half is reverted with the conflation registered as kb/Work PB172 (the window
context serves two rule regimes; splitting it is what makes `SET IX TO FUNCTION LOWER-CASE(X)`
screenable). The same gate run caught pb26's UNSTRING leg carrying ILLEGAL source the new SR2 delimiter
screen exposed — `DELIMITED BY FUNCTION LOG10(…)` is a category-numeric identifier-2 — and its
EC-ARGUMENT probe is now the SR2-legal `FUNCTION CHAR(0)` (§15.15.3 AR2 violated exactly as LOG10(0)
violated §15.56.3 AR2; the .out is unchanged). Three stale doc comments corrected. (b) The REMAINDER-exclusion golden leg
COULD NOT FAIL — the composite is maxInt + maxFrac across superimposed operands, not a sum, so an integer
quotient made R's inclusion invisible; Q is 9V9 now (31 + 1 = 32 if R counted) and the leg discriminates.
(c) UNSTRING §14.9.48.3 SR2 names identifier-1, -2, -3 AND -5 in one sentence; only the sender was
screened — SR2's category test is now the ONE Sr2OffendingCategory predicate asked by all four (a numeric
DELIMITED BY operand bound clean before; pb155-unstring-numeric-delimiter). The SR-14.9.48.3-2 row's PB88
CONFORMS had claimed all four names — re-recorded honestly. (d) StoredShapeOf's P-split anchored on 9/Z/*
only, so `PIC $$$$PP` (every digit position a FLOATING symbol) read its rightmost P run as LEADING — scale
+2 where the value is a multiple of 10^2 (scale −2), mis-shaping the composite AND the pre-existing VALUE
window; both CobolEdit.FractionDigits and PictureAnalyzer now anchor on the full digit-position set (9/Z/*
plus floating members, never a fixed sign — 99PPCR stays trailing), pinned by the new
CobolEditMaskGeometryTests (14 facts). (e) The IsFloatEdited exclusion is documented as a READING of
r2's superimposition definition, not as rule-2b text. Registered, not crammed: PB169 (STOP/GOBACK STATUS
literal-1's non-numeric form is legal COBOL now rejected under a miscited §8.8.1.1 — the fleet's refuters
proved no regression: it was a Roslyn splat before), PB170 (a SIMPLE subscript never reaches the screen —
the resolver renders it straight from the token), PB171 (a bare non-numeric literal sign-condition operand
degrades to constant 0). Also swept: STRING INTO's SR5 screen missed national-edited and UNSTRING SR4's
national arm admitted one — both shaped, both UNREACHABLE until Phase 4a residue #2 lands (a
national-edited ITEM is 0899 at declaration; deliberately no fixture on a loud stage). ScreenResultant
needed nothing — the batch-8 "receiving half unenforced" claim missed PB128's screen (probed 1675, pinned
DIVIDE-shaped by pb155-divide-into-alpha).

Verdicts: SR-14.9.2.3-1/-3 DIVERGES → CONFORMS · SR-14.9.12.3-1/-3/-4 PARTIAL → CONFORMS (all four
composite edges pinned) · SR-14.9.48.3-2 re-recorded (honest evidence). Goldens: 2023/pb155_composite_edges
(float-literal exclusion · SR1b GIVING exclusion · the discriminating REMAINDER exclusion); negatives
pb155-add-edited-operand, pb155-add-string-literal, pb155-divide-figurative, pb155-divide-into-alpha,
pb155-divide-composite-edited (a LEGAL 31-position Z9.9(29) receiver superimposed with 9(18) spans 47 —
the first draft's 33-position picture was itself illegal and the compiler's §8.3.1.2 cap caught it),
pb155-set-to-edited, pb155-unstring-edited-sender, pb155-unstring-numeric-delimiter; unit
CobolEditMaskGeometryTests (14 facts). Registered: PB169–PB172. Wave-local gate GREEN on this tree
(Conformance 3670/3670 with the NIST leg · Unit 4650/4650 · Characterization 33/33), inventory drift
gate 10/10, GAP 3672 → 3667. Battery #34 is owed for the PB151+PB154+PB155 batch.

