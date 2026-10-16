## PB1551 + PB1008: every receiving position parses the identifier formats that are never a receiving operand, and one form table refuses each by its own rule

**The defect (kb/Work PB1551, re-probed on 59208d8fb).** ISO §8.4.3 writes each receiving-operand exclusion into the
identifier FORMAT, not into a statement: §8.4.3.7.3 SR1 "NULL shall not be specified as a receiving operand",
§8.4.3.8.3 SR2 (SELF and SUPER), §8.4.3.5.3 SR2 (the object-view), §8.4.3.11.3 SR5 / §8.4.3.12.3 SR3 / §8.4.3.13.3 SR4
(the data-, function- and program-address-identifiers: "This identifier format shall not be specified as a receiving
operand"). Every receiving slot of the grammar was a bare `dataReference`, so `SET NULL TO O`, `SET SELF TO NULL`,
`MOVE X TO SELF`, `INITIALIZE SELF`, `ACCEPT SELF`, `INVOKE … RETURNING SELF`, `MOVE X TO U AS C` and `MOVE PP TO
ADDRESS OF PROGRAM "X"` were parse errors re-coded to §8.9 (COBOLNET0901 "'SELF' is a reserved word … cannot be used as
a user-defined word"), `SET ADDRESS OF PROGRAM "X" TO PP` a COBOL0201, and `ADD 1 TO SELF.` a bare COBOL0001 — the
right verdict for a rule the program had not broken, which kb/Work PB1782 measured would turn into SILENT ACCEPTANCE the
moment its step 2 widens the identifier tier. The object-view alone had a superset parse, in two positions (SET Format
5's receivers and INVOKE RETURNING), each with its own refusal text; EXCEPTION-OBJECT's rule was written twice (PB1008).

**The fix: the superset parse plus ONE table.** A grammar `receivingIdentifier` (`dataReference | nonReceivableIdentifier
| dataAddressIdentifier`) now stands in every receiving slot — MOVE and MOVE / ADD / SUBTRACT CORRESPONDING, INITIALIZE,
ACCEPT, the arithmetic resultants, COMPUTE, DIVIDE REMAINDER, CALL and INVOKE RETURNING, READ and RETURN INTO, STRING
INTO / POINTER, UNSTRING INTO / DELIMITER IN / COUNT IN / POINTER / TALLYING, INSPECT TALLYING, SEARCH VARYING,
PERFORM VARYING / AFTER and ALLOCATE RETURNING — and `setReceivingIdentifier` (the same, less the data-address
alternative, because Format 7 prints `ADDRESS OF data-name-1` as a receiver of its own, Annex D.9.2.1, and
setToValueStatement precedes setAddressStatement) in every SET receiving list. `nonReceivableIdentifier` spells the view
of a data reference, SELF / SUPER / NULL with or without a view, and the function- and program-address-identifiers.
`ExpressionBinder.ResolveReceiving` gains the two overloads and refuses each form by its row of the new
`ReceivingOperandForms` table, keyed by `NonReceivableForm`; LINE-COUNTER, LINAGE-COUNTER, EXCEPTION-OBJECT and the view
of a qualified item (the forms a data reference spells) report from the same rows. SET refuses before it selects a
format (`SetBinder.RefuseNonReceivable`). New codes COBOLNET3977 (NULL), 3978 (SELF / SUPER), 3979 (the
address-identifiers). At COBOL-85, where §8.9 leaves SELF / SUPER / NULL / ADDRESS free, an undeclared one is the
§8.4.2.1 undefined reference (COBOLNET1639) — the ConditionBinder class-condition precedent — never a rule about a
predefined identifier the edition does not have. In the mixed arithmetic rules' SENDING role (Format 2 with GIVING)
the operand walk refuses such a form by §8.8.1.1 (COBOLNET0844) instead of binding the data reference inside a view or
an ADDRESS OF — the first build bound `ADD 1 TO ADDRESS OF B GIVING N` as `B` and ended `ADD 1 TO SELF GIVING N` in
the COBOLNET2362 internal error, both caught by the position-sweep probes before commit. The function-identifier and
the inline invocation stay out of the purely receiving rules by construction (InlineMethodInvocationOperandDriftTests).
Deleted: `objectViewReceiver`, `setObjectReferenceReceiver`, `OoBinder.RefuseObjectViewReceiver`, the SET
object-reference loop's EXCEPTION-OBJECT arm (PB1008), and a second "COMPUTE receiver … is unresolvable" COBOLNET1511
that a boolean COMPUTE stacked on every receiver the chokepoint had already refused.

**Goldens and rows.** Negatives `pb1551-self-receiving`, `pb1551-super-receiving` (3978), `pb1551-object-view-initialize`
(2871), `pb1551-data-address-receiving`, `pb1551-program-address-move` (3979), `pb1551-self-arithmetic-sending` (0844)
and `pb1551-self-undeclared-85` (1639 at 85); `l1m7-null-receiving-operand` re-pinned 0901 → 3977 and
`l1m7-program-address-receiving` COBOL0201 → 3979; positive `2002/pb1551_receiving_identifier_routes` runs the legal
neighbour of every superset shape (SELF and NULL senders, INVOKE RETURNING, ALLOCATE RETURNING, Format 7's `SET ADDRESS
OF B TO P`, the data- and program-address senders, ADD GIVING and MULTIPLY BY) and prints SAME / ABCD / EQ / 0002 / SUB
/ NUL. Drift test `ReceivingOperandFormDriftTests`: every form has its rule; the grammar rule spells exactly the
alternatives `FormOf` reads; no receiving phrase (SET, INTO, GIVING, RETURNING, REMAINDER, POINTER, VARYING, AFTER,
TALLYING IN) takes a bare data reference outside five named, reasoned exemptions. Rows SR-8.4.3.8.3-2 and
SR-8.4.3.5.3-2 PARTIAL → CONFORMS (GAP 78 → 76); SR-8.4.3.7.3-1 and SR-8.4.3.13.3-4 re-witnessed by their rules.
kb/Work PB1551 and PB1008 landed; PB1136 unblocked; PB1782 step 1 recorded.
