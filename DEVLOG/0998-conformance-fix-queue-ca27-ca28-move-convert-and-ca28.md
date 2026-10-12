## Entry 998 — 2026-07-22 23:09 PDT — Conformance fix-queue: CA27 + CA28 (move-convert) — and CA28 exposed a TEST + a VCR row that both pinned a spec-WRONG value

Two move-convert majors in MoveEmitter.cs — and CA28 turned out to be the most valuable find so far: it caught the
greenfield emitter, a conformance TEST, and a VERSION_CHANGE_REFERENCE row all agreeing on a spec-INCORRECT behavior
(the exact class the spec-first campaign exists to find — a bug invisible to every differential because a test
enshrined it).

**CA27 (§14.9.25.4 GR5 + GR6d1).** MOVE of a numeric-edited source to a numeric-edited (or numeric) receiver must
DE-EDIT the sender to its numeric value (sign included) then re-edit into the receiver mask. `IsNumericOperand` — the
guard selecting the de-editing arm — returned true only for Category Numeric, so a numeric-edited source fell to the
fall-through arm (`NumFromAlphanumeric`, which digit-extracts the edited image, dropping the decimal point + sign):
' 12.34' → 1234 scale 0 → '234.00'. FIX: the BoundFieldOperand arm now accepts `Numeric or NumericEdited`, routing to
the de-editing arm (`num.AsNum` → CobolEdit.DeEdit at the mask scale). Verified: ' 12.34' → '012.34'; signed −12.34
into −999.99 → '-012.34' (sign preserved).

**CA28 (§14.9.25.4 GR4).** A MOVE with a GROUP receiver is NON-elementary (GR4 ¶1 — an elementary move needs an
elementary RECEIVER), so GR4 governs: "treated as an alphanumeric to alphanumeric elementary move, EXCEPT that there
is no conversion of data from one form of internal representation to another" (spec line 28569). GR6a's operational-
sign drop is scoped to GR6's "valid elementary moves" (line 28573/28586) and does NOT apply. EmitGroupMove rendered a
signed-numeric elementary source with `deSign:true`, stripping the overpunch ('−45' → '045'). FIX: `deSign:false` so
the DISPLAY overpunch image copies verbatim ('−45' → '04N', '−123' → '12L', '+123' → '12C'), matching the
already-correct GROUP-sender path and GnuCOBOL/IBM/MF. No-op for non-numeric / unsigned senders. The mis-citing
comment (§8.8.4.1, a relation rule) is rewritten to §14.9.25.4 GR4.

**THE SPEC-WRONG TEST + VCR ROW (the real story).** The full conformance run went 3872/1-fail: the single red was
`SignedAlphanumericMoveDifferentialTests.SignedToAlphanumericGroup_DeSigned`, an `AssertSpecOnly` test asserting the
group move DROPS the sign ('045'). I did NOT re-baseline it — I went to the spec (process rule #1). §14.9.25.4 GR4
(line 28569) is unambiguous: a non-elementary (group) move does NOT convert internal representation → the overpunch is
PRESERVED. The test had applied §8.8.4.1 — a RELATION-CONDITION rule ("a group compares as elementary alphanumeric")
— to MOVE, where GR4 governs (its own comment even admitted the legacy produced the sign-preserving '04N' and
mislabelled that spec-CORRECT value "non-conformant"). So the emitter bug, the test, AND VCR row 130c ("signed → group
de-sign", DEVLOG 509/516/517) were the SAME misreading, mutually reinforcing. Corrected all three: the emitter (CA28),
the test → `SignedToAlphanumericGroup_SignPreserved` expecting '04N' with a GR4 rationale + a class-doc caveat, and VCR
row 130c RETRACTED (the summary "three pin-to-spec determinations" → "two"; 130c is now a no-divergence ref-only row —
the legacy was correct, nothing to pin). The elementary-receiver de-sign (`SignedToAlphanumeric_NegativeMagnitude` →
'123') and the comparison de-sign (row 130b, §8.8.4.2.5) stay correct — GR6a/§8.8.4.2.5 DO apply there. This is the A7
"test pinning WRONG behavior" risk made concrete.

**Verified (direct CLI, all branches) + gate.** CA27 012.34 / signed -012.34; CA28 12L / 12C / (−45)→04N. Goldens
`tests/conformance/2023/move_numeric_edited_source` + `move_group_overpunch` (manifest-only — NO GreenfieldOnly, the
legacy differential is decoupled). Characterization 33/33 (no drift), unit 575/575, greenfield Conformance 3873/3873
after the test correction (the pre-correction run was 3872/1, that one being the spec-wrong test).

