## Entry 1142 — 2026-08-02 12:03 PDT — The 'i'/'n' adjudication: a numeric-edited item is not an arithmetic operand, and three artifacts were wrong together

`IntrinsicArgumentRules` had two argument screens resting on readings of §8.8.1.1 that could not both be right.
The `'n'` arm (class numeric) REFUTED the idea that a numeric-edited item de-edits into a numeric argument
position — its negative fixture `pb1-numeric-arg-numeric-edited` catches the widening. The `'i'` arm (§15.3 type
6, integer) was landed ON that same idea, and its comment said so outright: "a numeric-edited item is a legal
arithmetic operand, because it DE-EDITS to a defined numeric value." The owner flagged the contradiction and
declined to decide it, asking that it be derived and brought back if it turned.

It turned.

### The derivation

Three citations, each run through `cite.py --check`:

- **§8.8.1.1** — an arithmetic expression may be "an identifier referencing a **numeric data item**", a numeric
  literal, or ZERO. **§8.5.2.13** — such an item "is referred to as a **numeric-edited data item**", a distinct
  defined term. **§8.5.2.1 Table 2** files category numeric-edited under class ALPHANUMERIC (usage display) or
  NATIONAL — never class numeric. So it is neither the class nor the category §8.8.1.1 names.
- **§15.3 type 6** offers exactly two alternatives: "an arithmetic expression … or an **integer data item**". A
  numeric-edited item is not an integer data item either.
- **De-editing is a MOVE facility.** Every de-editing rule in the standard is a MOVE or editing rule —
  §14.9.25.4 GR5/GR6d1 ("de-editing establishes the operand's numeric value"), the CURRENCY SIGN and LOCALE
  clauses. GR6d1 has to GRANT de-editing for the MOVE, which would be unnecessary if it were generally available
  to any numeric context.

**And the decisive evidence was already sitting three lines up in the fix queue.** §15.43.3 r1
(HIGHEST-ALGEBRAIC) admits "a data item of category numeric **or numeric-edited**". When the standard means to
admit a numeric-edited argument, it says so explicitly. Type 6 and type 10 name it nowhere — and a reading that
makes numeric-edited implicit in "numeric data item" would make §15.43.3 r1's second alternative redundant. That
argument is worth more than the other three combined, and it had been recorded in our own queue for weeks as a
note about a DIFFERENT function.

### The two external oracles

Per the standing rule — ISO first, then compare interpretation against GnuCOBOL and NIST:

- **NIST**: the entire corpus stayed green across the flip. The 42 IF1xx intrinsic programs and every other NIST
  program compiled and matched. NIST does not depend on a numeric-edited arithmetic operand.
- **GnuCOBOL**: its suite exercises de-editing exclusively under MOVE — every case is titled "MOVE with
  de-editting to USAGE DISPLAY / DECIMAL IS COMMA / BINARY / COMP-3 / COMP-5". There is no arithmetic-with-edited
  case, and its syntax tests file edited items only under MOVE.

Both agree with the ISO reading.

### What moved, and the lesson

The owner decided: exclude it everywhere. Four things moved together, which is the point:
`ExpressionBinder.NonNumericOperandKind` (DA6's §8.8.1.1 screen, which had deliberately matched only
`EditMask: null` so a numeric-edited item slipped through), the `'i'` arm, the corpus golden `func_expr_arg`'s
`EDT=` case, and the AssertSpec test `StringChannel_NumericEditedArg_DeEdits`.

That last pair is the durable lesson. The `'i'` arm's defence was that "a corpus golden plus an AssertSpec unit
test had encoded that from the spec long before this screen existed" — two independent-looking witnesses. They
were not independent: both were written from the same premise, so they could only ever agree with each other.
**A golden and a test agreeing is not corroboration when both were derived from the same reading.** Three
artifacts agreed for months and all three were wrong together — and the whole-corpus run makes that visible in
one line, because those three were the ONLY reds among 4166 cases.

The misapplied citation is worth recording too: the AssertSpec test justified an INTEGER argument position
(§15.3 type 6) by citing §14.9.25.4 GR5 — a MOVE general rule. The rule was real and quoted correctly; it was
simply about a different statement. That is the `validate_the_premise_not_only_the_rule` failure in one line.

⚠ Narrow scope, deliberately: this changes only what an ARITHMETIC operand and an INTEGER argument may be. The
`'s'` string family still admits a numeric-edited item, because Table 2 genuinely makes it class alphanumeric and
a CLASS rule means what it says. MOVE de-editing (§14.9.25.4 GR5/GR6d1) is untouched and correct.

New fixture: `pb1-integer-arg-numeric-edited`, the type-6 sibling of `pb1-numeric-arg-numeric-edited`.

