## Entry 2111 — 2026-10-16 14:15 PDT — PB1551 + PB1008: every receiving position parses the identifier formats that are never a receiving operand, and one form table refuses each by its own rule

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
(2871), `pb1551-program-address-move` (3979), `pb1551-self-arithmetic-sending` (0844)
and `pb1551-self-undeclared-85` (1639 at 85); `l1m7-null-receiving-operand` re-pinned 0901 → 3977 and
`l1m7-program-address-receiving` COBOL0201 → 3979 and `gn1-data-address-receiving-initialize` 0901 → 3979; positive `2002/pb1551_receiving_identifier_routes` runs the legal
neighbour of every superset shape (SELF and NULL senders, INVOKE RETURNING, ALLOCATE RETURNING, Format 7's `SET ADDRESS
OF B TO P`, the data- and program-address senders, ADD GIVING and MULTIPLY BY) and prints SAME / ABCD / EQ / 0002 / SUB
/ NUL. Drift test `ReceivingOperandFormDriftTests`: every form has its rule; the grammar rule spells exactly the
alternatives `FormOf` reads; no receiving phrase (SET, INTO, GIVING, RETURNING, REMAINDER, POINTER, VARYING, AFTER,
TALLYING IN) takes a bare data reference outside five named, reasoned exemptions. Rows SR-8.4.3.8.3-2 and
SR-8.4.3.5.3-2 PARTIAL → CONFORMS (GAP 78 → 76); SR-8.4.3.7.3-1 and SR-8.4.3.13.3-4 re-witnessed by their rules.
kb/Work PB1551 and PB1008 landed; PB1136 unblocked; PB1782 step 1 recorded.

**Branch review round 1 (kb/Work PB3027).** The reviewer found two holes, both real. R1-1: the BY REFERENCE argument of
CALL and INVOKE was still a bare `dataReference`, though §14.9.4.3 SR5 and §14.9.23.3 SR20 make it a receiving operand,
so `CALL "X" USING BY REFERENCE SELF` (and SUPER, and `O AS UNIVERSAL`, and the INVOKE twins) still drew COBOLNET0901 —
the very refusal this entry set out to remove, while the rows claimed "every receiving position". Both slots now take
`addressIdentifier | predefinedNull | receivingIdentifier | OMITTED`: the address-identifier stays SR4/SR19's sending
operand and NULL keeps SR3/SR9's refusal, because ANTLR resolves the overlap to the lower alternative. The sibling sweep
found the IMPLIED BY REFERENCE refused under the wrong description: CALL Format 1's keyword-less SELF under GR5's
prevailing BY REFERENCE was "a function-identifier or inline method invocation", and a keyword-less SELF through a
universal object reference (SR6) was "a literal or arithmetic-expression argument". Both now reach the form table's row
(`ReceivingOperandForms.FormOfOoIdentifier`), and the universal path no longer repeats a reported `ResolveReceiving`
null as a second COBOLNET0866. `BY REFERENCE ADDRESS OF FUNCTION F` became §8.4.3.12.3 SR3 (COBOLNET3979; it was
COBOL0001), so SR-8.4.3.12.3-3's "exactly ONE grammar home" notes were restated. R1-2: the drift guard's keyword list
had no TO, so it could not have caught the slots this entry converted; it now reads TO, REFERENCE, ACCEPT, INITIALIZE,
SET CONTENT/SIZE OF, UNSTRING DELIMITER/COUNT IN, the ROUNDED resultant, INSPECT's counter, an alternation group and a
list rule one level down, and a theory pins each shape (run over base 59208d8fb's grammar it flags 42 rules where the
first cut flagged 24). New negatives `pb1551-call-by-reference-self`, `pb1551-call-implied-reference-self`,
`pb1551-invoke-universal-self` (3978), `pb1551-invoke-by-reference-view` (2871), `pb1551-function-address-receiving`
(3979). A function-identifier or inline invocation written BY REFERENCE is still a parse error, by the computed formats'
structural exclusion that this entry chose. Gate (run 20261016T201750Z-634ce0): `LEG 1 ONLY (batched gating, PB2515):
GREEN`; the run before it was red only because the five negatives were not in the manifest.

**Branch review round 2.** R2-1: the guard's alternation group was a greedy regex, so each receiving keyword yielded
one operand, the group's LAST lower-case alternative. `GIVING (dataReference | literal)` and `INTO (dataReferenceList |
identifierX)` therefore passed. It now reads the FIRST set of the element sequence after the keyword
(`ReceivingOperandFormDriftTests.SlotOperands`): every top-level alternative of a group, nested groups, and past an
optional element (`?`, `*`, a group with an empty alternative, a semantic predicate), so `TO (ADDRESS OF?)?
dataReference` is seen too. The sibling one level down had the same hole, since a list rule was read only at its first
alternative. It now reads the rule body as a group. Two names had been hard-coded as the converted shape; the test now
treats any operand rule that can begin with `nonReceivableIdentifier` as converted, so SET's `setAddressReceiver`
qualifies without being listed. The wider scan found no missed receiving slot in the grammar. It found four slots that
are not receiving, each now exempt with its checked rule. Three are sending operands after TO, exempt per operand rule
so their statements' receiving slots stay guarded: `sendingOperand` (INSPECT CONVERTING's identifier-7, §14.9.22.4 GR5),
`setAddressSender` (SET Format 7's identifier-6, §14.9.39.3 SR19) and `setLocaleSource` (SET Format 11's identifier-10,
§14.9.39.3 SR25). The fourth is `usingByReference`, the PROCEDURE DIVISION header's formal parameter (§14.2.2 SR1).
Eleven theory cases pin the new shapes, and three converted shapes pin the complement.

