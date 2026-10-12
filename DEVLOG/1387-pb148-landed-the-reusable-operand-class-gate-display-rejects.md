## Entry 1387 — 2026-08-29 09:43 PDT — PB148 LANDED: the reusable operand-CLASS gate — DISPLAY rejects pointer/object/index/NULL at compile time, the A.1 item-56/58 determinations complete, and the island legs consolidate into PB164

The recurring 'shall not be of class X' shape has ONE gate now (kb/Work PB148):
ExpressionBinder.ScreenOperandClass over IntrinsicArgumentRules.ClassOf — the one classifier, so 'class
pointer' spans data-/function-/program-pointer and an index-classed INTRINSIC RESULT (FUNCTION MAX(IX1
IX2), §15.2 item 6) screens exactly like an index DATA item. DISPLAY wires it first: §14.9.11.3 SR1's
class object/pointer (previously printed ManagedPointer.ToString() / the CLR object text),
§13.18.60.3 SR10's index DATA item (previously printed an EMPTY zero-digit image — the R16/0809 family's
unswept third arm), and the word NULL — which is NOT a §8.3.3.6.2 figurative constant (Formats 1–7 close
the list; the old code cited a FABRICATED §8.3.3.7, corrected to §8.4.3.7/§8.4.3.10) and previously
printed U+0000. All four are COBOLNET1694 rejections with negatives. The A.1 register gains item 56 WHOLE
(the PICTURE image + the signed-non-DISPLAY-usage leading-minus VARIABLE-WIDTH form + the float
shortest-round-trip image + verbatim character transfer) and item 58 (transfer size: UNBOUNDED, one
write); pb148_display_forms pins the ALL legs once-only in three spellings, the ±COMP pair, the float and
the boolean; pb148_display_zero_length prints [] over a zero-length literal + zero-ODO group + zero-length
ref-mod slice. The Tier-C island legs (group with float/COMP-5/INDEX leaves; the VLG display format +
A.1 item 57) consolidate into PB164's note — one island, one item. Fixture learning: a compiler directive
in a FIXED-FORM file must start at column 8 (column 7 is the indicator area); the sibling golden rode
free-form classification and hid the requirement.

Verdicts: SR-14.9.11.3-1 DIVERGES → CONFORMS, GR-14.9.11.4-1/-3 PARTIAL → CONFORMS, GR-14.9.11.4-4/-6
stay PARTIAL and GR-14.9.11.4-7 NOT-IMPLEMENTED (the PB164 island); 2 goldens + 4 negatives.

